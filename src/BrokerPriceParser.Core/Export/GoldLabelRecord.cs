using BrokerPriceParser.Core.Models;
using BrokerPriceParser.Core.Review;

namespace BrokerPriceParser.Core.Export;

/// <summary>
/// Represents one exported gold-label record for training or evaluation.
/// </summary>
public sealed class GoldLabelRecord
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
    /// Gets or sets the review status.
    /// </summary>
    public ReviewStatus ReviewStatus { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a manual override was applied.
    /// </summary>
    public bool HasManualOverride { get; set; }

    /// <summary>
    /// Gets or sets optional manual notes.
    /// </summary>
    public string ManualNotes { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the final structured broker parse result.
    /// </summary>
    public BrokerParseResult Result { get; set; } = new();
}