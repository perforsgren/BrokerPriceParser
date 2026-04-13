using System.Text.RegularExpressions;
using BrokerPriceParser.Core.Contracts;
using BrokerPriceParser.Core.Models;

namespace BrokerPriceParser.Infrastructure.Normalization;

/// <summary>
/// Provides deterministic normalization for raw broker messages.
/// </summary>
public sealed class BrokerMessageNormalizer : IBrokerMessageNormalizer
{
    private static readonly HashSet<string> SupportedCurrencies = new(StringComparer.OrdinalIgnoreCase)
    {
        "USD", "EUR", "GBP", "JPY", "SEK", "NOK", "DKK", "CHF", "AUD", "NZD", "CAD",
        "CNH", "CNY", "HKD", "SGD", "KRW", "INR", "TWD", "THB", "ZAR", "MXN", "TRY",
        "PLN", "CZK", "HUF", "RON", "ILS", "AED", "SAR"
    };

    /// <summary>
    /// Normalizes a raw broker message into a structured normalized message.
    /// </summary>
    /// <param name="message">The raw message to normalize.</param>
    /// <returns>A normalized broker message.</returns>
    public NormalizedBrokerMessage Normalize(RawBrokerMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var rules = new List<string>();
        var normalizedText = message.RawText ?? string.Empty;

        normalizedText = normalizedText.Trim();
        normalizedText = normalizedText.ToUpperInvariant();
        normalizedText = NormalizeGreekCharacters(normalizedText, rules);
        normalizedText = NormalizeWhitespace(normalizedText, rules);
        normalizedText = NormalizeCurrencyPair(normalizedText, rules);
        normalizedText = NormalizeAtmAliases(normalizedText, rules);
        normalizedText = NormalizeDeltaTokens(normalizedText, rules);
        normalizedText = NormalizeStructureAliases(normalizedText, rules);
        normalizedText = NormalizeWhitespace(normalizedText, rules);

        var tokens = SplitTokens(normalizedText);
        var detectedCurrencyPair = DetectCurrencyPair(normalizedText);
        var detectedTenor = DetectTenor(normalizedText);
        var detectedStructure = DetectStructure(normalizedText);
        var detectedDelta = DetectDelta(normalizedText);

        return new NormalizedBrokerMessage
        {
            RawMessage = message,
            NormalizedText = normalizedText,
            Tokens = tokens,
            DetectedCurrencyPair = detectedCurrencyPair,
            DetectedTenor = detectedTenor,
            DetectedStructure = detectedStructure,
            DetectedDelta = detectedDelta,
            AppliedNormalizationRules = rules
        };
    }

    // ────────────────────────────────────

    /// <summary>
    /// Normalizes Greek characters used in broker language into ASCII-friendly tokens.
    /// </summary>
    /// <param name="input">The text to normalize.</param>
    /// <param name="rules">The applied rule list.</param>
    /// <returns>The updated text.</returns>
    private static string NormalizeGreekCharacters(string input, ICollection<string> rules)
    {
        var output = input.Replace("Δ", "D", StringComparison.Ordinal);

        if (!ReferenceEquals(output, input) && output != input)
        {
            rules.Add("NormalizeGreekCharacters");
        }

        return output;
    }

    // ────────────────────────────────────

    /// <summary>
    /// Collapses repeated whitespace and normalizes line breaks to single spaces.
    /// </summary>
    /// <param name="input">The text to normalize.</param>
    /// <param name="rules">The applied rule list.</param>
    /// <returns>The normalized text.</returns>
    private static string NormalizeWhitespace(string input, ICollection<string> rules)
    {
        var output = Regex.Replace(input, @"\s+", " ").Trim();

        if (output != input)
        {
            rules.Add("NormalizeWhitespace");
        }

        return output;
    }

    // ────────────────────────────────────

