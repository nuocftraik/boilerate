namespace Boilerate.Application.Common.Exporters;

/// <summary>
/// Column definition for Excel export
/// </summary>
public class ExportColumn
{
    /// <summary>
    /// Property name to read value from
    /// </summary>
    public string PropertyName { get; set; } = default!;

    /// <summary>
    /// Header text to display
    /// </summary>
    public string Header { get; set; } = default!;

    /// <summary>
    /// Format string (e.g., "N2" for numbers, "dd/MM/yyyy" for dates)
    /// </summary>
    public string? Format { get; set; }

    /// <summary>
    /// Column width in characters (auto if null)
    /// </summary>
    public double? Width { get; set; }
}
