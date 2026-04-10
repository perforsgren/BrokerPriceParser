namespace BrokerPriceParser.Core.Llm;

/// <summary>
/// Represents the structured response returned from the LLM layer.
/// </summary>
public sealed class BrokerLlmResponse
{
    /// <summary>
    /// Gets or sets a value indicating whether the call completed successfully.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether enrichment was actually applied.
    /// </summary>
    public bool IsEnrichmentApplied { get; set; }

    /// <summary>
    /// Gets or sets the raw text returned by the LLM.
    /// </summary>
    public string RawResponseText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the parsed JSON payload returned by the LLM.
    /// </summary>
    public string ParsedJsonPayload { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets an optional error message if the call failed or was skipped.
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;
}