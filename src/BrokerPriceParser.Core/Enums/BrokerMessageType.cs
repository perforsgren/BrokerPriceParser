namespace BrokerPriceParser.Core.Enums;

/// <summary>
/// Represents the high-level classification of a broker message.
/// </summary>
public enum BrokerMessageType
{
    /// <summary>
    /// The message type could not be determined.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// The message is primarily an instrument or price request.
    /// </summary>
    InstrumentRequest = 1,

    /// <summary>
    /// The message contains a quote or indicative price.
    /// </summary>
    PriceQuote = 2,

    /// <summary>
    /// The message updates an earlier quote.
    /// </summary>
    PriceUpdate = 3,

    /// <summary>
    /// The message expresses an action or execution intent.
    /// </summary>
    ActionIntent = 4,

    /// <summary>
    /// The message is primarily clarification or follow-up.
    /// </summary>
    Clarification = 5,

    /// <summary>
    /// The message contains conversational noise or irrelevant content.
    /// </summary>
    Noise = 6,

    /// <summary>
    /// The message indicates directional market interest such as BUYER or SELLER.
    /// </summary>
    InterestIndication = 7
}