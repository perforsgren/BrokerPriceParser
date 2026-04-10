namespace BrokerPriceParser.Core.Models;

/// <summary>
/// Represents a raw inbound broker message before any normalization or parsing.
/// </summary>
public sealed class RawBrokerMessage
{
    /// <summary>
    /// Gets or sets the unique message identifier.
    /// </summary>
    public string MessageId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the conversation identifier used to group related messages.
    /// </summary>
    public string ConversationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the source system or channel name.
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the broker or counterparty display name.
    /// </summary>
    public string Broker { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the raw text exactly as received.
    /// </summary>
    public string RawText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC timestamp when the message was received.
    /// </summary>
    public DateTime ReceivedUtc { get; set; }
}