using BrokerPriceParser.Core.Models;

namespace BrokerPriceParser.Core.State;

/// <summary>
/// Represents the current resolved state for a conversation or broker thread.
/// </summary>
public sealed class ConversationState
{
    /// <summary>
    /// Gets or sets the conversation identifier.
    /// </summary>
    public string ConversationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current instrument in scope.
    /// </summary>
    public BrokerInstrument CurrentInstrument { get; set; } = new();

    /// <summary>
    /// Gets or sets the latest known quote in scope.
    /// </summary>
    public BrokerQuote LatestQuote { get; set; } = new();

    /// <summary>
    /// Gets or sets the latest parsed action in scope.
    /// </summary>
    public BrokerAction LatestAction { get; set; } = new();

    /// <summary>
    /// Gets or sets the latest market interest in scope.
    /// </summary>
    public BrokerInterest LatestInterest { get; set; } = new();

    /// <summary>
    /// Gets or sets the latest message identifier applied to this state.
    /// </summary>
    public string LatestMessageId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC timestamp of the latest applied message.
    /// </summary>
    public DateTime? LatestUpdatedUtc { get; set; }
}