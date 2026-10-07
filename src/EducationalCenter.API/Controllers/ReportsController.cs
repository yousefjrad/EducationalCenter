using Asp.Versioning;
using EducationalCenter.API.Authorization;
using EducationalCenter.Application.Features.Reports;
using EducationalCenter.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace EducationalCenter.API.Controllers;

[ApiVersion("1.0")]
public sealed class ReportsController(
    IFinancialReportService financial,
    IOperationalReportService operational) : ApiControllerBase
{
    // ---------- Financial (Admin) ----------

    [HttpGet("financial/monthly-revenue")]
    [HasPermission(Permissions.Reports.Financial)]
    public async Task<ActionResult<MonthlyRevenueReportDto>> MonthlyRevenue([FromQuery] MonthlyRevenueQuery query, CancellationToken ct) =>
        Ok(await financial.GetMonthlyRevenueAsync(query, ct));

    [HttpGet("financial/student-balances")]
    [HasPermission(Permissions.Reports.Financial)]
    public async Task<ActionResult<StudentBalancesReportDto>> StudentBalances([FromQuery] StudentBalancesQuery query, CancellationToken ct) =>
        Ok(await financial.GetStudentBalancesAsync(query, ct));

    [HttpGet("financial/overdue-installments")]
    [HasPermission(Permissions.Reports.Financial)]
    public async Task<ActionResult<OverdueInstallmentsReportDto>> OverdueInstallments([FromQuery] OverdueInstallmentsQuery query, CancellationToken ct) =>
        Ok(await financial.GetOverdueInstallmentsAsync(query, ct));

    /// <summary>Cash basis: payments received, minus expenses and paid trainer payrolls.</summary>
    [HttpGet("financial/net-profit")]
    [HasPermission(Permissions.Reports.Financial)]
    public async Task<ActionResult<NetProfitReportDto>> NetProfit([FromQuery] NetProfitQuery query, CancellationToken ct) =>
        Ok(await financial.GetNetProfitAsync(query, ct));

    /// <summary>The same data as the JSON reports, as an Excel or PDF file ("ar" or "en").</summary>
    [HttpGet("financial/export")]
    [HasPermission(Permissions.Reports.Financial)]
    public async Task<IActionResult> ExportFinancial([FromQuery] FinancialReportExportRequest request, CancellationToken ct)
    {
        var file = await financial.ExportAsync(request, ct);
        return File(file.Content, file.ContentType, file.FileName);
    }

    // ---------- Operational (receptionist and Admin) ----------

    [HttpGet("operational/section-occupancy")]
    [HasPermission(Permissions.Reports.Operational)]
    public async Task<ActionResult<SectionOccupancyReportDto>> SectionOccupancy([FromQuery] SectionOccupancyQuery query, CancellationToken ct) =>
        Ok(await operational.GetSectionOccupancyAsync(query, ct));

    [HttpGet("operational/room-schedule")]
    [HasPermission(Permissions.Reports.Operational)]
    public async Task<ActionResult<ScheduleReportDto>> RoomSchedule([FromQuery] RoomScheduleQuery query, CancellationToken ct) =>
        Ok(await operational.GetRoomScheduleAsync(query, ct));

    [HttpGet("operational/trainer-schedule")]
    [HasPermission(Permissions.Reports.Operational)]
    public async Task<ActionResult<ScheduleReportDto>> TrainerSchedule([FromQuery] TrainerScheduleQuery query, CancellationToken ct) =>
        Ok(await operational.GetTrainerScheduleAsync(query, ct));

    [HttpGet("operational/export")]
    [HasPermission(Permissions.Reports.Operational)]
    public async Task<IActionResult> ExportOperational([FromQuery] OperationalReportExportRequest request, CancellationToken ct)
    {
        var file = await operational.ExportAsync(request, ct);
        return File(file.Content, file.ContentType, file.FileName);
    }
}
