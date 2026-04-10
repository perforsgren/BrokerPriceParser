using BrokerPriceParser.Core.Models;
using BrokerPriceParser.Core.State;

namespace BrokerPriceParser.Core.Contracts;

/// <summary>
/// Defines behavior for storing and updating conversation state.
/// </summary>
public interface IConversationStateStore
{
    /// <summary>
    /// Gets the state for a conversation.
    /// </summary>
    /// <param name="conversationId">The conversation identifier.</param>
    /// <returns>The conversation state.</returns>
    ConversationState GetOrCreate(string conversationId);

    /// <summary>
    /// Applies a parse result to the conversation state.
    /// </summary>
    /// <param name="conversationId">The conversation identifier.</param>
    /// <param name="result">The parse result to apply.</param>
    /// <param name="receivedUtc">The message received timestamp in UTC.</param>
    void Apply(string conversationId, BrokerParseResult result, DateTime receivedUtc);
}