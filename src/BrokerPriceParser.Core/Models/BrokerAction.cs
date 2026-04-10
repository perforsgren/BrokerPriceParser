namespace BrokerPriceParser.Core.Models;

/// <summary>
/// Represents an action or execution intent found in a broker message.
/// </summary>
public sealed class BrokerAction
{
    /// <summary>
    /// Gets or sets the normalized action verb, for example TAKE, HIT, MINE or LIFT.
    /// </summary>
    public string Verb { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the resolved side target, for example BID, ASK or MID.
    /// </summary>
    public string Side { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the action target description.
    /// </summary>
    public string Target { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the action was linked to a prior quote.
    /// </summary>
    public bool? LinkedToPriorQuote { get; set; }
}