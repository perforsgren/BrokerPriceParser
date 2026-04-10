namespace BrokerPriceParser.Core.Llm;

/// <summary>
/// Represents the structured JSON payload expected back from the LLM.
/// All scalar values are strings so that unknown values can be represented as empty strings.
/// </summary>
public sealed class BrokerLlmStructuredOutput
{
    /// <summary>
    /// Gets or sets the suggested message type.
    /// </summary>
    public string MessageType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the suggested event type.
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the structured instrument suggestion.
    /// </summary>
    public BrokerLlmStructuredInstrument Instrument { get; set; } = new();

    /// <summary>
    /// Gets or sets the structured quote suggestion.
    /// </summary>
    public BrokerLlmStructuredQuote Quote { get; set; } = new();

    /// <summary>
    /// Gets or sets the structured action suggestion.
    /// </summary>
    public BrokerLlmStructuredAction Action { get; set; } = new();

    /// <summary>
    /// Gets or sets LLM-specific metadata.
    /// </summary>
    public BrokerLlmStructuredHints LlmHints { get; set; } = new();
}

/// <summary>
/// Represents instrument fields returned from the LLM.
/// </summary>
public sealed class BrokerLlmStructuredInstrument
{
    /// <summary>
    /// Gets or sets the pair.
    /// </summary>
    public string Pair { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the tenor.
    /// </summary>
    public string Tenor { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the expiry.
    /// </summary>
    public string Expiry { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the structure.
    /// </summary>
    public string Structure { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the delta as text.
    /// </summary>
    public string Delta { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the strike type.
    /// </summary>
    public string StrikeType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the strike.
    /// </summary>
    public string Strike { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the option side bias.
    /// </summary>
    public string OptionSideBias { get; set; } = string.Empty;
}

/// <summary>
/// Represents quote fields returned from the LLM.
/// </summary>
public sealed class BrokerLlmStructuredQuote
{
    /// <summary>
    /// Gets or sets the bid as text.
    /// </summary>
    public string Bid { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ask as text.
    /// </summary>
    public string Ask { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the mid as text.
    /// </summary>
    public string Mid { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the quote style.
    /// </summary>
    public string QuoteStyle { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the firmness flag as text.
    /// </summary>
    public string IsFirm { get; set; } = string.Empty;
}

/// <summary>
/// Represents action fields returned from the LLM.
/// </summary>
public sealed class BrokerLlmStructuredAction
{
    /// <summary>
    /// Gets or sets the action verb.
    /// </summary>
    public string Verb { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the action side.
    /// </summary>
    public string Side { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the action target.
    /// </summary>
    public string Target { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the linked-to-prior-quote flag as text.
    /// </summary>
    public string LinkedToPriorQuote { get; set; } = string.Empty;
}

/// <summary>
/// Represents extra metadata returned from the LLM.
/// </summary>
public sealed class BrokerLlmStructuredHints
{
    /// <summary>
    /// Gets or sets the LLM confidence as text.
    /// </summary>
    public string Confidence { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the LLM notes.
    /// </summary>
    public IReadOnlyList<string> Notes { get; set; } = Array.Empty<string>();
}