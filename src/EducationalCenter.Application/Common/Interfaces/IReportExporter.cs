using EducationalCenter.Application.Common.Models;

namespace EducationalCenter.Application.Common.Interfaces;

/// <summary>Implemented in Infrastructure (Excel and PDF libraries are chosen there).</summary>
public interface IReportExporter
{
    byte[] ToExcel(ReportTable table);
    byte[] ToPdf(ReportTable table);
}
