using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using BrokerPriceParser.Core.Contracts;
using BrokerPriceParser.Core.Llm;

namespace BrokerPriceParser.Infrastructure.Llm;

/// <summary>
/// Sends broker parser enrichment requests to the OpenAI Responses API.
/// </summary>
public sealed class OpenAiBrokerLlmClient : IBrokerLlmClient
{
    private readonly HttpClient _httpClient;
    private readonly BrokerLlmSettings _settings;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenAiBrokerLlmClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="settings">The LLM settings.</param>
    public OpenAiBrokerLlmClient(HttpClient httpClient, BrokerLlmSettings settings)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    /// <summary>
    /// Sends a broker LLM request and returns the provider response.
    /// </summary>
    /// <param name="request">The broker LLM request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The LLM response.</returns>
    public async Task<BrokerLlmResponse> ExecuteAsync(BrokerLlmRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!_settings.IsEnabled)
        {
            return CreateFailureResponse("LLM enrichment is disabled.");
        }

        var apiKey = Environment.GetEnvironmentVariable(_settings.ApiKeyEnvironmentVariableName);

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return CreateFailureResponse(
                $"Environment variable '{_settings.ApiKeyEnvironmentVariableName}' is not set.");
        }

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(_settings.TimeoutSeconds));

            var schemaElement = ParseSchemaElement(request.OutputSchemaJson);
            var requestBody = BuildRequestBody(request.Prompt, schemaElement);
            var requestJson = JsonSerializer.Serialize(requestBody);

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.ApiBaseUrl);
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            httpRequest.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

            using var httpResponse = await _httpClient
                .SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, timeoutCts.Token)
                .ConfigureAwait(false);

            var rawResponseText = await httpResponse.Content
                .ReadAsStringAsync(timeoutCts.Token)
                .ConfigureAwait(false);

            if (!httpResponse.IsSuccessStatusCode)
            {
                return new BrokerLlmResponse
                {
                    IsSuccess = false,
                    IsEnrichmentApplied = false,
                    RawResponseText = rawResponseText,
                    ParsedJsonPayload = string.Empty,
                    ErrorMessage = $"Provider returned HTTP {(int)httpResponse.StatusCode}."
                };
            }

            if (!TryExtractOutputText(rawResponseText, out var parsedJsonPayload))
            {
                return new BrokerLlmResponse
                {
                    IsSuccess = false,
                    IsEnrichmentApplied = false,
                    RawResponseText = rawResponseText,
                    ParsedJsonPayload = string.Empty,
                    ErrorMessage = "Provider response did not contain output_text."
                };
            }

            return new BrokerLlmResponse
            {
                IsSuccess = true,
                IsEnrichmentApplied = !string.IsNullOrWhiteSpace(parsedJsonPayload),
                RawResponseText = rawResponseText,
                ParsedJsonPayload = parsedJsonPayload,
                ErrorMessage = string.Empty
            };
        }
        catch (OperationCanceledException)
        {
            return CreateFailureResponse("LLM request timed out or was cancelled.");
        }
        catch (Exception exception)
        {
            return CreateFailureResponse($"LLM request failed: {exception.Message}");
        }
    }

    // ────────────────────────────────────

    /// <summary>
    /// Parses the JSON schema payload into a reusable <see cref="JsonElement"/>.
    /// </summary>
    /// <param name="outputSchemaJson">The serialized output schema JSON.</param>
    /// <returns>A cloned schema element.</returns>
    private static JsonElement ParseSchemaElement(string outputSchemaJson)
    {
        using var document = JsonDocument.Parse(outputSchemaJson);
        return document.RootElement.Clone();
    }

    // ────────────────────────────────────

    /// <summary>
    /// Builds the request body sent to the Responses API.
    /// </summary>
    /// <param name="prompt">The rendered prompt.</param>
    /// <param name="schemaElement">The structured output schema.</param>
    /// <returns>The request body object.</returns>
    private object BuildRequestBody(string prompt, JsonElement schemaElement)
    {
        return new
        {
            model = _settings.ModelName,
            input = prompt,
            text = new
            {
                format = new
                {
                    type = "json_schema",
                    name = _settings.SchemaName,
                    strict = true,
                    schema = schemaElement
                }
            },
            store = false
        };
    }

    // ────────────────────────────────────

    /// <summary>
    /// Extracts the top-level output_text field from a Responses API payload.
    /// </summary>
    /// <param name="rawResponseText">The raw response JSON.</param>
    /// <param name="outputText">The extracted output text.</param>
    /// <returns><c>true</c> if extraction succeeded; otherwise <c>false</c>.</returns>
    private static bool TryExtractOutputText(string rawResponseText, out string outputText)
    {
        using var document = JsonDocument.Parse(rawResponseText);
        var root = document.RootElement;

        if (root.TryGetProperty("output_text", out var outputTextElement)
            && outputTextElement.ValueKind == JsonValueKind.String)
        {
            outputText = outputTextElement.GetString() ?? string.Empty;
            return !string.IsNullOrWhiteSpace(outputText);
        }

        if (root.TryGetProperty("output", out var outputArray)
            && outputArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var outputItem in outputArray.EnumerateArray())
            {
                if (!outputItem.TryGetProperty("content", out var contentArray)
                    || contentArray.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var contentItem in contentArray.EnumerateArray())
                {
                    if (contentItem.TryGetProperty("type", out var typeElement)
                        && typeElement.ValueKind == JsonValueKind.String
                        && string.Equals(typeElement.GetString(), "output_text", StringComparison.OrdinalIgnoreCase)
                        && contentItem.TryGetProperty("text", out var textElement)
                        && textElement.ValueKind == JsonValueKind.String)
                    {
                        outputText = textElement.GetString() ?? string.Empty;
                        return !string.IsNullOrWhiteSpace(outputText);
                    }
                }
            }
        }

        outputText = string.Empty;
        return false;
    }

    // ────────────────────────────────────

    /// <summary>
    /// Creates a standardized failure response.
    /// </summary>
    /// <param name="message">The failure message.</param>
    /// <returns>A failure response.</returns>
    private static BrokerLlmResponse CreateFailureResponse(string message)
    {
        return new BrokerLlmResponse
        {
            IsSuccess = false,
            IsEnrichmentApplied = false,
            RawResponseText = string.Empty,
            ParsedJsonPayload = string.Empty,
            ErrorMessage = message
        };
    }
}