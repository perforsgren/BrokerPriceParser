using BrokerPriceParser.Core.Models;

namespace BrokerPriceParser.Core.Contracts;

/// <summary>
/// Defines behavior for validating parser results.
/// </summary>
public interface IBrokerValidationService
{
    /// <summary>
    /// Validates a broker parse result and returns validation messages.
    /// </summary>
    /// <param name="result">The result to validate.</param>
    /// <returns>A list of validation messages.</returns>
    IReadOnlyList<string> Validate(BrokerParseResult result);
}