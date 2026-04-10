using BrokerPriceParser.Core.Enums;

namespace BrokerPriceParser.Core.Models;

/// <summary>
/// Represents a normalized broker message used as input to classification and parsing.
/// </summary>
public sealed class NormalizedBrokerMessage
{
    /// <summary>
    /// Gets or sets the original raw message.
    /// </summary>
    public RawBrokerMessage RawMessage { get; set; } = new();

    /// <summary>
    /// Gets or sets the normalized message text.
    /// </summary>
    public string NormalizedText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the detected message type if known at this stage.
    /// </summary>
    public BrokerMessageType MessageTypeHint { get; set; } = BrokerMessageType.Unknown;

    /// <summary>
    /// Gets or sets extracted tokens useful for downstream parsing.
    /// </summary>
    public IReadOnlyList<string> Tokens { get; set; } = Array.Empty<string>();
}