using BrokerPriceParser.Core.Enums;

namespace BrokerPriceParser.Core.Models;

/// <summary>
/// Represents a quote extracted or resolved from a broker message.
/// </summary>
public sealed class BrokerQuote
{
    /// <summary>
    /// Gets or sets the bid value.
    /// </summary>
    public decimal? Bid { get; set; }

    /// <summary>
    /// Gets or sets the ask value.
    /// </summary>
    public decimal? Ask { get; set; }

    /// <summary>
    /// Gets or sets the mid value if applicable.
    /// </summary>
    public decimal? Mid { get; set; }

    /// <summary>
    /// Gets or sets the identified quote style.
    /// </summary>
    public QuoteStyle QuoteStyle { get; set; } = QuoteStyle.Unknown;

    /// <summary>
    /// Gets or sets a value indicating whether the quote appears firm.
    /// </summary>
    public bool? IsFirm { get; set; }
}