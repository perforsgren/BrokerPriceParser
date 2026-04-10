using System.Globalization;
using System.Text.Json;
using BrokerPriceParser.Core.Contracts;
using BrokerPriceParser.Core.Enums;
using BrokerPriceParser.Core.Llm;
using BrokerPriceParser.Core.Models;
using BrokerPriceParser.Core.State;

namespace BrokerPriceParser.Infrastructure.Llm;

/// <summary>
/// Provides optional LLM enrichment for broker parse results.
/// </summary>
public sealed class BrokerLlmEnrichmentService : IBrokerLlmEnrichmentService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IBrokerPromptBuilder _promptBuilder;
    private readonly IBrokerLlmClient _llmClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="BrokerLlmEnrichmentService"/> class.
    /// </summary>
    /// <param name="promptBuilder">The prompt builder.</param>
    /// <param name="llmClient">The LLM client.</param>
    public BrokerLlmEnrichmentService(
        IBrokerPromptBuilder promptBuilder,
        IBrokerLlmClient llmClient)
    {
        _promptBuilder = promptBuilder ?? throw new ArgumentNullException(nameof(promptBuilder));
        _llmClient = llmClient ?? throw new ArgumentNullException(nameof(llmClient));
    }

    /// <summary>
    /// Attempts to enrich the current parse result using the configured LLM layer.
    /// </summary>
    /// <param name="context">The parse context.</param>
    /// <param name="currentResult">The current parse result.</param>
    /// <param name="settings">The LLM settings.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The possibly enriched parse result.</returns>
    public async Task<BrokerParseResult> EnrichAsync(
        ParseContext context,
        BrokerParseResult currentResult,
        BrokerLlmSettings settings,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(currentResult);
        ArgumentNullException.ThrowIfNull(settings);

        if (!settings.IsEnabled)
        {
            return currentResult;
        }

        if (settings.UseOnlyForLowConfidence && currentResult.Quality.Confidence >= settings.LowConfidenceThreshold)
        {
            return currentResult;
        }

        var request = _promptBuilder.Build(context, currentResult, settings);
        var response = await _llmClient.ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccess || !response.IsEnrichmentApplied || string.IsNullOrWhiteSpace(response.ParsedJsonPayload))
        {
            return currentResult;
        }

        if (!TryDeserializeStructuredOutput(response.ParsedJsonPayload, out var structuredOutput))
        {
            return currentResult;
        }

        MergeStructuredOutput(currentResult, structuredOutput);
        return currentResult;
    }

    // ────────────────────────────────────

    /// <summary>
    /// Deserializes the structured LLM payload.
    /// </summary>
    /// <param name="payload">The JSON payload.</param>
    /// <param name="structuredOutput">The deserialized structured output.</param>
    /// <returns><c>true</c> if deserialization succeeded; otherwise <c>false</c>.</returns>
    private static bool TryDeserializeStructuredOutput(string payload, out BrokerLlmStructuredOutput structuredOutput)
    {
        try
        {
            structuredOutput = JsonSerializer.Deserialize<BrokerLlmStructuredOutput>(payload, SerializerOptions) ?? new();
            return true;
        }
        catch
        {
            structuredOutput = new BrokerLlmStructuredOutput();
            return false;
        }
    }

    // ────────────────────────────────────

    /// <summary>
    /// Safely merges missing or ambiguous fields from the LLM output into the current parse result.
    /// </summary>
    /// <param name="currentResult">The current parse result.</param>
    /// <param name="structuredOutput">The structured LLM output.</param>
    private static void MergeStructuredOutput(BrokerParseResult currentResult, BrokerLlmStructuredOutput structuredOutput)
    {
        MergeMessageType(currentResult, structuredOutput);
        MergeEventType(currentResult, structuredOutput);
        MergeInstrument(currentResult, structuredOutput.Instrument);
        MergeQuote(currentResult, structuredOutput.Quote);
        MergeAction(currentResult, structuredOutput.Action);
    }

    // ────────────────────────────────────

    /// <summary>
    /// Merges a missing message type from the LLM output.
    /// </summary>
    /// <param name="currentResult">The current parse result.</param>
    /// <param name="structuredOutput">The structured LLM output.</param>
    private static void MergeMessageType(BrokerParseResult currentResult, BrokerLlmStructuredOutput structuredOutput)
    {
        if (currentResult.MessageType != BrokerMessageType.Unknown)
        {
            return;
        }

        if (Enum.TryParse<BrokerMessageType>(structuredOutput.MessageType, true, out var parsedMessageType))
        {
            currentResult.MessageType = parsedMessageType;
        }
    }

    // ────────────────────────────────────

    /// <summary>
    /// Merges a missing event type from the LLM output.
    /// </summary>
    /// <param name="currentResult">The current parse result.</param>
    /// <param name="structuredOutput">The structured LLM output.</param>
    private static void MergeEventType(BrokerParseResult currentResult, BrokerLlmStructuredOutput structuredOutput)
    {
        if (currentResult.EventType != BrokerEventType.None)
        {
            return;
        }

        if (Enum.TryParse<BrokerEventType>(structuredOutput.EventType, true, out var parsedEventType))
        {
            currentResult.EventType = parsedEventType;
        }
    }

    // ────────────────────────────────────

    /// <summary>
    /// Merges instrument fields from the LLM output only when current values are missing.
    /// </summary>
    /// <param name="currentResult">The current parse result.</param>
    /// <param name="instrument">The LLM instrument.</param>
    private static void MergeInstrument(BrokerParseResult currentResult, BrokerLlmStructuredInstrument instrument)
    {
        if (string.IsNullOrWhiteSpace(currentResult.Instrument.Pair) && !string.IsNullOrWhiteSpace(instrument.Pair))
        {
            currentResult.Instrument.Pair = instrument.Pair;
            currentResult.Provenance.PairSource = FieldSourceType.Derived;
        }

        if (string.IsNullOrWhiteSpace(currentResult.Instrument.Tenor) && !string.IsNullOrWhiteSpace(instrument.Tenor))
        {
            currentResult.Instrument.Tenor = instrument.Tenor;
            currentResult.Provenance.TenorSource = FieldSourceType.Derived;
        }

        if (string.IsNullOrWhiteSpace(currentResult.Instrument.Expiry) && !string.IsNullOrWhiteSpace(instrument.Expiry))
        {
            currentResult.Instrument.Expiry = instrument.Expiry;
        }

        if (string.IsNullOrWhiteSpace(currentResult.Instrument.Structure) && !string.IsNullOrWhiteSpace(instrument.Structure))
        {
            currentResult.Instrument.Structure = instrument.Structure;
            currentResult.Provenance.StructureSource = FieldSourceType.Derived;
        }

        if (!currentResult.Instrument.Delta.HasValue
            && TryParseDecimalString(instrument.Delta, out var delta))
        {
            currentResult.Instrument.Delta = delta;
        }

        if (string.IsNullOrWhiteSpace(currentResult.Instrument.StrikeType) && !string.IsNullOrWhiteSpace(instrument.StrikeType))
        {
            currentResult.Instrument.StrikeType = instrument.StrikeType;
        }

        if (string.IsNullOrWhiteSpace(currentResult.Instrument.Strike) && !string.IsNullOrWhiteSpace(instrument.Strike))
        {
            currentResult.Instrument.Strike = instrument.Strike;
        }

        if (string.IsNullOrWhiteSpace(currentResult.Instrument.OptionSideBias) && !string.IsNullOrWhiteSpace(instrument.OptionSideBias))
        {
            currentResult.Instrument.OptionSideBias = instrument.OptionSideBias;
        }
    }

    // ────────────────────────────────────

    /// <summary>
    /// Merges quote fields from the LLM output only when current values are missing.
    /// </summary>
    /// <param name="currentResult">The current parse result.</param>
    /// <param name="quote">The LLM quote.</param>
    private static void MergeQuote(BrokerParseResult currentResult, BrokerLlmStructuredQuote quote)
    {
        var priceFieldMerged = false;

        if (!currentResult.Quote.Bid.HasValue && TryParseDecimalString(quote.Bid, out var bid))
        {
            currentResult.Quote.Bid = bid;
            priceFieldMerged = true;
        }

        if (!currentResult.Quote.Ask.HasValue && TryParseDecimalString(quote.Ask, out var ask))
        {
            currentResult.Quote.Ask = ask;
            priceFieldMerged = true;
        }

        if (!currentResult.Quote.Mid.HasValue && TryParseDecimalString(quote.Mid, out var mid))
        {
            currentResult.Quote.Mid = mid;
            priceFieldMerged = true;
        }

        if (currentResult.Quote.QuoteStyle == QuoteStyle.Unknown
            && Enum.TryParse<QuoteStyle>(quote.QuoteStyle, true, out var parsedQuoteStyle))
        {
            currentResult.Quote.QuoteStyle = parsedQuoteStyle;
        }

        if (!currentResult.Quote.IsFirm.HasValue && TryParseBooleanString(quote.IsFirm, out var isFirm))
        {
            currentResult.Quote.IsFirm = isFirm;
        }

        if (priceFieldMerged && currentResult.Provenance.PriceSource == FieldSourceType.None)
        {
            currentResult.Provenance.PriceSource = FieldSourceType.Derived;
        }
    }

    // ────────────────────────────────────

    /// <summary>
    /// Merges action fields from the LLM output only when current values are missing.
    /// </summary>
    /// <param name="currentResult">The current parse result.</param>
    /// <param name="action">The LLM action.</param>
    private static void MergeAction(BrokerParseResult currentResult, BrokerLlmStructuredAction action)
    {
        var actionFieldMerged = false;

        if (string.IsNullOrWhiteSpace(currentResult.Action.Verb) && !string.IsNullOrWhiteSpace(action.Verb))
        {
            currentResult.Action.Verb = action.Verb;
            actionFieldMerged = true;
        }

        if (string.IsNullOrWhiteSpace(currentResult.Action.Side) && !string.IsNullOrWhiteSpace(action.Side))
        {
            currentResult.Action.Side = action.Side;
            actionFieldMerged = true;
        }

        if (string.IsNullOrWhiteSpace(currentResult.Action.Target) && !string.IsNullOrWhiteSpace(action.Target))
        {
            currentResult.Action.Target = action.Target;
            actionFieldMerged = true;
        }

        if (!currentResult.Action.LinkedToPriorQuote.HasValue
            && TryParseBooleanString(action.LinkedToPriorQuote, out var linkedToPriorQuote))
        {
            currentResult.Action.LinkedToPriorQuote = linkedToPriorQuote;
            actionFieldMerged = true;
        }

        if (actionFieldMerged && currentResult.Provenance.ActionSource == FieldSourceType.None)
        {
            currentResult.Provenance.ActionSource = FieldSourceType.Derived;
        }
    }

    // ────────────────────────────────────

    /// <summary>
    /// Tries to parse a decimal from a string using invariant culture.
    /// </summary>
    /// <param name="value">The source string.</param>
    /// <param name="result">The parsed decimal.</param>
    /// <returns><c>true</c> if parsing succeeded; otherwise <c>false</c>.</returns>
    private static bool TryParseDecimalString(string value, out decimal result)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result = default;
            return false;
        }

        return decimal.TryParse(
            value,
            NumberStyles.Number | NumberStyles.AllowLeadingSign,
            CultureInfo.InvariantCulture,
            out result);
    }

    // ────────────────────────────────────

    /// <summary>
    /// Tries to parse a boolean from a string.
    /// </summary>
    /// <param name="value">The source string.</param>
    /// <param name="result">The parsed boolean.</param>
    /// <returns><c>true</c> if parsing succeeded; otherwise <c>false</c>.</returns>
    private static bool TryParseBooleanString(string value, out bool result)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result = default;
            return false;
        }

        return bool.TryParse(value, out result);
    }
}