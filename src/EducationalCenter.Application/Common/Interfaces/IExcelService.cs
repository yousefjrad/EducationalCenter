using EducationalCenter.Application.Common.Models;

namespace EducationalCenter.Application.Common.Interfaces;

/// <summary>Implemented in Infrastructure (the Excel library is chosen there).</summary>
public interface IExcelService
{
    /// <summary>
    /// Builds an .xlsx with two sheets: "Data" holding only the header row (the column names, nothing else,
    /// so leftover sample rows can never be imported) and "Instructions" holding the title, notes,
    /// the column table (name, required, hint) and the sample rows. Right-to-left when Language is "ar".
    /// </summary>
    byte[] CreateTemplate(ImportTemplate template);

    /// <summary>
    /// Reads the first sheet. Row 1 is the header; header names are matched case-insensitively and every
    /// name in <paramref name="expectedColumns"/> must exist, otherwise throw RequestValidationException.
    /// Fully blank rows are skipped. RowNumber is the Excel row number. Cell text is normalised:
    /// dates as "yyyy-MM-dd", times as "HH:mm", numbers in invariant culture without thousands separators.
    /// Throw RequestValidationException if the stream is not a valid .xlsx.
    /// </summary>
    ImportSheet ReadSheet(Stream file, IReadOnlyList<string> expectedColumns);
}