    /// <summary>
    /// Normalizes common currency pair spellings into a six-letter format when both legs are valid currencies.
    /// </summary>
    /// <param name="input">The text to normalize.</param>
    /// <param name="rules">The applied rule list.</param>
    /// <returns>The updated text.</returns>
    private static string NormalizeCurrencyPair(string input, ICollection<string> rules)
    {
        var output = input;

        output = Regex.Replace(
            output,
            @"\b([A-Z]{3})\s*/\s*([A-Z]{3})\b",
            match =>
            {
                var left = match.Groups[1].Value;
                var right = match.Groups[2].Value;

                return AreValidDistinctCurrencies(left, right)
                    ? left + right
                    : match.Value;
            });

        output = Regex.Replace(
            output,
            @"\b([A-Z]{3})\s+([A-Z]{3})\b",
            match =>
            {
                var left = match.Groups[1].Value;
                var right = match.Groups[2].Value;

                return AreValidDistinctCurrencies(left, right)
                    ? left + right
                    : match.Value;
            });

        if (output != input)
        {
            rules.Add("NormalizeCurrencyPair");
        }

        return output;
    }

    // ────────────────────────────────────

    /// <summary>
    /// Normalizes common ATM aliases into ATM or ATMF.
    /// </summary>
    /// <param name="input">The text to normalize.</param>
    /// <param name="rules">The applied rule list.</param>
    /// <returns>The updated text.</returns>
    private static string NormalizeAtmAliases(string input, ICollection<string> rules)
    {
        var output = input;

        output = Regex.Replace(output, @"\bAT\s+THE\s+MONEY\s+FORWARD\b", "ATMF");
        output = Regex.Replace(output, @"\bAT\s+THE\s+MONEY\s+FWD\b", "ATMF");
        output = Regex.Replace(output, @"\bAT\s+THE\s+MONEY\b", "ATM");

        if (output != input)
        {
            rules.Add("NormalizeAtmAliases");
        }

        return output;
    }

    // ────────────────────────────────────

    /// <summary>
    /// Normalizes common delta expressions into a compact D-suffixed format.
    /// </summary>
    /// <param name="input">The text to normalize.</param>
    /// <param name="rules">The applied rule list.</param>
    /// <returns>The updated text.</returns>
    private static string NormalizeDeltaTokens(string input, ICollection<string> rules)
    {
        var output = input;

        output = Regex.Replace(output, @"\b(\d{1,2})\s*DELTA\b", "$1D");
        output = Regex.Replace(output, @"\b(\d{1,2})\s+D\b", "$1D");

        if (output != input)
        {
            rules.Add("NormalizeDeltaTokens");
        }

        return output;
    }

    // ────────────────────────────────────

    /// <summary>
    /// Normalizes structure aliases such as RISK REVERSAL, BFLY and FLY.
    /// </summary>
    /// <param name="input">The text to normalize.</param>
    /// <param name="rules">The applied rule list.</param>
    /// <returns>The updated text.</returns>
    private static string NormalizeStructureAliases(string input, ICollection<string> rules)
    {
        var output = input;

        output = Regex.Replace(output, @"\bRISK\s+REVERSAL\b", "RR");
        output = Regex.Replace(output, @"\bRISK\s+REV\b", "RR");
        output = Regex.Replace(output, @"\bBUTTERFLY\b", "BF");
        output = Regex.Replace(output, @"\bBFLY\b", "BF");
        output = Regex.Replace(output, @"\bFLY\b", "BF");

        if (output != input)
        {
            rules.Add("NormalizeStructureAliases");
        }

        return output;
    }

    // ────────────────────────────────────

    /// <summary>
    /// Detects the first valid currency pair in the message, including conservative implicit USD pairs.
    /// </summary>
    /// <param name="input">The normalized text.</param>
    /// <returns>The detected pair, or an empty string if not found.</returns>
    private static string DetectCurrencyPair(string input)
    {
        var explicitMatches = Regex.Matches(input, @"\b([A-Z]{6})\b");

        foreach (Match match in explicitMatches)
        {
            var candidate = match.Value;
            var left = candidate[..3];
            var right = candidate[3..6];

            if (AreValidDistinctCurrencies(left, right))
            {
                return candidate;
            }
        }

        var implicitUsdPair = DetectImplicitUsdPair(input);
        return implicitUsdPair;
    }

    // ────────────────────────────────────

