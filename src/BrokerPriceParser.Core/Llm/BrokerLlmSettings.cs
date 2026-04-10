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
    /// Gets or sets the model name to send to the provider.
    /// </summary>
    public string ModelName { get; set; } = "gpt-5";

    /// <summary>
    /// Gets or sets the maximum number of prior messages to include in the prompt.
    /// </summary>
    public int MaxPriorMessages { get; set; } = 5;

    /// <summary>
    /// Gets or sets the full Responses API endpoint URL.
    /// </summary>
    public string ApiBaseUrl { get; set; } = "https://api.openai.com/v1/responses";

    /// <summary>
    /// Gets or sets the environment variable name that stores the API key.
    /// </summary>
    public string ApiKeyEnvironmentVariableName { get; set; } = "OPENAI_API_KEY";

    /// <summary>
    /// Gets or sets the schema name sent to Structured Outputs.
    /// </summary>
    public string SchemaName { get; set; } = "broker_parse_enrichment";

    /// <summary>
    /// Gets or sets the request timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 60;
}