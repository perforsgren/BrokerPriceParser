using BrokerPriceParser.Core.Review;

namespace BrokerPriceParser.Core.Contracts;

/// <summary>
/// Defines behavior for exporting reviewed items as gold labels.
/// </summary>
public interface IGoldLabelExportService
{
    /// <summary>
    /// Exports reviewed queue items to a JSONL gold-label file.
    /// </summary>
    /// <param name="filePath">The output file path.</param>
    /// <param name="items">The review queue items to export.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task ExportAsync(
        string filePath,
        IReadOnlyList<ReviewQueueItem> items,
        CancellationToken cancellationToken = default);
}