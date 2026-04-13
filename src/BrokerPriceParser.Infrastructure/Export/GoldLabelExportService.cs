using System.IO;
using System.Text;
using System.Text.Json;
using BrokerPriceParser.Core.Contracts;
using BrokerPriceParser.Core.Export;
using BrokerPriceParser.Core.Models;
using BrokerPriceParser.Core.Review;
using System.Text.Json.Serialization;

namespace BrokerPriceParser.Infrastructure.Export;

/// <summary>
/// Exports reviewed queue items to JSONL gold-label files.
/// </summary>
public sealed class GoldLabelExportService : IGoldLabelExportService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = false,
        PropertyNameCaseInsensitive = true,
        Converters =
    {
        new JsonStringEnumConverter()
    }
    };

    /// <summary>
    /// Exports reviewed queue items to a JSONL gold-label file.
    /// </summary>
    /// <param name="filePath">The output file path.</param>
    /// <param name="items">The review queue items to export.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task ExportAsync(
        string filePath,
        IReadOnlyList<ReviewQueueItem> items,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(items);

        var reviewedItems = items
            .Where(x => x.ReviewStatus is ReviewStatus.Accepted or ReviewStatus.Corrected)
            .ToArray();

        using var streamWriter = new StreamWriter(filePath, false, Encoding.UTF8);

        foreach (var item in reviewedItems)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!TryDeserializeResult(item.CurrentResultJson, out var result))
            {
                continue;
            }

            var record = new GoldLabelRecord
            {
                SequenceNumber = item.SequenceNumber,
                MessageId = item.MessageId,
                ConversationId = item.ConversationId,
                RawText = item.RawText,
                NormalizedText = item.NormalizedText,
                ReviewStatus = item.ReviewStatus,
                HasManualOverride = item.HasManualOverride,
                ManualNotes = item.ManualNotes,
                Result = result
            };

            var jsonLine = JsonSerializer.Serialize(record, SerializerOptions);
            await streamWriter.WriteLineAsync(jsonLine).ConfigureAwait(false);
        }
    }

    // ────────────────────────────────────

    /// <summary>
    /// Deserializes the current result JSON into a broker parse result.
    /// </summary>
    /// <param name="json">The JSON payload.</param>
    /// <param name="result">The deserialized parse result.</param>
    /// <returns><c>true</c> if deserialization succeeded; otherwise <c>false</c>.</returns>
    private static bool TryDeserializeResult(string json, out BrokerParseResult result)
    {
        try
        {
            result = JsonSerializer.Deserialize<BrokerParseResult>(json, SerializerOptions) ?? new();
            return true;
        }
        catch
        {
            result = new BrokerParseResult();
            return false;
        }
    }
}