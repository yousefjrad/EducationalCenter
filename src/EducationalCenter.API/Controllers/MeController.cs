using Asp.Versioning;
using EducationalCenter.Application.Features.Enrollments;
using EducationalCenter.Application.Features.StudentPortal;
using Microsoft.AspNetCore.Mvc;

namespace EducationalCenter.API.Controllers;

/// <summary>The student portal: a signed-in student sees only their own data (anyone else gets 404).</summary>
[ApiVersion("1.0")]
public sealed class MeController(IStudentPortalService portal, IStudentBookingService booking) : ApiControllerBase
{
    [HttpGet("enrollments")]
    public async Task<ActionResult<IReadOnlyList<MyEnrollmentDto>>> Enrollments(CancellationToken ct) =>
        Ok(await portal.GetEnrollmentsAsync(ct));

    /// <summary>Holds a seat (Pending) for the signed-in student.</summary>
    [HttpPost("enrollments")]
    [ProducesResponseType(typeof(EnrollmentDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> BookSeat(BookSeatRequest request, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await booking.HoldSeatAsync(request.SectionId, ct));

    /// <summary>Cancels the student's own pending hold.</summary>
    [HttpDelete("enrollments/{id:int}")]
    public async Task<IActionResult> CancelHold(int id, CancellationToken ct)
    {
        await booking.CancelHoldAsync(id, ct);
        return NoContent();
    }

    [HttpGet("enrollments/{id:int}/attendance")]
    public async Task<ActionResult<IReadOnlyList<MyAttendanceDto>>> Attendance(int id, CancellationToken ct) =>
        Ok(await portal.GetAttendanceAsync(id, ct));

    [HttpGet("enrollments/{id:int}/payments")]
    public async Task<ActionResult<MyPaymentsDto>> Payments(int id, CancellationToken ct) =>
        Ok(await portal.GetPaymentsAsync(id, ct));

    [HttpGet("certificates/{id:int}/pdf")]
    public async Task<IActionResult> CertificatePdf(int id, [FromQuery] string language = "ar", CancellationToken ct = default)
    {
        var file = await portal.GetCertificatePdfAsync(id, language, ct);
        return File(file.Content, "application/pdf", file.FileName);
    }

    [HttpGet("receipts/{id:int}/pdf")]
    public async Task<IActionResult> ReceiptPdf(int id, [FromQuery] string language = "ar", CancellationToken ct = default)
    {
        var file = await portal.GetReceiptPdfAsync(id, language, ct);
        return File(file.Content, "application/pdf", file.FileName);
    }
}