namespace BrokerPriceParser.Core.Models;

/// <summary>
/// Represents the instrument information resolved from a broker message.
/// </summary>
public sealed class BrokerInstrument
{
    /// <summary>
    /// Gets or sets the normalized six-letter currency pair, for example NOKSEK.
    /// </summary>
    public string Pair { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the tenor, for example 1W, 3M or 1Y.
    /// </summary>
    public string Tenor { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the concrete expiry if explicitly resolved.
    /// </summary>
    public string Expiry { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the structure, for example RR, BF, ATM, VANILLA.
    /// </summary>
    public string Structure { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the delta if applicable.
    /// </summary>
    public decimal? Delta { get; set; }

    /// <summary>
    /// Gets or sets the strike type, for example ATM or ATMF.
    /// </summary>
    public string StrikeType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the strike representation if applicable.
    /// </summary>
    public string Strike { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the option-side bias, for example NOK_PUT or SEK_CALL if identified.
    /// </summary>
    public string OptionSideBias { get; set; } = string.Empty;
}