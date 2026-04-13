namespace BrokerPriceParser.Core.Review;

/// <summary>
/// Represents one replayable broker message record loaded from a file or sample source.
/// </summary>
public sealed class ReplayMessageRecord
{
    /// <summary>
    /// Gets or sets the replay sequence number.
    /// </summary>
    public int SequenceNumber { get; set; }

    /// <summary>
    /// Gets or sets the conversation identifier.
    /// </summary>
    public string ConversationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the source label.
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the broker label.
    /// </summary>
    public string Broker { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the raw text to replay.
    /// </summary>
    public string RawText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp used during replay.
    /// </summary>
    public DateTime ReceivedUtc { get; set; }
}