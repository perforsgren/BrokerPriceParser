using System.IO;
using BrokerPriceParser.Core.Review;
using BrokerPriceParser.Infrastructure.Export;
using Xunit;

namespace BrokerPriceParser.Tests.Export;

/// <summary>
/// Contains tests for <see cref="GoldLabelExportService"/>.
/// </summary>
public sealed class GoldLabelExportServiceTests
{
    /// <summary>
    /// Verifies that only accepted or corrected items are exported as gold labels.
    /// </summary>
    [Fact]
    public async Task ExportAsync_ShouldExportOnlyReviewedItems()
    {
        var filePath = Path.GetTempFileName();

        try
        {
            var service = new GoldLabelExportService();

            var items = new[]
            {
                new ReviewQueueItem
                {
                    SequenceNumber = 1,
                    MessageId = "MSG-001",
                    ConversationId = "CONV-001",
                    RawText = "row 1",
                    NormalizedText = "ROW 1",
                    ReviewStatus = ReviewStatus.Accepted,
                    CurrentResultJson = """
                    {
                      "messageId": "MSG-001",
                      "messageType": "PriceQuote",
                      "eventType": "QuoteProvided",
                      "rawMessage": "row 1",
                      "normalizedMessage": "ROW 1",
                      "instrument": {},
                      "quote": {},
                      "action": {},
                      "contextUsage": {},
                      "provenance": {},
                      "quality": {}
                    }
                    """
                },
                new ReviewQueueItem
                {
                    SequenceNumber = 2,
                    MessageId = "MSG-002",
                    ConversationId = "CONV-001",
                    RawText = "row 2",
                    NormalizedText = "ROW 2",
                    ReviewStatus = ReviewStatus.Unreviewed,
                    CurrentResultJson = "{}"
                }
            };

            await service.ExportAsync(filePath, items);

            var lines = await File.ReadAllLinesAsync(filePath);

            Assert.Single(lines);
            Assert.Contains("\"MessageId\":\"MSG-001\"", lines[0]);
        }
        finally
        {
            File.Delete(filePath);
        }
    }
}