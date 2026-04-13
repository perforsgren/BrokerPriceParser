namespace BrokerPriceParser.Core.Models;

/// <summary>
/// Represents directional market interest such as BUYER or SELLER.
/// This does not represent completed execution.
/// </summary>
public sealed class BrokerInterest
{
    /// <summary>
    /// Gets or sets the normalized interest side, for example BUYER or SELLER.
    /// </summary>
    public string Side { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets an optional human-readable description of the interest.
    /// </summary>
    public string Description { get; set; } = string.Empty;
}