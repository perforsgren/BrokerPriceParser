using BrokerPriceParser.Core.Enums;
using BrokerPriceParser.Core.Models;
using BrokerPriceParser.Core.Review;
using BrokerPriceParser.Core.Validation;
using BrokerPriceParser.Infrastructure.Review;
using Xunit;

namespace BrokerPriceParser.Tests.Review;

/// <summary>
/// Contains tests for <see cref="ReviewQueueService"/>.
/// </summary>
public sealed class ReviewQueueServiceTests
{
    /// <summary>
    /// Verifies that low-confidence unknown results are routed to review.
    /// </summary>
    [Fact]
    public void Create_ShouldRequireReview_ForLowConfidenceUnknownResult()
    {
        var service = new ReviewQueueService();

        var replayRecord = new ReplayMessageRecord
        {
            SequenceNumber = 1,
            ConversationId = "CONV-001",
            RawText = "mom int"
        };

        var normalizedMessage = new NormalizedBrokerMessage
        {
            NormalizedText = "MOM INT"
        };

        var result = new BrokerParseResult
        {
            MessageId = "MSG-001",
            MessageType = BrokerMessageType.Unknown,
            EventType = BrokerEventType.None,
            RawMessage = "mom int",
            NormalizedMessage = "MOM INT",
            Quality = new BrokerParseQuality
            {
                Confidence = 0.20,
                ValidationErrors = Array.Empty<string>(),
                AmbiguityFlags = Array.Empty<string>()
            }
        };

        var item = service.Create(replayRecord, normalizedMessage, result, 0.55);

        Assert.True(item.RequiresReview);
        Assert.Contains("UnknownMessageType", item.ReviewReason);
        Assert.Contains("LowConfidence<0.55", item.ReviewReason);
        Assert.Equal(ReviewStatus.Unreviewed, item.ReviewStatus);
    }

    // ────────────────────────────────────

    /// <summary>
    /// Verifies that a clean high-confidence result does not require review.
    /// </summary>
    [Fact]
    public void Create_ShouldNotRequireReview_ForCleanHighConfidenceResult()
    {
        var service = new ReviewQueueService();

        var replayRecord = new ReplayMessageRecord
        {
            SequenceNumber = 2,
            ConversationId = "CONV-001",
            RawText = "0.10/0.30"
        };

        var normalizedMessage = new NormalizedBrokerMessage
        {
            NormalizedText = "0.10/0.30"
        };

        var result = new BrokerParseResult
        {
            MessageId = "MSG-002",
            MessageType = BrokerMessageType.PriceQuote,
            EventType = BrokerEventType.QuoteProvided,
            RawMessage = "0.10/0.30",
            NormalizedMessage = "0.10/0.30",
            Quality = new BrokerParseQuality
            {
                Confidence = 0.90,
                ValidationErrors = Array.Empty<string>(),
                AmbiguityFlags = Array.Empty<string>()
            }
        };

        var item = service.Create(replayRecord, normalizedMessage, result, 0.55);

        Assert.False(item.RequiresReview);
        Assert.Equal("No review required", item.ReviewReason);
    }
}