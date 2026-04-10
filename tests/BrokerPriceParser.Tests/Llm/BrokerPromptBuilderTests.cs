using System.Text.Json;
using BrokerPriceParser.Core.Llm;
using BrokerPriceParser.Core.Models;
using BrokerPriceParser.Core.State;
using BrokerPriceParser.Infrastructure.Llm;
using Xunit;

namespace BrokerPriceParser.Tests.Llm;

/// <summary>
/// Contains unit tests for <see cref="BrokerPromptBuilder"/>.
/// </summary>
public sealed class BrokerPromptBuilderTests
{
    /// <summary>
    /// Verifies that the prompt builder includes the essential sections and emits a strict JSON schema.
    /// </summary>
    [Fact]
    public void Build_ShouldIncludePromptSections_AndProduceStrictJsonSchema()
    {
        var builder = new BrokerPromptBuilder();

        var context = new ParseContext
        {
            RawMessage = new RawBrokerMessage
            {
                MessageId = "MSG-001",
                ConversationId = "CONV-001",
                RawText = "NOK/SEK 1Y 25 delta rr pls",
                ReceivedUtc = DateTime.UtcNow
            },
            NormalizedMessage = new NormalizedBrokerMessage
            {
                NormalizedText = "NOKSEK 1Y 25D RR PLS"
            }
        };

        var currentResult = new BrokerParseResult
        {
            MessageId = "MSG-001",
            RawMessage = "NOK/SEK 1Y 25 delta rr pls",
            NormalizedMessage = "NOKSEK 1Y 25D RR PLS"
        };

        var settings = new BrokerLlmSettings
        {
            IsEnabled = true,
            MaxPriorMessages = 3,
            SchemaName = "broker_parse_enrichment"
        };

        var request = builder.Build(context, currentResult, settings);

        using var document = JsonDocument.Parse(request.OutputSchemaJson);

        Assert.Equal("MSG-001", request.MessageId);
        Assert.Equal("CONV-001", request.ConversationId);
        Assert.Contains("Raw message:", request.Prompt);
        Assert.Contains("Normalized message:", request.Prompt);
        Assert.Contains("Current rule-based parse result:", request.Prompt);
        Assert.Contains("Output schema:", request.Prompt);

        Assert.True(document.RootElement.TryGetProperty("type", out var typeElement));
        Assert.Equal("object", typeElement.GetString());

        Assert.True(document.RootElement.TryGetProperty("properties", out var propertiesElement));
        Assert.True(propertiesElement.TryGetProperty("instrument", out _));
        Assert.True(propertiesElement.TryGetProperty("quote", out _));
        Assert.True(propertiesElement.TryGetProperty("action", out _));
        Assert.True(propertiesElement.TryGetProperty("llmHints", out _));
    }
}