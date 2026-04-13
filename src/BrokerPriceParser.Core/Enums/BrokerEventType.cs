namespace BrokerPriceParser.Core.Enums;

/// <summary>
/// Represents the semantic event resolved from a broker message.
/// </summary>
public enum BrokerEventType
{
    /// <summary>
    /// No event has been resolved.
    /// </summary>
    None = 0,

    /// <summary>
    /// A request for a market or quote.
    /// </summary>
    RequestMarket = 1,

    /// <summary>
    /// A quote has been provided.
    /// </summary>
    QuoteProvided = 2,

    /// <summary>
    /// A previous quote has been revised.
    /// </summary>
    QuoteRevised = 3,

    /// <summary>
    /// An offer has been lifted.
    /// </summary>
    LiftOffer = 4,

    /// <summary>
    /// A bid has been hit.
    /// </summary>
    HitBid = 5,

    /// <summary>
    /// A quote has been cancelled or withdrawn.
    /// </summary>
    QuoteCancelled = 6,

    /// <summary>
    /// The message requests clarification.
    /// </summary>
    ClarificationRequested = 7,

    /// <summary>
    /// The message indicates directional market interest.
    /// </summary>
    MarketInterestIndicated = 8
}