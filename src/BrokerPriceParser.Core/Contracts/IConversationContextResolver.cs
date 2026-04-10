using BrokerPriceParser.Core.Models;
using BrokerPriceParser.Core.State;

namespace BrokerPriceParser.Core.Contracts;

/// <summary>
/// Defines behavior for resolving broker messages against conversation state.
/// </summary>
public interface IConversationContextResolver
{
    /// <summary>
    /// Resolves a partially parsed broker result using normalized input and conversation state.
    /// </summary>
    /// <param name="context">The parse context.</param>
    /// <param name="currentResult">The current parse result.</param>
    /// <returns>The resolved parse result.</returns>
    BrokerParseResult Resolve(ParseContext context, BrokerParseResult currentResult);
}