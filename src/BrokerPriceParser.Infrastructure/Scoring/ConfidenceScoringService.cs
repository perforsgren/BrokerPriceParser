using BrokerPriceParser.Core.Contracts;
using BrokerPriceParser.Core.Enums;
using BrokerPriceParser.Core.Models;

namespace BrokerPriceParser.Infrastructure.Scoring;

/// <summary>
/// Calculates a simple confidence score for broker parser results.
/// </summary>
public sealed class ConfidenceScoringService : IConfidenceScoringService
{
    /// <summary>
    /// Calculates confidence for a broker parse result.
    /// </summary>
    /// <param name="result">The parse result.</param>
    /// <returns>A confidence score in the range 0.0 to 1.0.</returns>
    public double Calculate(BrokerParseResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        double score = 0.10;

        if (result.MessageType != BrokerMessageType.Unknown)
        {
            score += 0.15;
        }

        if (!string.IsNullOrWhiteSpace(result.Instrument.Pair))
        {
            score += 0.20;
        }

        if (!string.IsNullOrWhiteSpace(result.Instrument.Tenor))
        {
            score += 0.10;
        }

        if (!string.IsNullOrWhiteSpace(result.Instrument.Structure))
        {
            score += 0.10;
        }

        if (result.Quote.Bid.HasValue || result.Quote.Ask.HasValue || result.Quote.Mid.HasValue)
        {
            score += 0.15;
        }

        if (!string.IsNullOrWhiteSpace(result.Action.Verb))
        {
            score += 0.10;
        }

        if (result.ContextUsage.UsedContext)
        {
            score += 0.05;
        }

        if (result.Quality.ValidationErrors.Count > 0)
        {
            score -= 0.20;
        }

        if (result.Quality.AmbiguityFlags.Count > 0)
        {
            score -= 0.10;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }
}