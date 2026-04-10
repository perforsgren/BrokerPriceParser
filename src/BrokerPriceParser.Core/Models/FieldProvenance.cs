using BrokerPriceParser.Core.Enums;

namespace BrokerPriceParser.Core.Models;

/// <summary>
/// Represents provenance metadata for resolved parser fields.
/// </summary>
public sealed class FieldProvenance
{
    /// <summary>
    /// Gets or sets the source for the pair field.
    /// </summary>
    public FieldSourceType PairSource { get; set; } = FieldSourceType.None;

    /// <summary>
    /// Gets or sets the source for the tenor field.
    /// </summary>
    public FieldSourceType TenorSource { get; set; } = FieldSourceType.None;

    /// <summary>
    /// Gets or sets the source for the structure field.
    /// </summary>
    public FieldSourceType StructureSource { get; set; } = FieldSourceType.None;

    /// <summary>
    /// Gets or sets the source for the price field.
    /// </summary>
    public FieldSourceType PriceSource { get; set; } = FieldSourceType.None;

    /// <summary>
    /// Gets or sets the source for the action field.
    /// </summary>
    public FieldSourceType ActionSource { get; set; } = FieldSourceType.None;
}