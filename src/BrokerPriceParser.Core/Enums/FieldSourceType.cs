namespace BrokerPriceParser.Core.Enums;

/// <summary>
/// Represents where a resolved field value came from.
/// </summary>
public enum FieldSourceType
{
    /// <summary>
    /// The field is not set.
    /// </summary>
    None = 0,

    /// <summary>
    /// The field was explicitly present in the message text.
    /// </summary>
    Explicit = 1,

    /// <summary>
    /// The field was inferred from conversation state or context.
    /// </summary>
    InferredFromContext = 2,

    /// <summary>
    /// The field was defaulted by system logic.
    /// </summary>
    Derived = 3
}