using BrokerPriceParser.Core.Models;

namespace BrokerPriceParser.Core.State;

/// <summary>
/// Represents the full context supplied to the parser for a single message.
/// </summary>
public sealed class ParseContext
{
    /// <summary>
    /// Gets or sets the current raw message.
    /// </summary>
    public RawBrokerMessage RawMessage { get; set; } = new();

    /// <summary>
    /// Gets or sets the normalized message.
    /// </summary>
    public NormalizedBrokerMessage NormalizedMessage { get; set; } = new();

    /// <summary>
    /// Gets or sets the active conversation state.
    /// </summary>
    public ConversationState ConversationState { get; set; } = new();

    /// <summary>
    /// Gets or sets prior normalized messages used as context.
    /// </summary>
    public IReadOnlyList<NormalizedBrokerMessage> PriorMessages { get; set; } = Array.Empty<NormalizedBrokerMessage>();
}