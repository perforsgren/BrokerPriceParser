using System.Globalization;
using System.Text.RegularExpressions;
using BrokerPriceParser.Core.Contracts;
using BrokerPriceParser.Core.Enums;
using BrokerPriceParser.Core.Models;
using BrokerPriceParser.Core.State;

namespace BrokerPriceParser.Infrastructure.Parsing;

/// <summary>
/// Resolves broker parse results against the current conversation state.
/// </summary>
public sealed class ConversationContextResolver : IConversationContextResolver
{
    /// <summary>
    /// Resolves a partially parsed broker result using normalized input and conversation state.
    /// </summary>
    /// <param name="context">The parse context.</param>
    /// <param name="currentResult">The current parse result.</param>
    /// <returns>The resolved parse result.</returns>
    public BrokerParseResult Resolve(ParseContext context, BrokerParseResult currentResult)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(currentResult);

        var resolvedFields = new List<string>();
        var unresolvedReferences = new List<string>();

        ApplyExplicitInstrumentHints(context.NormalizedMessage, currentResult);
        ExtractExplicitQuote(context.NormalizedMessage.NormalizedText, currentResult);
        ExtractExplicitAction(context.NormalizedMessage.NormalizedText, currentResult);
        ApplyQuoteFirmness(context.NormalizedMessage.NormalizedText, currentResult);
        InheritInstrumentFromState(context, currentResult, resolvedFields);
        ResolvePriceUpdateFromState(context, currentResult, resolvedFields, unresolvedReferences);
        ResolveActionFromState(context, currentResult, resolvedFields, unresolvedReferences);

        currentResult.ContextUsage = new ContextUsage
        {
            UsedContext = resolvedFields.Count > 0,
            ResolvedFromContext = resolvedFields,
            UnresolvedReferences = unresolvedReferences
        };

