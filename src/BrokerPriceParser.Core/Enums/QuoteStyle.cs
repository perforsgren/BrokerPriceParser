namespace BrokerPriceParser.Core.Enums;

/// <summary>
/// Represents the quote presentation style found in the message.
/// </summary>
public enum QuoteStyle
{
    /// <summary>
    /// No quote style has been identified.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Two-way quote with bid and ask.
    /// </summary>
    TwoWay = 1,

    /// <summary>
    /// Single bid quote.
    /// </summary>
    BidOnly = 2,

    /// <summary>
    /// Single ask quote.
    /// </summary>
    AskOnly = 3,

    /// <summary>
    /// Mid or single neutral level.
    /// </summary>
    MidOnly = 4
}