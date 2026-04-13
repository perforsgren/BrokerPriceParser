using System.IO;
using BrokerPriceParser.Core.Review;
using BrokerPriceParser.Infrastructure.Review;
using Xunit;

namespace BrokerPriceParser.Tests.Review;

/// <summary>
/// Contains tests for <see cref="ReviewDecisionPersistenceService"/>.
/// </summary>
public sealed class ReviewDecisionPersistenceServiceTests
{
    /// <summary>
    /// Verifies that review queue items can be saved and loaded.
    /// </summary>
    [Fact]
    public async Task SaveAsync_ThenLoadAsync_ShouldRoundTripItems()
    {
        var filePath = Path.GetTempFileName();

        try
        {
            var service = new ReviewDecisionPersistenceService();

            var items = new[]
            {
                new ReviewQueueItem
                {
                    SequenceNumber = 1,
                    MessageId = "MSG-001",
                    ConversationId = "CONV-001",
                    RawText = "mom int",
                    NormalizedText = "MOM INT",
                    ReviewStatus = ReviewStatus.Corrected,
                    HasManualOverride = true,
                    ManualNotes = "manual fix",
                    OriginalResultJson = "{}",
                    CurrentResultJson = "{\"messageId\":\"MSG-001\"}"
                }
            };

            await service.SaveAsync(filePath, items);
            var loaded = await service.LoadAsync(filePath);

            Assert.Single(loaded);
            Assert.Equal("MSG-001", loaded[0].MessageId);
            Assert.True(loaded[0].HasManualOverride);
            Assert.Equal("manual fix", loaded[0].ManualNotes);
            Assert.Equal(ReviewStatus.Corrected, loaded[0].ReviewStatus);
        }
        finally
        {
            File.Delete(filePath);
        }
    }
}