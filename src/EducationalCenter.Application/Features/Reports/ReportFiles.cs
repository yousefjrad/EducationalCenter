using System.Globalization;
using EducationalCenter.Application.Common.Interfaces;
using EducationalCenter.Application.Common.Models;

namespace EducationalCenter.Application.Features.Reports;

internal static class ReportFiles
{
    private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private const string PdfContentType = "application/pdf";

    public static ReportFileDto Create(
        ReportTable table, ReportFormat format, string reportName, DateTime utcNow, IReportExporter exporter)
    {
        var stamp = utcNow.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

        return format == ReportFormat.Excel
            ? new ReportFileDto(exporter.ToExcel(table), $"{reportName}-{stamp}.xlsx", ExcelContentType)
            : new ReportFileDto(exporter.ToPdf(table), $"{reportName}-{stamp}.pdf", PdfContentType);
    }
}
