using BrokerPriceParser.Core.Enums;

namespace BrokerPriceParser.Core.Review;

/// <summary>
/// Represents one review queue item shown in the WPF replay tool.
/// </summary>
public sealed class ReviewQueueItem
{
    /// <summary>
    /// Gets or sets the replay sequence number.
    /// </summary>
    public int SequenceNumber { get; set; }

    /// <summary>
    /// Gets or sets the message identifier.
    /// </summary>
    public string MessageId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the conversation identifier.
    /// </summary>
    public string ConversationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the raw message text.
    /// </summary>
    public string RawText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the normalized message text.
    /// </summary>
    public string NormalizedText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the parsed message type.
    /// </summary>
    public BrokerMessageType MessageType { get; set; } = BrokerMessageType.Unknown;

    /// <summary>
    /// Gets or sets the parsed event type.
    /// </summary>
    public BrokerEventType EventType { get; set; } = BrokerEventType.None;

    /// <summary>
    /// Gets or sets the parser confidence score.
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the item should be reviewed.
    /// </summary>
    public bool RequiresReview { get; set; }

    /// <summary>
    /// Gets or sets the review reason summary.
    /// </summary>
    public string ReviewReason { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the manual review status.
    /// </summary>
    public ReviewStatus ReviewStatus { get; set; } = ReviewStatus.Unreviewed;

    /// <summary>
    /// Gets or sets the original parser result JSON captured before any manual edits.
    /// </summary>
    public string OriginalResultJson { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current parser result JSON, including manual overrides if applied.
    /// </summary>
    public string CurrentResultJson { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether a manual override has been applied.
    /// </summary>
    public bool HasManualOverride { get; set; }

    /// <summary>
    /// Gets or sets optional manual notes written during review.
    /// </summary>
    public string ManualNotes { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the human-readable context summary.
    /// </summary>
    public string ContextSummary { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the validation error summary.
    /// </summary>
    public string ValidationErrorsText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ambiguity flag summary.
    /// </summary>
    public string AmbiguityFlagsText { get; set; } = string.Empty;
}