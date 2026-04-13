namespace BrokerPriceParser.Core.Review;

/// <summary>
/// Represents the manual review status for a replay queue item.
/// </summary>
public enum ReviewStatus
{
    /// <summary>
    /// The item has not been reviewed yet.
    /// </summary>
    Unreviewed = 0,

    /// <summary>
    /// The parsed result has been accepted as-is.
    /// </summary>
    Accepted = 1,

    /// <summary>
    /// The parsed result required manual correction.
    /// </summary>
    Corrected = 2,

    /// <summary>
    /// The item has been intentionally ignored.
    /// </summary>
    Ignored = 3
}