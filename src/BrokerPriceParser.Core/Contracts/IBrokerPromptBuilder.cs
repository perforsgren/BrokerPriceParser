using BrokerPriceParser.Core.Llm;
using BrokerPriceParser.Core.Models;
using BrokerPriceParser.Core.State;

namespace BrokerPriceParser.Core.Contracts;

/// <summary>
/// Defines behavior for building broker parser prompts for LLM use.
/// </summary>
public interface IBrokerPromptBuilder
{
    /// <summary>
    /// Builds a broker LLM request from parse context and current parse result.
    /// </summary>
    /// <param name="context">The parse context.</param>
    /// <param name="currentResult">The current rule-based parse result.</param>
    /// <param name="settings">The LLM settings.</param>
    /// <returns>A broker LLM request.</returns>
    BrokerLlmRequest Build(ParseContext context, BrokerParseResult currentResult, BrokerLlmSettings settings);
}