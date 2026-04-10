using BrokerPriceParser.Core.Enums;

namespace BrokerPriceParser.Core.Llm;

/// <summary>
/// Represents the full request sent to the LLM layer for broker parsing enrichment.
/// </summary>
public sealed class BrokerLlmRequest
{
    /// <summary>
    /// Gets or sets the message identifier.
    /// </summary>
    public string MessageId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the conversation identifier.
    /// </summary>
    public string ConversationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the raw broker message text.
    /// </summary>
    public string RawMessage { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the normalized broker message text.
    /// </summary>
    public string NormalizedMessage { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current rule-based message classification.
    /// </summary>
    public BrokerMessageType MessageType { get; set; } = BrokerMessageType.Unknown;

    /// <summary>
    /// Gets or sets the serialized conversation state snapshot.
    /// </summary>
    public string ConversationStateJson { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the serialized prior message context.
    /// </summary>
    public string PriorMessagesJson { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the serialized current rule-based parse result.
    /// </summary>
    public string CurrentParseResultJson { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the expected output schema definition.
    /// </summary>
    public string OutputSchemaJson { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the final rendered prompt text.
    /// </summary>
    public string Prompt { get; set; } = string.Empty;
}