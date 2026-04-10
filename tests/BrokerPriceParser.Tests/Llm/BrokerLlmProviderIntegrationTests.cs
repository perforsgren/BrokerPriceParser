using System.Net;
using System.Net.Http;
using System.Text;
using BrokerPriceParser.Core.Llm;
using BrokerPriceParser.Infrastructure.Llm;
using Xunit;

namespace BrokerPriceParser.Tests.Llm;

/// <summary>
/// Contains provider-client level tests for <see cref="OpenAiBrokerLlmClient"/>.
/// </summary>
public sealed class BrokerLlmProviderIntegrationTests
{
    /// <summary>
    /// Verifies that the OpenAI provider client extracts output_text from a successful response.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ShouldReturnParsedJsonPayload_WhenResponseContainsOutputText()
    {
        const string apiKeyVariableName = "BROKER_PARSER_TEST_OPENAI_KEY";
        Environment.SetEnvironmentVariable(apiKeyVariableName, "test-key");

        var settings = new BrokerLlmSettings
        {
            IsEnabled = true,
            ApiKeyEnvironmentVariableName = apiKeyVariableName,
            ApiBaseUrl = "https://example.test/v1/responses",
            ModelName = "gpt-5",
            SchemaName = "broker_parse_enrichment",
            TimeoutSeconds = 30
        };

        var handler = new StubHttpMessageHandler("""
        {
          "id": "resp_123",
          "object": "response",
          "output_text": "{\"messageType\":\"Unknown\",\"eventType\":\"None\",\"instrument\":{\"pair\":\"\",\"tenor\":\"\",\"expiry\":\"\",\"structure\":\"\",\"delta\":\"\",\"strikeType\":\"\",\"strike\":\"\",\"optionSideBias\":\"\"},\"quote\":{\"bid\":\"\",\"ask\":\"\",\"mid\":\"\",\"quoteStyle\":\"\",\"isFirm\":\"\"},\"action\":{\"verb\":\"\",\"side\":\"\",\"target\":\"\",\"linkedToPriorQuote\":\"\"},\"llmHints\":{\"confidence\":\"0.10\",\"notes\":[]}}"
        }
        """);

        var httpClient = new HttpClient(handler);
        var client = new OpenAiBrokerLlmClient(httpClient, settings);

        var request = new BrokerLlmRequest
        {
            MessageId = "MSG-001",
            ConversationId = "CONV-001",
            RawMessage = "mom int",
            NormalizedMessage = "MOM INT",
            OutputSchemaJson = """
            {
              "type": "object",
              "properties": {},
              "additionalProperties": false
            }
            """,
            Prompt = "test prompt"
        };

        var response = await client.ExecuteAsync(request);

        Assert.True(response.IsSuccess);
        Assert.True(response.IsEnrichmentApplied);
        Assert.Contains("\"messageType\":\"Unknown\"", response.ParsedJsonPayload);
    }

    // ────────────────────────────────────

    /// <summary>
    /// Verifies that the OpenAI provider client returns a failure when the API key is missing.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenApiKeyIsMissing()
    {
        const string apiKeyVariableName = "BROKER_PARSER_TEST_OPENAI_KEY_MISSING";
        Environment.SetEnvironmentVariable(apiKeyVariableName, null);

        var settings = new BrokerLlmSettings
        {
            IsEnabled = true,
            ApiKeyEnvironmentVariableName = apiKeyVariableName,
            ApiBaseUrl = "https://example.test/v1/responses",
            ModelName = "gpt-5",
            SchemaName = "broker_parse_enrichment",
            TimeoutSeconds = 30
        };

        var httpClient = new HttpClient(new StubHttpMessageHandler("{}"));
        var client = new OpenAiBrokerLlmClient(httpClient, settings);

        var request = new BrokerLlmRequest
        {
            MessageId = "MSG-001",
            ConversationId = "CONV-001",
            RawMessage = "mom int",
            NormalizedMessage = "MOM INT",
            OutputSchemaJson = """
            {
              "type": "object",
              "properties": {},
              "additionalProperties": false
            }
            """,
            Prompt = "test prompt"
        };

        var response = await client.ExecuteAsync(request);

        Assert.False(response.IsSuccess);
        Assert.False(response.IsEnrichmentApplied);
        Assert.Contains("not set", response.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    // ────────────────────────────────────

    /// <summary>
    /// Provides a stub HTTP handler for deterministic provider-client tests.
    /// </summary>
    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _responseBody;

        /// <summary>
        /// Initializes a new instance of the <see cref="StubHttpMessageHandler"/> class.
        /// </summary>
        /// <param name="responseBody">The response body to return.</param>
        public StubHttpMessageHandler(string responseBody)
        {
            _responseBody = responseBody;
        }

        /// <summary>
        /// Sends a fake HTTP response.
        /// </summary>
        /// <param name="request">The HTTP request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A fake HTTP response.</returns>
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_responseBody, Encoding.UTF8, "application/json")
            };

            return Task.FromResult(response);
        }
    }
}