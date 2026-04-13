using System.IO;
using BrokerPriceParser.Infrastructure.Replay;
using Xunit;

namespace BrokerPriceParser.Tests.Replay;

/// <summary>
/// Contains tests for <see cref="TextReplayMessageReader"/>.
/// </summary>
public sealed class TextReplayMessageReaderTests
{
    /// <summary>
    /// Verifies that the reader supports both default conversation IDs and explicit conversation IDs.
    /// </summary>
    [Fact]
    public async Task ReadAsync_ShouldParseReplayFileLines()
    {
        var filePath = Path.GetTempFileName();

        try
        {
            await File.WriteAllLinesAsync(filePath, new[]
            {
                "# Comment",
                "",
                "NOK/SEK 1Y 25 delta rr pls",
                "CONV-002|0.10/0.30",
                "CONV-002|ok take"
            });

            var reader = new TextReplayMessageReader();
            var records = await reader.ReadAsync(filePath, "DEFAULT-CONV");

            Assert.Equal(3, records.Count);
            Assert.Equal("DEFAULT-CONV", records[0].ConversationId);
            Assert.Equal("NOK/SEK 1Y 25 delta rr pls", records[0].RawText);
            Assert.Equal("CONV-002", records[1].ConversationId);
            Assert.Equal("0.10/0.30", records[1].RawText);
            Assert.Equal("ok take", records[2].RawText);
        }
        finally
        {
            File.Delete(filePath);
        }
    }
}