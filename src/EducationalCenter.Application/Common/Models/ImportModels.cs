namespace EducationalCenter.Application.Common.Models;

/// <summary>One data row read from an uploaded workbook. Cell values are trimmed text ("" when blank).</summary>
public sealed record ImportSheetRow(int RowNumber, IReadOnlyDictionary<string, string> Cells)
{
    /// <remarks>The dictionary must be case-insensitive on column names.</remarks>
    public string Get(string column) => Cells.TryGetValue(column, out var value) ? value : string.Empty;
}

public sealed record ImportSheet(IReadOnlyList<ImportSheetRow> Rows);

public sealed record ImportTemplateColumn(string Name, bool Required, string Hint);

/// <param name="Notes">Free-text instructions shown above the column table.</param>
/// <param name="SampleRows">Example rows (same order as Columns), shown on the instructions sheet only.</param>
public sealed record ImportTemplate(
    string Title,
    string Language,
    IReadOnlyList<ImportTemplateColumn> Columns,
    IReadOnlyList<IReadOnlyList<string>> SampleRows,
    IReadOnlyList<string> Notes);
