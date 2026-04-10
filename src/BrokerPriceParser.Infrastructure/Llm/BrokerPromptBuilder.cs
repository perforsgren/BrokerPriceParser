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
            currentResult,
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
    /// Builds the output schema contract that the LLM must follow.
    /// </summary>
    /// <returns>A JSON schema-like contract string.</returns>
    private static string BuildOutputSchemaJson()
    {
        var schema = new
        {
            messageType = "string",
            eventType = "string",
            instrument = new
            {
                pair = "string",
                tenor = "string",
                expiry = "string",
                structure = "string",
                delta = "number|null",
                strikeType = "string",
                strike = "string",
                optionSideBias = "string"
            },
            quote = new
            {
                bid = "number|null",
                ask = "number|null",
                mid = "number|null",
                quoteStyle = "string",
                isFirm = "boolean|null"
            },
            action = new
            {
                verb = "string",
                side = "string",
                target = "string",
                linkedToPriorQuote = "boolean|null"
            },
            contextUsage = new
            {
                usedContext = "boolean",
                resolvedFromContext = "string[]",
                unresolvedReferences = "string[]"
            },
            llmHints = new
            {
                confidence = "number|null",
                notes = "string[]"
            }
        };

        return JsonSerializer.Serialize(schema, SerializerOptions);
    }

    // ────────────────────────────────────

    /// <summary>
    /// Builds the final prompt text for the LLM request.
    /// </summary>
    /// <param name="context">The parse context.</param>
    /// <param name="currentResult">The current rule-based result.</param>
    /// <param name="conversationStateJson">The conversation state JSON.</param>
    /// <param name="priorMessagesJson">The prior messages JSON.</param>
    /// <param name="outputSchemaJson">The output schema JSON.</param>
    /// <returns>The rendered prompt text.</returns>
    private static string BuildPromptText(
        ParseContext context,
        BrokerParseResult currentResult,
        string conversationStateJson,
        string priorMessagesJson,
        string outputSchemaJson)
    {
        var builder = new StringBuilder();

        builder.AppendLine("You are a strict broker price parser for FX options and broker chat language.");
        builder.AppendLine("Return only valid JSON.");
        builder.AppendLine("Do not invent missing values.");
        builder.AppendLine("Prefer partial correctness over aggressive guessing.");
        builder.AppendLine("Use the rule-based parse result as the starting point and only improve fields when justified.");
        builder.AppendLine("If context is required, use the provided conversation state and prior messages.");
        builder.AppendLine();
        builder.AppendLine("Rules:");
        builder.AppendLine("1. Distinguish explicit information from context-resolved information.");
        builder.AppendLine("2. Keep unresolved ambiguity visible.");
        builder.AppendLine("3. Do not output prose outside JSON.");
        builder.AppendLine("4. Broker action words such as TAKE, MINE, LIFT and PAID usually target ASK.");
        builder.AppendLine("5. Broker action words such as HIT, SOLD, YOURS and SELLER usually target BID.");
        builder.AppendLine("6. FLAT BID or FLAT OFFER usually refers to a prior quote level in context.");
        builder.AppendLine();
        builder.AppendLine("Raw message:");
        builder.AppendLine(context.RawMessage.RawText);
        builder.AppendLine();
        builder.AppendLine("Normalized message:");
        builder.AppendLine(context.NormalizedMessage.NormalizedText);
        builder.AppendLine();
        builder.AppendLine("Current rule-based parse result:");
        builder.AppendLine(JsonSerializer.Serialize(currentResult, SerializerOptions));
        builder.AppendLine();
        builder.AppendLine("Conversation state:");
        builder.AppendLine(conversationStateJson);
        builder.AppendLine();
        builder.AppendLine("Prior messages:");
        builder.AppendLine(priorMessagesJson);
        builder.AppendLine();
        builder.AppendLine("Expected output schema:");
        builder.AppendLine(outputSchemaJson);

        return builder.ToString();
    }
}