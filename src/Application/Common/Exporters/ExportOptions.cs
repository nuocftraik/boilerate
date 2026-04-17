using System.Collections.Generic;

namespace Boilerate.Application.Common.Exporters;

/// <summary>
/// Options for Excel export styling
/// </summary>
public class ExportOptions
{
    /// <summary>
    /// Title row text (optional)
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Freeze header row
    /// </summary>
    public bool FreezeHeader { get; set; } = true;

    /// <summary>
    /// Auto-filter on header row
    /// </summary>
    public bool AutoFilter { get; set; } = true;

    /// <summary>
    /// Auto-fit column widths
    /// </summary>
    public bool AutoFitColumns { get; set; } = true;

    /// <summary>
    /// Bold header row
    /// </summary>
    public bool BoldHeaders { get; set; } = true;

    /// <summary>
    /// Show gridlines
    /// </summary>
    public bool ShowGridLines { get; set; } = true;

    /// <summary>
    /// Column definitions (nếu null, auto-detect từ properties)
    /// </summary>
    public List<ExportColumn>? Columns { get; set; }
}
