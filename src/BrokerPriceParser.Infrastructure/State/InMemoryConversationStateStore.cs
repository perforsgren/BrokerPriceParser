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

        if (ContainsInstrumentData(result.Instrument))
        {
            state.CurrentInstrument = MergeInstrument(state.CurrentInstrument, result.Instrument);
        }

        if (ContainsQuoteData(result.Quote))
        {
            state.LatestQuote = MergeQuote(state.LatestQuote, result.Quote);
        }

        if (ContainsActionData(result.Action))
        {
            state.LatestAction = MergeAction(state.LatestAction, result.Action);
        }

        state.LatestMessageId = result.MessageId;
        state.LatestUpdatedUtc = receivedUtc;
    }

    // ────────────────────────────────────

    /// <summary>
    /// Determines whether an instrument contains any meaningful data.
    /// </summary>
    /// <param name="instrument">The instrument to inspect.</param>
    /// <returns><c>true</c> if the instrument contains data; otherwise <c>false</c>.</returns>
    private static bool ContainsInstrumentData(BrokerInstrument instrument)
    {
        return !string.IsNullOrWhiteSpace(instrument.Pair)
            || !string.IsNullOrWhiteSpace(instrument.Tenor)
            || !string.IsNullOrWhiteSpace(instrument.Expiry)
            || !string.IsNullOrWhiteSpace(instrument.Structure)
            || instrument.Delta.HasValue
            || !string.IsNullOrWhiteSpace(instrument.StrikeType)
            || !string.IsNullOrWhiteSpace(instrument.Strike)
            || !string.IsNullOrWhiteSpace(instrument.OptionSideBias);
    }

    // ────────────────────────────────────

    /// <summary>
    /// Determines whether a quote contains any meaningful data.
    /// </summary>
    /// <param name="quote">The quote to inspect.</param>
    /// <returns><c>true</c> if the quote contains data; otherwise <c>false</c>.</returns>
    private static bool ContainsQuoteData(BrokerQuote quote)
    {
        return quote.Bid.HasValue
            || quote.Ask.HasValue
            || quote.Mid.HasValue
            || quote.IsFirm.HasValue
            || quote.QuoteStyle != BrokerPriceParser.Core.Enums.QuoteStyle.Unknown;
    }

    // ────────────────────────────────────

    /// <summary>
    /// Determines whether an action contains any meaningful data.
    /// </summary>
    /// <param name="action">The action to inspect.</param>
    /// <returns><c>true</c> if the action contains data; otherwise <c>false</c>.</returns>
    private static bool ContainsActionData(BrokerAction action)
    {
        return !string.IsNullOrWhiteSpace(action.Verb)
            || !string.IsNullOrWhiteSpace(action.Side)
            || !string.IsNullOrWhiteSpace(action.Target)
            || action.LinkedToPriorQuote.HasValue;
    }

    // ────────────────────────────────────

    /// <summary>
    /// Merges a new instrument snapshot into the existing state instrument.
    /// </summary>
    /// <param name="existing">The existing instrument.</param>
    /// <param name="current">The current instrument.</param>
    /// <returns>The merged instrument.</returns>
    private static BrokerInstrument MergeInstrument(BrokerInstrument existing, BrokerInstrument current)
    {
        return new BrokerInstrument
        {
            Pair = !string.IsNullOrWhiteSpace(current.Pair) ? current.Pair : existing.Pair,
            Tenor = !string.IsNullOrWhiteSpace(current.Tenor) ? current.Tenor : existing.Tenor,
            Expiry = !string.IsNullOrWhiteSpace(current.Expiry) ? current.Expiry : existing.Expiry,
            Structure = !string.IsNullOrWhiteSpace(current.Structure) ? current.Structure : existing.Structure,
            Delta = current.Delta ?? existing.Delta,
            StrikeType = !string.IsNullOrWhiteSpace(current.StrikeType) ? current.StrikeType : existing.StrikeType,
            Strike = !string.IsNullOrWhiteSpace(current.Strike) ? current.Strike : existing.Strike,
            OptionSideBias = !string.IsNullOrWhiteSpace(current.OptionSideBias) ? current.OptionSideBias : existing.OptionSideBias
        };
    }

    // ────────────────────────────────────

    /// <summary>
    /// Merges a new quote snapshot into the existing state quote.
    /// </summary>
    /// <param name="existing">The existing quote.</param>
    /// <param name="current">The current quote.</param>
    /// <returns>The merged quote.</returns>
    private static BrokerQuote MergeQuote(BrokerQuote existing, BrokerQuote current)
    {
        return new BrokerQuote
        {
            Bid = current.Bid ?? existing.Bid,
            Ask = current.Ask ?? existing.Ask,
            Mid = current.Mid ?? existing.Mid,
            QuoteStyle = current.QuoteStyle != BrokerPriceParser.Core.Enums.QuoteStyle.Unknown
                ? current.QuoteStyle
                : existing.QuoteStyle,
            IsFirm = current.IsFirm ?? existing.IsFirm
        };
    }

    // ────────────────────────────────────

    /// <summary>
    /// Merges a new action snapshot into the existing state action.
    /// </summary>
    /// <param name="existing">The existing action.</param>
    /// <param name="current">The current action.</param>
    /// <returns>The merged action.</returns>
    private static BrokerAction MergeAction(BrokerAction existing, BrokerAction current)
    {
        return new BrokerAction
        {
            Verb = !string.IsNullOrWhiteSpace(current.Verb) ? current.Verb : existing.Verb,
            Side = !string.IsNullOrWhiteSpace(current.Side) ? current.Side : existing.Side,
            Target = !string.IsNullOrWhiteSpace(current.Target) ? current.Target : existing.Target,
            LinkedToPriorQuote = current.LinkedToPriorQuote ?? existing.LinkedToPriorQuote
        };
    }
}