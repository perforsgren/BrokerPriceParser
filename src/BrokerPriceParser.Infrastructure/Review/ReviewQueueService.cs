using System.Text;
using System.Text.Json;
using BrokerPriceParser.Core.Contracts;
using BrokerPriceParser.Core.Enums;
using BrokerPriceParser.Core.Models;
using BrokerPriceParser.Core.Review;

namespace BrokerPriceParser.Infrastructure.Review;

/// <summary>
/// Creates review queue items from replayed parser results.
/// </summary>
public sealed class ReviewQueueService : IReviewQueueService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    /// <summary>
    /// Creates a review queue item from replay input and parser output.
    /// </summary>
    /// <param name="replayRecord">The replay input record.</param>
    /// <param name="normalizedMessage">The normalized message.</param>
    /// <param name="result">The parser result.</param>
    /// <param name="lowConfidenceThreshold">The low-confidence review threshold.</param>
    /// <returns>A review queue item.</returns>
    public ReviewQueueItem Create(
        ReplayMessageRecord replayRecord,
        NormalizedBrokerMessage normalizedMessage,
        BrokerParseResult result,
        double lowConfidenceThreshold)
    {
        ArgumentNullException.ThrowIfNull(replayRecord);
        ArgumentNullException.ThrowIfNull(normalizedMessage);
        ArgumentNullException.ThrowIfNull(result);

        var reviewReasons = BuildReviewReasons(result, lowConfidenceThreshold);
        var requiresReview = reviewReasons.Count > 0;

        return new ReviewQueueItem
        {
            SequenceNumber = replayRecord.SequenceNumber,
            MessageId = result.MessageId,
            ConversationId = replayRecord.ConversationId,
            RawText = replayRecord.RawText,
            NormalizedText = normalizedMessage.NormalizedText,
            MessageType = result.MessageType,
            EventType = result.EventType,
            Confidence = result.Quality.Confidence,
            RequiresReview = requiresReview,
            ReviewReason = requiresReview ? string.Join(" | ", reviewReasons) : "No review required",
            ReviewStatus = ReviewStatus.Unreviewed,
            ResultJson = JsonSerializer.Serialize(result, SerializerOptions),
            ContextSummary = BuildContextSummary(result),
            ValidationErrorsText = result.Quality.ValidationErrors.Count > 0
                ? string.Join(Environment.NewLine, result.Quality.ValidationErrors)
                : "None",
            AmbiguityFlagsText = result.Quality.AmbiguityFlags.Count > 0
                ? string.Join(Environment.NewLine, result.Quality.AmbiguityFlags)
                : "None"
        };
    }

    // ────────────────────────────────────

    /// <summary>
    /// Builds review reasons from the parser result.
    /// </summary>
    /// <param name="result">The parser result.</param>
    /// <param name="lowConfidenceThreshold">The low-confidence threshold.</param>
    /// <returns>A review reason list.</returns>
    private static List<string> BuildReviewReasons(BrokerParseResult result, double lowConfidenceThreshold)
    {
        var reasons = new List<string>();

        if (result.MessageType == BrokerMessageType.Unknown)
        {
            reasons.Add("UnknownMessageType");
        }

        if (result.Quality.Confidence < lowConfidenceThreshold)
        {
            reasons.Add($"LowConfidence<{lowConfidenceThreshold:F2}");
        }

        if (result.Quality.ValidationErrors.Count > 0)
        {
            reasons.Add("ValidationErrors");
        }

        if (result.Quality.AmbiguityFlags.Count > 0)
        {
            reasons.Add("AmbiguityFlags");
        }

        return reasons;
    }

    // ────────────────────────────────────

    /// <summary>
    /// Builds a compact human-readable summary of the parse result.
    /// </summary>
    /// <param name="result">The parser result.</param>
    /// <returns>A context summary string.</returns>
    private static string BuildContextSummary(BrokerParseResult result)
    {
        var builder = new StringBuilder();

        builder.AppendLine(
            $"Instrument: Pair={ValueOrDash(result.Instrument.Pair)}, Tenor={ValueOrDash(result.Instrument.Tenor)}, Structure={ValueOrDash(result.Instrument.Structure)}, Delta={result.Instrument.Delta?.ToString() ?? "-"}");

        builder.AppendLine(
            $"Quote: Bid={result.Quote.Bid?.ToString() ?? "-"}, Ask={result.Quote.Ask?.ToString() ?? "-"}, Mid={result.Quote.Mid?.ToString() ?? "-"}, Style={result.Quote.QuoteStyle}, Firm={result.Quote.IsFirm?.ToString() ?? "-"}");

        builder.AppendLine(
            $"Action: Verb={ValueOrDash(result.Action.Verb)}, Side={ValueOrDash(result.Action.Side)}, Target={ValueOrDash(result.Action.Target)}, Linked={result.Action.LinkedToPriorQuote?.ToString() ?? "-"}");

        builder.AppendLine(
            $"Context: Used={result.ContextUsage.UsedContext}, Resolved={string.Join(", ", result.ContextUsage.ResolvedFromContext)}, Unresolved={string.Join(", ", result.ContextUsage.UnresolvedReferences)}");

        return builder.ToString().TrimEnd();
    }

    // ────────────────────────────────────

    /// <summary>
    /// Returns a dash for blank values.
    /// </summary>
    /// <param name="value">The source value.</param>
    /// <returns>The original value or a dash.</returns>
    private static string ValueOrDash(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "-" : value;
    }
}