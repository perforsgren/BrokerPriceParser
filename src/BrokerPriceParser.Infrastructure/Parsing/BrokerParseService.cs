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
    private readonly IBrokerValidationService _validationService;
    private readonly IConfidenceScoringService _confidenceScoringService;

    /// <summary>
    /// Initializes a new instance of the <see cref="BrokerParseService"/> class.
    /// </summary>
    /// <param name="classifier">The message classifier.</param>
    /// <param name="validationService">The validation service.</param>
    /// <param name="confidenceScoringService">The confidence scoring service.</param>
    public BrokerParseService(
        IBrokerMessageClassifier classifier,
        IBrokerValidationService validationService,
        IConfidenceScoringService confidenceScoringService)
    {
        _classifier = classifier ?? throw new ArgumentNullException(nameof(classifier));
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
            EventType = MapEventType(messageType),
            RawMessage = context.RawMessage.RawText,
            NormalizedMessage = context.NormalizedMessage.NormalizedText
        };

        var validationErrors = _validationService.Validate(result);

        result.Quality = new BrokerParseQuality
        {
            ValidationErrors = validationErrors,
            AmbiguityFlags = Array.Empty<string>()
        };

        result.Quality.Confidence = _confidenceScoringService.Calculate(result);

        return Task.FromResult(result);
    }

    // ────────────────────────────────────

    /// <summary>
    /// Maps a message type to a default semantic event type.
    /// </summary>
    /// <param name="messageType">The message type.</param>
    /// <returns>The mapped event type.</returns>
    private static BrokerEventType MapEventType(BrokerMessageType messageType)
    {
        return messageType switch
        {
            BrokerMessageType.InstrumentRequest => BrokerEventType.RequestMarket,
            BrokerMessageType.PriceQuote => BrokerEventType.QuoteProvided,
            BrokerMessageType.PriceUpdate => BrokerEventType.QuoteRevised,
            BrokerMessageType.ActionIntent => BrokerEventType.ClarificationRequested,
            BrokerMessageType.Clarification => BrokerEventType.ClarificationRequested,
            _ => BrokerEventType.None
        };
    }
}