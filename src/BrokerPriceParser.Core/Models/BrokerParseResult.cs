using BrokerPriceParser.Core.Enums;
using BrokerPriceParser.Core.Validation;

namespace BrokerPriceParser.Core.Models;

/// <summary>
/// Represents the full structured result of parsing a broker message.
/// </summary>
public sealed class BrokerParseResult
{
    /// <summary>
    /// Gets or sets the message identifier.
    /// </summary>
    public string MessageId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the resolved high-level message type.
    /// </summary>
    public BrokerMessageType MessageType { get; set; } = BrokerMessageType.Unknown;

    /// <summary>
    /// Gets or sets the resolved semantic event type.
    /// </summary>
    public BrokerEventType EventType { get; set; } = BrokerEventType.None;

    /// <summary>
    /// Gets or sets the original raw message text.
    /// </summary>
    public string RawMessage { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the normalized message text.
    /// </summary>
    public string NormalizedMessage { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the resolved instrument.
    /// </summary>
    public BrokerInstrument Instrument { get; set; } = new();

    /// <summary>
    /// Gets or sets the resolved quote.
    /// </summary>
    public BrokerQuote Quote { get; set; } = new();

    /// <summary>
    /// Gets or sets the resolved action.
    /// </summary>
    public BrokerAction Action { get; set; } = new();

    /// <summary>
    /// Gets or sets the resolved market interest.
    /// </summary>
    public BrokerInterest Interest { get; set; } = new();

    /// <summary>
    /// Gets or sets context usage information.
    /// </summary>
    public ContextUsage ContextUsage { get; set; } = new();

    /// <summary>
    /// Gets or sets field provenance information.
    /// </summary>
    public FieldProvenance Provenance { get; set; } = new();

    /// <summary>
    /// Gets or sets parse quality information.
    /// </summary>
    public BrokerParseQuality Quality { get; set; } = new();
}