using System.IO;
using System.Text.Json;
using BrokerPriceParser.Core.Contracts;
using BrokerPriceParser.Core.Review;
using System.Text.Json.Serialization;

namespace BrokerPriceParser.Infrastructure.Review;

/// <summary>
/// Saves and loads persisted review queue snapshots.
/// </summary>
public sealed class ReviewDecisionPersistenceService : IReviewDecisionPersistenceService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    /// <summary>
    /// Saves the current review queue snapshot to a file.
    /// </summary>
    /// <param name="filePath">The output file path.</param>
    /// <param name="items">The review queue items to save.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task SaveAsync(
        string filePath,
        IReadOnlyList<ReviewQueueItem> items,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(items);

        var snapshot = new ReviewQueueSnapshot
        {
            SavedUtc = DateTime.UtcNow,
            Items = items.ToArray()
        };

        var json = JsonSerializer.Serialize(snapshot, SerializerOptions);
        await File.WriteAllTextAsync(filePath, json, cancellationToken).ConfigureAwait(false);
    }

    // ────────────────────────────────────

    /// <summary>
    /// Loads a persisted review queue snapshot from a file.
    /// </summary>
    /// <param name="filePath">The input file path.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The loaded review queue items.</returns>
    public async Task<IReadOnlyList<ReviewQueueItem>> LoadAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var json = await File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);
        var snapshot = JsonSerializer.Deserialize<ReviewQueueSnapshot>(json, SerializerOptions);

        return snapshot?.Items?.ToArray() ?? Array.Empty<ReviewQueueItem>();
    }
}