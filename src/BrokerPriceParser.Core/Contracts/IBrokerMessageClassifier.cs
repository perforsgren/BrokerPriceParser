using BrokerPriceParser.Core.Enums;
using BrokerPriceParser.Core.Models;

namespace BrokerPriceParser.Core.Contracts;

/// <summary>
/// Defines behavior for classifying normalized broker messages.
/// </summary>
public interface IBrokerMessageClassifier
{
    /// <summary>
    /// Classifies a normalized broker message.
    /// </summary>
    /// <param name="message">The normalized message to classify.</param>
    /// <returns>The resolved broker message type.</returns>
    BrokerMessageType Classify(NormalizedBrokerMessage message);
}