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

    /// <summary>
    /// Gets or sets the detected normalized currency pair, for example NOKSEK.
    /// </summary>
    public string DetectedCurrencyPair { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the detected tenor, for example 1W, 3M or 1Y.
    /// </summary>
    public string DetectedTenor { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the detected structure hint, for example RR, BF, ATM or ATMF.
    /// </summary>
    public string DetectedStructure { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the detected delta if present.
    /// </summary>
    public decimal? DetectedDelta { get; set; }

    /// <summary>
    /// Gets or sets the list of normalization rules that were applied.
    /// </summary>
    public IReadOnlyList<string> AppliedNormalizationRules { get; set; } = Array.Empty<string>();
}