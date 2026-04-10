using System.Text.RegularExpressions;
using BrokerPriceParser.Core.Contracts;
using BrokerPriceParser.Core.Models;

namespace BrokerPriceParser.Infrastructure.Validation;

/// <summary>
/// Provides rule-based validation for broker parser results.
/// </summary>
public sealed class BrokerValidationService : IBrokerValidationService
{
    /// <summary>
    /// Validates a broker parse result and returns validation messages.
    /// </summary>
    /// <param name="result">The result to validate.</param>
    /// <returns>A list of validation messages.</returns>
    public IReadOnlyList<string> Validate(BrokerParseResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var errors = new List<string>();

        if (!string.IsNullOrWhiteSpace(result.Instrument.Pair)
            && !Regex.IsMatch(result.Instrument.Pair, @"^[A-Z]{6}$"))
        {
            errors.Add("Instrument pair must be a six-letter uppercase currency pair.");
        }

        if (!string.IsNullOrWhiteSpace(result.Instrument.Tenor)
            && !Regex.IsMatch(result.Instrument.Tenor, @"^\d+(D|W|M|Y)$"))
        {
            errors.Add("Instrument tenor must match the expected format, for example 1W, 3M or 1Y.");
        }

        if (result.Quote.Bid.HasValue && result.Quote.Ask.HasValue && result.Quote.Bid > result.Quote.Ask)
        {
            errors.Add("Quote bid must not be greater than quote ask.");
        }

        return errors;
    }
}