    /// <summary>
    /// Detects an implicit XXXUSD pair when the broker omits USD and only the base currency is written.
    /// </summary>
    /// <param name="input">The normalized text.</param>
    /// <returns>The inferred XXXUSD pair, or an empty string if none is detected.</returns>
    private static string DetectImplicitUsdPair(string input)
    {
        var tokens = input
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        for (var index = 0; index < tokens.Length; index++)
        {
            var token = tokens[index];

            if (!IsSupportedNonUsdCurrencyToken(token))
            {
                continue;
            }

            var hasNearbyTenor =
                (index + 1 < tokens.Length && Regex.IsMatch(tokens[index + 1], @"^\d+(D|W|M|Y)$"))
                || (index > 0 && Regex.IsMatch(tokens[index - 1], @"^\d+(D|W|M|Y)$"));

            var hasNearbyStructure =
                tokens.Skip(index + 1).Take(3).Any(x => Regex.IsMatch(x, @"^(RR|BF|ATM|ATMF)$"));

            var hasNearbyPrice =
                Regex.IsMatch(input, @"(?<!\d)-?\d+(\.\d+)?\s*/\s*-?\d+(\.\d+)?")
                || Regex.IsMatch(input, @"\b(BID|ASK|OFFER|OFR|MID|PAYING|OFFERING)\b\s*-?\d+(\.\d+)?")
                || Regex.IsMatch(input, @"-?\d+(\.\d+)?\s*\b(BID|ASK|OFFER|OFR|MID)\b");

            if (hasNearbyTenor || hasNearbyStructure || hasNearbyPrice)
            {
                return token + "USD";
            }
        }

        return string.Empty;
    }

    // ────────────────────────────────────

    /// <summary>
    /// Determines whether a token is a supported non-USD currency that can imply an XXXUSD pair.
    /// </summary>
    /// <param name="token">The token to inspect.</param>
    /// <returns><c>true</c> if the token can imply an XXXUSD pair; otherwise <c>false</c>.</returns>
    private static bool IsSupportedNonUsdCurrencyToken(string token)
    {
        return SupportedCurrencies.Contains(token)
            && !token.Equals("USD", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Detects the first tenor token in the message.
    /// </summary>
    /// <param name="input">The normalized text.</param>
    /// <returns>The detected tenor, or an empty string if not found.</returns>
    private static string DetectTenor(string input)
    {
        var match = Regex.Match(input, @"\b\d+(D|W|M|Y)\b");
        return match.Success ? match.Value : string.Empty;
    }

    // ────────────────────────────────────

    /// <summary>
    /// Detects the first supported structure hint in the message.
    /// </summary>
    /// <param name="input">The normalized text.</param>
    /// <returns>The detected structure, or an empty string if not found.</returns>
    private static string DetectStructure(string input)
    {
        if (Regex.IsMatch(input, @"\bATMF\b"))
        {
            return "ATMF";
        }

        if (Regex.IsMatch(input, @"\bATM\b"))
        {
            return "ATM";
        }

        if (Regex.IsMatch(input, @"\bRR\b"))
        {
            return "RR";
        }

        if (Regex.IsMatch(input, @"\bBF\b"))
        {
            return "BF";
        }

        return string.Empty;
    }

    // ────────────────────────────────────

    /// <summary>
    /// Detects the first delta token in the message.
    /// </summary>
    /// <param name="input">The normalized text.</param>
    /// <returns>The detected delta, or <c>null</c> if not found.</returns>
    private static decimal? DetectDelta(string input)
    {
        var match = Regex.Match(input, @"\b(\d{1,2})D\b");

        if (!match.Success)
        {
            return null;
        }

        if (decimal.TryParse(match.Groups[1].Value, out var delta))
        {
            return delta;
        }

        return null;
    }

    // ────────────────────────────────────

    /// <summary>
    /// Splits normalized text into tokens using spaces.
    /// </summary>
    /// <param name="input">The normalized text.</param>
    /// <returns>A read-only token list.</returns>
    private static IReadOnlyList<string> SplitTokens(string input)
    {
        return input
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToArray();
    }

    // ────────────────────────────────────

    /// <summary>
    /// Determines whether two three-letter codes are distinct supported currencies.
    /// </summary>
    /// <param name="left">The left currency.</param>
    /// <param name="right">The right currency.</param>
    /// <returns><c>true</c> if both codes are supported and distinct; otherwise <c>false</c>.</returns>
    private static bool AreValidDistinctCurrencies(string left, string right)
    {
        return SupportedCurrencies.Contains(left)
            && SupportedCurrencies.Contains(right)
            && !left.Equals(right, StringComparison.OrdinalIgnoreCase);
    }
}