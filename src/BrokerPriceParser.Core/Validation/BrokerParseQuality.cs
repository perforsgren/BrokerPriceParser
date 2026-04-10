namespace BrokerPriceParser.Core.Validation;

/// <summary>
/// Represents quality metadata for a parser result.
/// </summary>
public sealed class BrokerParseQuality
{
    /// <summary>
    /// Gets or sets the confidence score in the range 0.0 to 1.0.
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Gets or sets ambiguity flags detected during parsing or validation.
    /// </summary>
    public IReadOnlyList<string> AmbiguityFlags { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets validation errors produced after parsing.
    /// </summary>
    public IReadOnlyList<string> ValidationErrors { get; set; } = Array.Empty<string>();
}