        return currentResult;
    }

    // ────────────────────────────────────

    /// <summary>
    /// Applies explicit instrument hints produced by the normalization layer.
    /// </summary>
    /// <param name="message">The normalized message.</param>
    /// <param name="result">The result to update.</param>
    private static void ApplyExplicitInstrumentHints(NormalizedBrokerMessage message, BrokerParseResult result)
    {
        if (!string.IsNullOrWhiteSpace(message.DetectedCurrencyPair))
        {
            result.Instrument.Pair = message.DetectedCurrencyPair;
            result.Provenance.PairSource = FieldSourceType.Explicit;
        }

        if (!string.IsNullOrWhiteSpace(message.DetectedTenor))
        {
            result.Instrument.Tenor = message.DetectedTenor;
            result.Provenance.TenorSource = FieldSourceType.Explicit;
        }

        if (!string.IsNullOrWhiteSpace(message.DetectedStructure))
        {
            result.Instrument.Structure = message.DetectedStructure;
            result.Provenance.StructureSource = FieldSourceType.Explicit;

            if (message.DetectedStructure is "ATM" or "ATMF")
            {
                result.Instrument.StrikeType = message.DetectedStructure;
            }
        }

        if (message.DetectedDelta.HasValue)
        {
            result.Instrument.Delta = message.DetectedDelta.Value;
        }
    }

    // ────────────────────────────────────

    /// <summary>
    /// Extracts an explicit quote from normalized text when present.
    /// </summary>
    /// <param name="text">The normalized text.</param>
    /// <param name="result">The result to update.</param>
    private static void ExtractExplicitQuote(string text, BrokerParseResult result)
    {
        if (TryExtractTwoWayQuote(text, out var bid, out var ask))
        {
            result.Quote.Bid = bid;
            result.Quote.Ask = ask;
            result.Quote.QuoteStyle = QuoteStyle.TwoWay;
            result.Provenance.PriceSource = FieldSourceType.Explicit;
            return;
        }

        if (TryExtractOneWayQuote(text, out var side, out var value))
        {
            switch (side)
            {
                case "BID":
                    result.Quote.Bid = value;
                    result.Quote.QuoteStyle = QuoteStyle.BidOnly;
                    break;

                case "ASK":
                    result.Quote.Ask = value;
                    result.Quote.QuoteStyle = QuoteStyle.AskOnly;
                    break;

                case "MID":
                    result.Quote.Mid = value;
                    result.Quote.QuoteStyle = QuoteStyle.MidOnly;
                    break;
            }

            result.Provenance.PriceSource = FieldSourceType.Explicit;
        }
    }

    // ────────────────────────────────────

    /// <summary>
    /// Extracts an explicit action verb and optional explicit action price from normalized text.
    /// </summary>
    /// <param name="text">The normalized text.</param>
    /// <param name="result">The result to update.</param>
    private static void ExtractExplicitAction(string text, BrokerParseResult result)
    {
        if (!TryExtractActionVerb(text, out var verb, out var side))
        {
            return;
        }

        result.Action.Verb = verb;
        result.Action.Side = side;
        result.Provenance.ActionSource = FieldSourceType.Explicit;

        if (!TryExtractStandalonePrice(text, out var explicitPrice))
        {
            return;
        }

        switch (side)
        {
            case "ASK":
                result.Quote.Ask = explicitPrice;
                result.Quote.QuoteStyle = QuoteStyle.AskOnly;
                result.Provenance.PriceSource = FieldSourceType.Explicit;
                result.Action.Target = FormatDecimal(explicitPrice);
                break;

            case "BID":
                result.Quote.Bid = explicitPrice;
                result.Quote.QuoteStyle = QuoteStyle.BidOnly;
                result.Provenance.PriceSource = FieldSourceType.Explicit;
                result.Action.Target = FormatDecimal(explicitPrice);
                break;

            case "MID":
                result.Quote.Mid = explicitPrice;
                result.Quote.QuoteStyle = QuoteStyle.MidOnly;
                result.Provenance.PriceSource = FieldSourceType.Explicit;
                result.Action.Target = FormatDecimal(explicitPrice);
                break;
        }
    }

    // ────────────────────────────────────

    /// <summary>
    /// Applies quote firmness markers such as GOOD, FIRM or INDICATIVE when a quote is present.
    /// </summary>
    /// <param name="text">The normalized text.</param>
    /// <param name="result">The result to update.</param>
    private static void ApplyQuoteFirmness(string text, BrokerParseResult result)
    {
        if (!ContainsQuoteData(result.Quote))
        {
            return;
        }

        if (Regex.IsMatch(text, @"\b(IND|INDICATIVE)\b"))
        {
            result.Quote.IsFirm = false;
            return;
        }

        if (Regex.IsMatch(text, @"\b(GOOD|FIRM|CAN DO|WORKABLE)\b"))
        {
            result.Quote.IsFirm = true;
        }
    }

    // ────────────────────────────────────

    /// <summary>
    /// Inherits missing instrument fields from conversation state where appropriate.
    /// </summary>
    /// <param name="context">The parse context.</param>
    /// <param name="result">The result to update.</param>
    /// <param name="resolvedFields">The resolved field list.</param>
    private static void InheritInstrumentFromState(
        ParseContext context,
        BrokerParseResult result,
        ICollection<string> resolvedFields)
    {
        if (result.MessageType is BrokerMessageType.Noise or BrokerMessageType.Unknown)
        {
            return;
        }

        var stateInstrument = context.ConversationState.CurrentInstrument;

        if (string.IsNullOrWhiteSpace(result.Instrument.Pair) && !string.IsNullOrWhiteSpace(stateInstrument.Pair))
        {
            result.Instrument.Pair = stateInstrument.Pair;
            result.Provenance.PairSource = FieldSourceType.InferredFromContext;
            resolvedFields.Add("Instrument.Pair");
        }

        if (string.IsNullOrWhiteSpace(result.Instrument.Tenor) && !string.IsNullOrWhiteSpace(stateInstrument.Tenor))
        {
            result.Instrument.Tenor = stateInstrument.Tenor;
            result.Provenance.TenorSource = FieldSourceType.InferredFromContext;
            resolvedFields.Add("Instrument.Tenor");
        }

        if (string.IsNullOrWhiteSpace(result.Instrument.Structure) && !string.IsNullOrWhiteSpace(stateInstrument.Structure))
        {
            result.Instrument.Structure = stateInstrument.Structure;
            result.Provenance.StructureSource = FieldSourceType.InferredFromContext;
            resolvedFields.Add("Instrument.Structure");
        }

        if (!result.Instrument.Delta.HasValue && stateInstrument.Delta.HasValue)
        {
            result.Instrument.Delta = stateInstrument.Delta.Value;
            resolvedFields.Add("Instrument.Delta");
        }

        if (string.IsNullOrWhiteSpace(result.Instrument.StrikeType) && !string.IsNullOrWhiteSpace(stateInstrument.StrikeType))
        {
            result.Instrument.StrikeType = stateInstrument.StrikeType;
            resolvedFields.Add("Instrument.StrikeType");
        }
    }

    // ────────────────────────────────────

    /// <summary>
    /// Resolves state-dependent quote update messages such as FLAT BID.
    /// </summary>
    /// <param name="context">The parse context.</param>
    /// <param name="result">The result to update.</param>
    /// <param name="resolvedFields">The resolved field list.</param>
    /// <param name="unresolvedReferences">The unresolved reference list.</param>
    private static void ResolvePriceUpdateFromState(
        ParseContext context,
        BrokerParseResult result,
        ICollection<string> resolvedFields,
        ICollection<string> unresolvedReferences)
    {
        if (result.MessageType != BrokerMessageType.PriceUpdate)
        {
            return;
        }

        if (ContainsQuoteData(result.Quote))
        {
            return;
        }

        var text = context.NormalizedMessage.NormalizedText;
        var latestQuote = context.ConversationState.LatestQuote;

        if (Regex.IsMatch(text, @"\bFLAT\s+BID\b"))
        {
            if (latestQuote.Bid.HasValue)
            {
                result.Quote.Bid = latestQuote.Bid.Value;
                result.Quote.QuoteStyle = QuoteStyle.BidOnly;
                result.Quote.IsFirm = latestQuote.IsFirm;
                result.Provenance.PriceSource = FieldSourceType.InferredFromContext;
                resolvedFields.Add("Quote.Bid");
            }
            else
            {
                unresolvedReferences.Add("FlatBidWithoutPriorBid");
            }

            return;
        }

        if (Regex.IsMatch(text, @"\bFLAT\s+(ASK|OFFER|OFR)\b"))
        {
            if (latestQuote.Ask.HasValue)
            {
                result.Quote.Ask = latestQuote.Ask.Value;
                result.Quote.QuoteStyle = QuoteStyle.AskOnly;
                result.Quote.IsFirm = latestQuote.IsFirm;
                result.Provenance.PriceSource = FieldSourceType.InferredFromContext;
                resolvedFields.Add("Quote.Ask");
            }
            else
            {
                unresolvedReferences.Add("FlatAskWithoutPriorAsk");
            }

            return;
        }

        if (Regex.IsMatch(text, @"\bFLAT\s+MID\b"))
        {
            if (latestQuote.Mid.HasValue)
            {
                result.Quote.Mid = latestQuote.Mid.Value;
                result.Quote.QuoteStyle = QuoteStyle.MidOnly;
                result.Quote.IsFirm = latestQuote.IsFirm;
                result.Provenance.PriceSource = FieldSourceType.InferredFromContext;
                resolvedFields.Add("Quote.Mid");
            }
            else
            {
                unresolvedReferences.Add("FlatMidWithoutPriorMid");
            }
        }
    }

    // ────────────────────────────────────

    /// <summary>
    /// Resolves action messages against the latest known quote in conversation state.
    /// </summary>
    /// <param name="context">The parse context.</param>
    /// <param name="result">The result to update.</param>
    /// <param name="resolvedFields">The resolved field list.</param>
    /// <param name="unresolvedReferences">The unresolved reference list.</param>
    private static void ResolveActionFromState(
        ParseContext context,
        BrokerParseResult result,
        ICollection<string> resolvedFields,
        ICollection<string> unresolvedReferences)
    {
        if (result.MessageType != BrokerMessageType.ActionIntent)
        {
            return;
        }

        var latestQuote = context.ConversationState.LatestQuote;
        var hasExplicitActionPrice = ContainsQuoteData(result.Quote);

        if (string.IsNullOrWhiteSpace(result.Action.Side))
        {
            InferActionSideFromOneWayState(latestQuote, result, resolvedFields);
        }

        if (hasExplicitActionPrice)
        {
            result.Action.LinkedToPriorQuote = false;

            if (string.IsNullOrWhiteSpace(result.Action.Target))
            {
                result.Action.Target = ResolveExplicitPriceTarget(result.Quote);
            }

            return;
        }

        var hasPriorQuote = ContainsQuoteData(latestQuote);

        if (!hasPriorQuote)
        {
            result.Action.LinkedToPriorQuote = false;
            unresolvedReferences.Add("ActionWithoutPriorQuote");
            return;
        }

        result.Action.LinkedToPriorQuote = true;
        resolvedFields.Add("Action.LinkedToPriorQuote");

        switch (result.Action.Side)
        {
            case "ASK":
                if (latestQuote.Ask.HasValue)
                {
                    result.Quote.Ask = latestQuote.Ask.Value;
                    result.Quote.QuoteStyle = QuoteStyle.AskOnly;
                    result.Quote.IsFirm = latestQuote.IsFirm;
                    result.Provenance.PriceSource = FieldSourceType.InferredFromContext;
                    result.Action.Target = "LATEST_ASK";
                    resolvedFields.Add("Quote.Ask");
                }
                else
                {
                    unresolvedReferences.Add("AskActionWithoutPriorAsk");
                }

                break;

            case "BID":
                if (latestQuote.Bid.HasValue)
                {
                    result.Quote.Bid = latestQuote.Bid.Value;
                    result.Quote.QuoteStyle = QuoteStyle.BidOnly;
                    result.Quote.IsFirm = latestQuote.IsFirm;
                    result.Provenance.PriceSource = FieldSourceType.InferredFromContext;
                    result.Action.Target = "LATEST_BID";
                    resolvedFields.Add("Quote.Bid");
                }
                else
                {
                    unresolvedReferences.Add("BidActionWithoutPriorBid");
                }

                break;

            case "MID":
                if (latestQuote.Mid.HasValue)
                {
                    result.Quote.Mid = latestQuote.Mid.Value;
                    result.Quote.QuoteStyle = QuoteStyle.MidOnly;
                    result.Quote.IsFirm = latestQuote.IsFirm;
                    result.Provenance.PriceSource = FieldSourceType.InferredFromContext;
                    result.Action.Target = "LATEST_MID";
                    resolvedFields.Add("Quote.Mid");
                }
                else
                {
                    unresolvedReferences.Add("MidActionWithoutPriorMid");
                }

                break;

            default:
                unresolvedReferences.Add("DoneWithoutResolvedSide");
                break;
        }
    }

    // ────────────────────────────────────

    /// <summary>
    /// Tries to infer an action side from a one-way quote currently stored in state.
    /// </summary>
    /// <param name="latestQuote">The latest quote in state.</param>
    /// <param name="result">The result to update.</param>
    /// <param name="resolvedFields">The resolved field list.</param>
    private static void InferActionSideFromOneWayState(
        BrokerQuote latestQuote,
        BrokerParseResult result,
        ICollection<string> resolvedFields)
    {
        var hasOnlyBid = latestQuote.Bid.HasValue && !latestQuote.Ask.HasValue && !latestQuote.Mid.HasValue;
        var hasOnlyAsk = latestQuote.Ask.HasValue && !latestQuote.Bid.HasValue && !latestQuote.Mid.HasValue;
        var hasOnlyMid = latestQuote.Mid.HasValue && !latestQuote.Bid.HasValue && !latestQuote.Ask.HasValue;

        if (hasOnlyBid)
        {
            result.Action.Side = "BID";
            resolvedFields.Add("Action.Side");
        }
        else if (hasOnlyAsk)
        {
            result.Action.Side = "ASK";
            resolvedFields.Add("Action.Side");
        }
        else if (hasOnlyMid)
        {
            result.Action.Side = "MID";
            resolvedFields.Add("Action.Side");
        }
    }

    // ────────────────────────────────────

    /// <summary>
    /// Tries to extract a two-way quote from normalized text.
    /// </summary>
    /// <param name="text">The normalized text.</param>
    /// <param name="bid">The extracted bid.</param>
    /// <param name="ask">The extracted ask.</param>
    /// <returns><c>true</c> if extraction succeeded; otherwise <c>false</c>.</returns>
    private static bool TryExtractTwoWayQuote(string text, out decimal bid, out decimal ask)
    {
        var match = Regex.Match(text, @"(?<!\d)(-?\d+(?:\.\d+)?)\s*/\s*(-?\d+(?:\.\d+)?)");

        if (match.Success
            && TryParseDecimalInvariant(match.Groups[1].Value, out bid)
            && TryParseDecimalInvariant(match.Groups[2].Value, out ask))
        {
            return true;
        }

        bid = default;
        ask = default;
        return false;
    }

    // ────────────────────────────────────

    /// <summary>
    /// Tries to extract a one-way quote from normalized text.
    /// </summary>
    /// <param name="text">The normalized text.</param>
    /// <param name="side">The extracted normalized side.</param>
    /// <param name="value">The extracted quote value.</param>
    /// <returns><c>true</c> if extraction succeeded; otherwise <c>false</c>.</returns>
    private static bool TryExtractOneWayQuote(string text, out string side, out decimal value)
    {
        var prefixMatch = Regex.Match(text, @"\b(BID|ASK|OFFER|OFR|MID|PAYING|OFFERING)\b\s*(-?\d+(?:\.\d+)?)");

        if (prefixMatch.Success && TryParseDecimalInvariant(prefixMatch.Groups[2].Value, out value))
        {
            side = NormalizeQuoteSide(prefixMatch.Groups[1].Value);
            return !string.IsNullOrWhiteSpace(side);
        }

        var suffixMatch = Regex.Match(text, @"(-?\d+(?:\.\d+)?)\s*\b(BID|ASK|OFFER|OFR|MID)\b");

        if (suffixMatch.Success && TryParseDecimalInvariant(suffixMatch.Groups[1].Value, out value))
        {
            side = NormalizeQuoteSide(suffixMatch.Groups[2].Value);
            return !string.IsNullOrWhiteSpace(side);
        }

        side = string.Empty;
        value = default;
        return false;
    }

    // ────────────────────────────────────

    /// <summary>
    /// Tries to extract a normalized action verb and default side from normalized text.
    /// </summary>
    /// <param name="text">The normalized text.</param>
    /// <param name="verb">The normalized verb.</param>
    /// <param name="side">The default normalized side.</param>
    /// <returns><c>true</c> if extraction succeeded; otherwise <c>false</c>.</returns>
    private static bool TryExtractActionVerb(string text, out string verb, out string side)
    {
        if (Regex.IsMatch(text, @"\b(TAKE|TAK|MINE|LIFT|PAID)\b"))
        {
            verb = Regex.IsMatch(text, @"\bMINE\b") ? "MINE"
                : Regex.IsMatch(text, @"\bLIFT\b") ? "LIFT"
                : Regex.IsMatch(text, @"\bPAID\b") ? "PAID"
                : "TAKE";

            side = "ASK";
            return true;
        }

        if (Regex.IsMatch(text, @"\b(HIT|SOLD|YOURS)\b"))
        {
            verb = Regex.IsMatch(text, @"\bSOLD\b") ? "SOLD"
                : Regex.IsMatch(text, @"\bYOURS\b") ? "YOURS"
                : "HIT";

            side = "BID";
            return true;
        }

        if (Regex.IsMatch(text, @"\bDONE\b"))
        {
            verb = "DONE";

            if (Regex.IsMatch(text, @"\b(BID)\b"))
            {
                side = "BID";
            }
            else if (Regex.IsMatch(text, @"\b(ASK|OFFER|OFR)\b"))
            {
                side = "ASK";
            }
            else if (Regex.IsMatch(text, @"\bMID\b"))
            {
                side = "MID";
            }
            else
            {
                side = string.Empty;
            }

            return true;
        }

        verb = string.Empty;
        side = string.Empty;
        return false;
    }

    // ────────────────────────────────────

    /// <summary>
    /// Tries to extract a standalone decimal price from normalized text.
    /// </summary>
    /// <param name="text">The normalized text.</param>
    /// <param name="value">The extracted value.</param>
    /// <returns><c>true</c> if a price was extracted; otherwise <c>false</c>.</returns>
    private static bool TryExtractStandalonePrice(string text, out decimal value)
    {
        var match = Regex.Match(text, @"(?<!/)(?<!\d)(-?\d+(?:\.\d+)?)(?!\s*/)(?!/\d)");

        if (match.Success && TryParseDecimalInvariant(match.Groups[1].Value, out value))
        {
            return true;
        }

        value = default;
        return false;
    }

    // ────────────────────────────────────

    /// <summary>
    /// Normalizes quote-side aliases into BID, ASK or MID.
    /// </summary>
    /// <param name="rawSide">The raw side token.</param>
    /// <returns>The normalized side.</returns>
    private static string NormalizeQuoteSide(string rawSide)
    {
        return rawSide switch
        {
            "BID" => "BID",
            "ASK" => "ASK",
            "OFFER" => "ASK",
            "OFR" => "ASK",
            "MID" => "MID",
            "PAYING" => "BID",
            "OFFERING" => "ASK",
            _ => string.Empty
        };
    }

    // ────────────────────────────────────

    /// <summary>
    /// Resolves the explicit price target string from a quote snapshot.
    /// </summary>
    /// <param name="quote">The quote snapshot.</param>
    /// <returns>The resolved target string.</returns>
    private static string ResolveExplicitPriceTarget(BrokerQuote quote)
    {
        if (quote.Ask.HasValue)
        {
            return FormatDecimal(quote.Ask.Value);
        }

        if (quote.Bid.HasValue)
        {
            return FormatDecimal(quote.Bid.Value);
        }

        if (quote.Mid.HasValue)
        {
            return FormatDecimal(quote.Mid.Value);
        }

        return string.Empty;
    }

    // ────────────────────────────────────

    /// <summary>
    /// Determines whether a quote contains any meaningful data.
    /// </summary>
    /// <param name="quote">The quote to inspect.</param>
    /// <returns><c>true</c> if the quote contains data; otherwise <c>false</c>.</returns>
    private static bool ContainsQuoteData(BrokerQuote quote)
    {
        return quote.Bid.HasValue
            || quote.Ask.HasValue
            || quote.Mid.HasValue
            || quote.IsFirm.HasValue
            || quote.QuoteStyle != QuoteStyle.Unknown;
    }

    // ────────────────────────────────────

    /// <summary>
    /// Parses a decimal value using invariant culture.
    /// </summary>
    /// <param name="text">The text to parse.</param>
    /// <param name="value">The parsed value.</param>
    /// <returns><c>true</c> if parsing succeeded; otherwise <c>false</c>.</returns>
    private static bool TryParseDecimalInvariant(string text, out decimal value)
    {
        return decimal.TryParse(
            text,
            NumberStyles.Number | NumberStyles.AllowLeadingSign,
            CultureInfo.InvariantCulture,
            out value);
    }

    // ────────────────────────────────────

    /// <summary>
    /// Formats a decimal value using invariant culture without unnecessary trailing zeroes.
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <returns>The formatted value.</returns>
    private static string FormatDecimal(decimal value)
    {
        return value.ToString("0.################", CultureInfo.InvariantCulture);
    }
}