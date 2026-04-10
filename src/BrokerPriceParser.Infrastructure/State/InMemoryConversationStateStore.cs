using System.Collections.Concurrent;
using BrokerPriceParser.Core.Contracts;
using BrokerPriceParser.Core.Models;
using BrokerPriceParser.Core.State;

namespace BrokerPriceParser.Infrastructure.State;

/// <summary>
/// Stores conversation state in memory for the current application session.
/// </summary>
public sealed class InMemoryConversationStateStore : IConversationStateStore
{
    private readonly ConcurrentDictionary<string, ConversationState> _states = new();

    /// <summary>
    /// Gets the state for a conversation.
    /// </summary>
    /// <param name="conversationId">The conversation identifier.</param>
    /// <returns>The conversation state.</returns>
    public ConversationState GetOrCreate(string conversationId)
    {
        if (string.IsNullOrWhiteSpace(conversationId))
        {
            throw new ArgumentException("Conversation ID must not be empty.", nameof(conversationId));
        }

        return _states.GetOrAdd(conversationId, id => new ConversationState
        {
            ConversationId = id
        });
    }

    /// <summary>
    /// Applies a parse result to the conversation state.
    /// </summary>
    /// <param name="conversationId">The conversation identifier.</param>
    /// <param name="result">The parse result to apply.</param>
    /// <param name="receivedUtc">The message received timestamp in UTC.</param>
    public void Apply(string conversationId, BrokerParseResult result, DateTime receivedUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conversationId);
        ArgumentNullException.ThrowIfNull(result);

        var state = GetOrCreate(conversationId);

        if (!string.IsNullOrWhiteSpace(result.Instrument.Pair)
            || !string.IsNullOrWhiteSpace(result.Instrument.Tenor)
            || !string.IsNullOrWhiteSpace(result.Instrument.Structure))
        {
            state.CurrentInstrument = result.Instrument;
        }

        if (result.Quote.Bid.HasValue || result.Quote.Ask.HasValue || result.Quote.Mid.HasValue)
        {
            state.LatestQuote = result.Quote;
        }

        if (!string.IsNullOrWhiteSpace(result.Action.Verb))
        {
            state.LatestAction = result.Action;
        }

        state.LatestMessageId = result.MessageId;
        state.LatestUpdatedUtc = receivedUtc;
    }
}