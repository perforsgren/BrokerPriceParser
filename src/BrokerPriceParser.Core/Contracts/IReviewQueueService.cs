using BrokerPriceParser.Core.Models;
using BrokerPriceParser.Core.Review;

namespace BrokerPriceParser.Core.Contracts;

/// <summary>
/// Defines behavior for creating review queue items from parser results.
/// </summary>
public interface IReviewQueueService
{
    /// <summary>
    /// Creates a review queue item from replay input and parser output.
    /// </summary>
    /// <param name="replayRecord">The replay input record.</param>
    /// <param name="normalizedMessage">The normalized message.</param>
    /// <param name="result">The parser result.</param>
    /// <param name="lowConfidenceThreshold">The low-confidence review threshold.</param>
    /// <returns>A review queue item.</returns>
    ReviewQueueItem Create(
        ReplayMessageRecord replayRecord,
        NormalizedBrokerMessage normalizedMessage,
        BrokerParseResult result,
        double lowConfidenceThreshold);
}