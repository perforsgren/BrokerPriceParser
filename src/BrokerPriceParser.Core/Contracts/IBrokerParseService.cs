using BrokerPriceParser.Core.Models;
using BrokerPriceParser.Core.State;

namespace BrokerPriceParser.Core.Contracts;

/// <summary>
/// Defines behavior for parsing broker messages into structured output.
/// </summary>
public interface IBrokerParseService
{
    /// <summary>
    /// Parses the supplied parse context into a structured result.
    /// </summary>
    /// <param name="context">The parse context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A structured broker parse result.</returns>
    Task<BrokerParseResult> ParseAsync(ParseContext context, CancellationToken cancellationToken = default);
}