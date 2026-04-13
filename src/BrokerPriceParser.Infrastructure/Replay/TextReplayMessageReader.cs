using System.IO;
using BrokerPriceParser.Core.Contracts;
using BrokerPriceParser.Core.Review;

namespace BrokerPriceParser.Infrastructure.Replay;

/// <summary>
/// Reads replay messages from a text file.
/// </summary>
public sealed class TextReplayMessageReader : IReplayMessageReader
{
    /// <summary>
    /// Reads replay messages from the supplied file path.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    /// <param name="defaultConversationId">The default conversation identifier when a line does not specify one.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A replay message collection.</returns>
    public async Task<IReadOnlyList<ReplayMessageRecord>> ReadAsync(
        string filePath,
        string defaultConversationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultConversationId);

        var lines = await File.ReadAllLinesAsync(filePath, cancellationToken).ConfigureAwait(false);
        var result = new List<ReplayMessageRecord>();
        var sourceName = Path.GetFileName(filePath);

        var sequenceNumber = 0;
        var timestamp = DateTime.UtcNow;

        foreach (var rawLine in lines)
        {
            var line = rawLine?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (line.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            sequenceNumber++;

            var conversationId = defaultConversationId;
            var rawText = line;

            var separatorIndex = line.IndexOf('|');

            if (separatorIndex > 0 && separatorIndex < line.Length - 1)
            {
                var parsedConversationId = line[..separatorIndex].Trim();
                var parsedRawText = line[(separatorIndex + 1)..].Trim();

                if (!string.IsNullOrWhiteSpace(parsedConversationId))
                {
                    conversationId = parsedConversationId;
                }

                if (!string.IsNullOrWhiteSpace(parsedRawText))
                {
                    rawText = parsedRawText;
                }
            }

            result.Add(new ReplayMessageRecord
            {
                SequenceNumber = sequenceNumber,
                ConversationId = conversationId,
                Source = sourceName,
                Broker = "ReplayFile",
                RawText = rawText,
                ReceivedUtc = timestamp.AddMilliseconds(sequenceNumber)
            });
        }

        return result;
    }
}