using BrokerPriceParser.Core.Review;

namespace BrokerPriceParser.Core.Contracts;

/// <summary>
/// Defines behavior for reading replay messages from an external source.
/// </summary>
public interface IReplayMessageReader
{
    /// <summary>
    /// Reads replay messages from the supplied file path.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    /// <param name="defaultConversationId">The default conversation identifier when a line does not specify one.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A replay message collection.</returns>
    Task<IReadOnlyList<ReplayMessageRecord>> ReadAsync(
        string filePath,
        string defaultConversationId,
        CancellationToken cancellationToken = default);
}