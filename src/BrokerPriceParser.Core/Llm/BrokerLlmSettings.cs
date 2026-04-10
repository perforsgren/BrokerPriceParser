namespace BrokerPriceParser.Core.Llm;

/// <summary>
/// Represents configuration settings for the broker LLM enrichment layer.
/// </summary>
public sealed class BrokerLlmSettings
{
    /// <summary>
    /// Gets or sets a value indicating whether LLM enrichment is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether enrichment should only run for low-confidence results.
    /// </summary>
    public bool UseOnlyForLowConfidence { get; set; } = true;

    /// <summary>
    /// Gets or sets the confidence threshold below which enrichment is considered.
    /// </summary>
    public double LowConfidenceThreshold { get; set; } = 0.55;

    /// <summary>
    /// Gets or sets the logical model name to use.
    /// </summary>
    public string ModelName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the maximum number of prior messages to include in the prompt.
    /// </summary>
    public int MaxPriorMessages { get; set; } = 5;
}