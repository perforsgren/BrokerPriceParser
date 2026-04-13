using BrokerPriceParser.Core.Review;

namespace BrokerPriceParser.Core.Contracts;

/// <summary>
/// Defines behavior for saving and loading persisted review decisions.
/// </summary>
public interface IReviewDecisionPersistenceService
{
    /// <summary>
    /// Saves the current review queue snapshot to a file.
    /// </summary>
    /// <param name="filePath">The output file path.</param>
    /// <param name="items">The review queue items to save.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task SaveAsync(
        string filePath,
        IReadOnlyList<ReviewQueueItem> items,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a persisted review queue snapshot from a file.
    /// </summary>
    /// <param name="filePath">The input file path.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The loaded review queue items.</returns>
    Task<IReadOnlyList<ReviewQueueItem>> LoadAsync(
        string filePath,
        CancellationToken cancellationToken = default);
}