using BrokerPriceParser.Core.Models;

namespace BrokerPriceParser.Core.Contracts;

/// <summary>
/// Defines behavior for normalizing raw broker messages.
/// </summary>
public interface IBrokerMessageNormalizer
{
    /// <summary>
    /// Normalizes a raw broker message into a structured normalized message.
    /// </summary>
    /// <param name="message">The raw message to normalize.</param>
    /// <returns>A normalized broker message.</returns>
    NormalizedBrokerMessage Normalize(RawBrokerMessage message);
}