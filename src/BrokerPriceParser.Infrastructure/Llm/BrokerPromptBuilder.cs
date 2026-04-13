using System.Text;
using System.Text.Json;
using BrokerPriceParser.Core.Contracts;
using BrokerPriceParser.Core.Llm;
using BrokerPriceParser.Core.Models;
using BrokerPriceParser.Core.State;

namespace BrokerPriceParser.Infrastructure.Llm;

/// <summary>
/// Builds structured broker parser prompts for LLM enrichment.
/// </summary>
public sealed class BrokerPromptBuilder : IBrokerPromptBuilder
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    /// <summary>
    /// Builds a broker LLM request from parse context and current parse result.
    /// </summary>
    /// <param name="context">The parse context.</param>
    /// <param name="currentResult">The current rule-based parse result.</param>
    /// <param name="settings">The LLM settings.</param>
    /// <returns>A broker LLM request.</returns>
    public BrokerLlmRequest Build(ParseContext context, BrokerParseResult currentResult, BrokerLlmSettings settings)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(currentResult);
        ArgumentNullException.ThrowIfNull(settings);

        var priorMessages = context.PriorMessages
            .Take(Math.Max(settings.MaxPriorMessages, 0))
            .Select(x => new
            {
                x.RawMessage.MessageId,
                x.RawMessage.ReceivedUtc,
                x.RawMessage.RawText,
                x.NormalizedText
            })
            .ToArray();

        var conversationStateJson = JsonSerializer.Serialize(context.ConversationState, SerializerOptions);
        var priorMessagesJson = JsonSerializer.Serialize(priorMessages, SerializerOptions);
        var currentParseResultJson = JsonSerializer.Serialize(currentResult, SerializerOptions);
        var outputSchemaJson = BuildOutputSchemaJson();
        var prompt = BuildPromptText(
            context,
            currentParseResultJson,
            conversationStateJson,
            priorMessagesJson,
            outputSchemaJson);

        return new BrokerLlmRequest
        {
            MessageId = context.RawMessage.MessageId,
            ConversationId = context.RawMessage.ConversationId,
            RawMessage = context.RawMessage.RawText,
            NormalizedMessage = context.NormalizedMessage.NormalizedText,
            MessageType = currentResult.MessageType,
            ConversationStateJson = conversationStateJson,
            PriorMessagesJson = priorMessagesJson,
            CurrentParseResultJson = currentParseResultJson,
            OutputSchemaJson = outputSchemaJson,
            Prompt = prompt
        };
    }

    // ────────────────────────────────────

    /// <summary>
    /// Builds the strict JSON schema used for Structured Outputs.
    /// </summary>
    /// <returns>The serialized JSON schema.</returns>
    private static string BuildOutputSchemaJson()
    {
        var schema = new
        {
            type = "object",
            properties = new
            {
                messageType = new
                {
                    type = "string"
                },
                eventType = new
                {
                    type = "string"
                },
                instrument = new
                {
                    type = "object",
                    properties = new
                    {
                        pair = new { type = "string" },
                        tenor = new { type = "string" },
                        expiry = new { type = "string" },
                        structure = new { type = "string" },
                        delta = new { type = "string" },
                        strikeType = new { type = "string" },
                        strike = new { type = "string" },
                        optionSideBias = new { type = "string" }
                    },
                    required = new[]
                    {
                        "pair", "tenor", "expiry", "structure", "delta", "strikeType", "strike", "optionSideBias"
                    },
                    additionalProperties = false
                },
                quote = new
                {
                    type = "object",
                    properties = new
                    {
                        bid = new { type = "string" },
                        ask = new { type = "string" },
                        mid = new { type = "string" },
                        quoteStyle = new { type = "string" },
                        isFirm = new { type = "string" }
                    },
                    required = new[]
                    {
                        "bid", "ask", "mid", "quoteStyle", "isFirm"
                    },
                    additionalProperties = false
                },
                action = new
                {
                    type = "object",
                    properties = new
                    {
                        verb = new { type = "string" },
                        side = new { type = "string" },
                        target = new { type = "string" },
                        linkedToPriorQuote = new { type = "string" }
                    },
                    required = new[]
                    {
                        "verb", "side", "target", "linkedToPriorQuote"
                    },
                    additionalProperties = false
                },
                llmHints = new
                {
                    type = "object",
                    properties = new
                    {
                        confidence = new { type = "string" },
                        notes = new
                        {
                            type = "array",
                            items = new
                            {
                                type = "string"
                            }
                        }
                    },
                    required = new[]
                    {
                        "confidence", "notes"
                    },
                    additionalProperties = false
                }
            },
            required = new[]
            {
                "messageType", "eventType", "instrument", "quote", "action", "llmHints"
            },
            additionalProperties = false
        };

        return JsonSerializer.Serialize(schema, SerializerOptions);
    }

    // ────────────────────────────────────

    /// <summary>
    /// Builds the final prompt text for the LLM request.
    /// </summary>
    /// <param name="context">The parse context.</param>
    /// <param name="currentParseResultJson">The serialized current parse result.</param>
    /// <param name="conversationStateJson">The serialized conversation state.</param>
    /// <param name="priorMessagesJson">The serialized prior messages.</param>
    /// <param name="outputSchemaJson">The serialized output schema.</param>
    /// <returns>The rendered prompt text.</returns>
    private static string BuildPromptText(
        ParseContext context,
        string currentParseResultJson,
        string conversationStateJson,
        string priorMessagesJson,
        string outputSchemaJson)
    {
        var builder = new StringBuilder();

        builder.AppendLine("You are a strict broker price parser for FX options broker chat.");
        builder.AppendLine("Return JSON only.");
        builder.AppendLine("Do not invent values.");
        builder.AppendLine("Use empty strings for unknown scalar fields.");
        builder.AppendLine("Use an empty array for llmHints.notes when there are no notes.");
        builder.AppendLine("Never output prose outside JSON.");
        builder.AppendLine("Use the current rule-based parse result as the baseline and only improve missing or ambiguous fields.");
        builder.AppendLine();
        builder.AppendLine("Interpretation rules:");
        builder.AppendLine("- TAKE, MINE, LIFT and PAID usually target ASK.");
        builder.AppendLine("- HIT, SOLD and YOURS usually target BID.");
        builder.AppendLine("- BUYER and SELLER usually indicate directional interest, not completed execution.");
        builder.AppendLine("- FLAT BID and FLAT OFFER usually refer to the latest prior quote in context.");
        builder.AppendLine("- Keep unresolved ambiguity visible by leaving fields empty rather than guessing.");
        builder.AppendLine();
        builder.AppendLine("Raw message:");
        builder.AppendLine(context.RawMessage.RawText);
        builder.AppendLine();
        builder.AppendLine("Normalized message:");
        builder.AppendLine(context.NormalizedMessage.NormalizedText);
        builder.AppendLine();
        builder.AppendLine("Current rule-based parse result:");
        builder.AppendLine(currentParseResultJson);
        builder.AppendLine();
        builder.AppendLine("Conversation state:");
        builder.AppendLine(conversationStateJson);
        builder.AppendLine();
        builder.AppendLine("Prior messages:");
        builder.AppendLine(priorMessagesJson);
        builder.AppendLine();
        builder.AppendLine("Output schema:");
        builder.AppendLine(outputSchemaJson);

        return builder.ToString();
    }
}