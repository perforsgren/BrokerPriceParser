using BrokerPriceParser.Core.Models;

namespace BrokerPriceParser.Core.Contracts;

/// <summary>
/// Defines behavior for scoring parser result confidence.
/// </summary>
public interface IConfidenceScoringService
{
    /// <summary>
    /// Calculates confidence for a broker parse result.
    /// </summary>
    /// <param name="result">The parse result.</param>
    /// <returns>A confidence score in the range 0.0 to 1.0.</returns>
    double Calculate(BrokerParseResult result);
}