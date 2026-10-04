namespace EducationalCenter.Application.Common.Models;

public sealed record ReportSummaryLine(string Label, string Value);

/// <summary>
/// A report flattened to text, already translated and formatted, so Excel and PDF exporters
/// stay generic. <see cref="Language"/> ("ar" or "en") tells them whether to lay the page out right-to-left.
/// </summary>
public sealed record ReportTable(
    string Title,
    string Language,
    IReadOnlyList<string> Columns,
    IReadOnlyList<IReadOnlyList<string>> Rows,
    IReadOnlyList<ReportSummaryLine> Summary);
