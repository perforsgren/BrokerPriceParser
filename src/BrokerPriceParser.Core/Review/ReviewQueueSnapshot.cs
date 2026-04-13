namespace BrokerPriceParser.Core.Review;

/// <summary>
/// Represents a persisted snapshot of the review queue.
/// </summary>
public sealed class ReviewQueueSnapshot
{
    /// <summary>
    /// Gets or sets the UTC timestamp when the snapshot was saved.
    /// </summary>
    public DateTime SavedUtc { get; set; }

    /// <summary>
    /// Gets or sets the application schema version.
    /// </summary>
    public string SchemaVersion { get; set; } = "1.0";

    /// <summary>
    /// Gets or sets the saved review queue items.
    /// </summary>
    public IReadOnlyList<ReviewQueueItem> Items { get; set; } = Array.Empty<ReviewQueueItem>();
}