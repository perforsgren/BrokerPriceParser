namespace BrokerPriceParser.Core.Models;

/// <summary>
/// Represents how conversation context was used when resolving a message.
/// </summary>
public sealed class ContextUsage
{
    /// <summary>
    /// Gets or sets a value indicating whether context was used.
    /// </summary>
    public bool UsedContext { get; set; }

    /// <summary>
    /// Gets or sets the field names resolved using context.
    /// </summary>
    public IReadOnlyList<string> ResolvedFromContext { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets unresolved references that still require manual review.
    /// </summary>
    public IReadOnlyList<string> UnresolvedReferences { get; set; } = Array.Empty<string>();
}