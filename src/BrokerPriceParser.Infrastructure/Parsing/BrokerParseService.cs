using BrokerPriceParser.Core.Contracts;
using BrokerPriceParser.Core.Enums;
using BrokerPriceParser.Core.Models;
using BrokerPriceParser.Core.State;
using BrokerPriceParser.Core.Validation;

namespace BrokerPriceParser.Infrastructure.Parsing;

/// <summary>
/// Provides the main orchestration entry point for broker parsing.
/// </summary>
public sealed class BrokerParseService : IBrokerParseService
{
    private readonly IBrokerMessageClassifier _classifier;
    private readonly IConversationContextResolver _contextResolver;
    private readonly IBrokerValidationService _validationService;
    private readonly IConfidenceScoringService _confidenceScoringService;

    /// <summary>
    /// Initializes a new instance of the <see cref="BrokerParseService"/> class.
    /// </summary>
    /// <param name="classifier">The message classifier.</param>
    /// <param name="contextResolver">The conversation context resolver.</param>
    /// <param name="validationService">The validation service.</param>
    /// <param name="confidenceScoringService">The confidence scoring service.</param>
    public BrokerParseService(
        IBrokerMessageClassifier classifier,
        IConversationContextResolver contextResolver,
        IBrokerValidationService validationService,
        IConfidenceScoringService confidenceScoringService)
    {
        _classifier = classifier ?? throw new ArgumentNullException(nameof(classifier));
        _contextResolver = contextResolver ?? throw new ArgumentNullException(nameof(contextResolver));
        _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
        _confidenceScoringService = confidenceScoringService ?? throw new ArgumentNullException(nameof(confidenceScoringService));
    }

    /// <summary>
    /// Parses the supplied parse context into a structured result.
    /// </summary>
    /// <param name="context">The parse context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A structured broker parse result.</returns>
    public Task<BrokerParseResult> ParseAsync(ParseContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var messageType = _classifier.Classify(context.NormalizedMessage);

        var result = new BrokerParseResult
        {
            MessageId = context.RawMessage.MessageId,
            MessageType = messageType,
            RawMessage = context.RawMessage.RawText,
            NormalizedMessage = context.NormalizedMessage.NormalizedText
        };

        result = _contextResolver.Resolve(context, result);
        result.EventType = ResolveEventType(result);

        var validationErrors = _validationService.Validate(result);

        result.Quality = new BrokerParseQuality
        {
            ValidationErrors = validationErrors,
            AmbiguityFlags = result.ContextUsage.UnresolvedReferences
        };

        result.Quality.Confidence = _confidenceScoringService.Calculate(result);

        return Task.FromResult(result);
    }

    // ────────────────────────────────────

    /// <summary>
    /// Resolves the semantic event type for a broker parse result.
    /// </summary>
    /// <param name="result">The broker parse result.</param>
    /// <returns>The resolved event type.</returns>
    private static BrokerEventType ResolveEventType(BrokerParseResult result)
    {
        return result.MessageType switch
        {
            BrokerMessageType.InstrumentRequest => BrokerEventType.RequestMarket,
            BrokerMessageType.PriceQuote => BrokerEventType.QuoteProvided,
            BrokerMessageType.PriceUpdate => BrokerEventType.QuoteRevised,
            BrokerMessageType.Clarification => BrokerEventType.ClarificationRequested,
            BrokerMessageType.ActionIntent => ResolveActionEventType(result.Action),
            _ => BrokerEventType.None
        };
    }

    // ────────────────────────────────────

    /// <summary>
    /// Resolves the semantic event type for an action.
    /// </summary>
    /// <param name="action">The parsed action.</param>
    /// <returns>The resolved action event type.</returns>
    private static BrokerEventType ResolveActionEventType(BrokerAction action)
    {
        return action.Side switch
        {
            "ASK" => BrokerEventType.LiftOffer,
            "BID" => BrokerEventType.HitBid,
            _ => BrokerEventType.None
        };
    }
}