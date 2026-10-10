using EducationalCenter.Application.Features.StudentPortal;
using EducationalCenter.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EducationalCenter.Infrastructure.Persistence;

internal sealed class StudentPortalReader(AppDbContext db) : IStudentPortalReader
{
    public async Task<int?> FindStudentIdAsync(int userId, CancellationToken ct = default) =>
        await db.Students.AsNoTracking()
            .Where(s => s.UserId == userId && s.IsActive)
            .Select(s => (int?)s.Id)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<MyEnrollmentDto>> GetEnrollmentsAsync(int studentId, CancellationToken ct = default)
    {
        var rows = await db.Enrollments.AsNoTracking()
            .Where(e => e.StudentId == studentId
                        && e.Status != EnrollmentStatus.Cancelled
                        && e.Status != EnrollmentStatus.Transferred)
            .OrderByDescending(e => e.EnrolledAt)
            .Select(e => new
            {
                e.Id,
                CourseName = e.Section.Course.Name,
                SectionName = e.Section.Name,
                SectionStatus = e.Section.Status,
                e.Section.StartDate,
                e.Section.EndDate,
                e.Status,
                e.HoldExpiresAt,
                Present = e.Attendances.Count(a => a.IsPresent),
                Absent = e.Attendances.Count(a => !a.IsPresent),
                GradeScore = e.Grade == null ? (decimal?)null : e.Grade.Score,
                GradeMax = e.Grade == null ? (decimal?)null : e.Grade.MaxScore,
                GradePassed = e.Grade == null ? (bool?)null : e.Grade.IsPassed,
                CertificateId = e.Certificate == null ? (int?)null : e.Certificate.Id,
                PlanTotal = e.PaymentPlan == null ? (decimal?)null : e.PaymentPlan.TotalAmount,
                Paid = e.PaymentPlan == null
                    ? 0m
                    : e.PaymentPlan.Installments
                        .SelectMany(i => i.Payments)
                        .Where(p => p.Status == PaymentStatus.Valid)
                        .Sum(p => p.AmountInSyp)
            })
            .ToListAsync(ct);

        return rows.Select(r => new MyEnrollmentDto(
            r.Id,
            r.CourseName,
            r.SectionName,
            r.SectionStatus,
            r.StartDate,
            r.EndDate,
            r.Status,
            r.HoldExpiresAt,
            r.Present,
            r.Absent,
            r.GradeScore is null
                ? null
                : new MyGradeDto(
                    r.GradeScore.Value,
                    r.GradeMax!.Value,
                    r.GradeMax.Value == 0 ? 0m : Math.Round(r.GradeScore.Value * 100m / r.GradeMax.Value, 2),
                    r.GradePassed ?? false),
            r.CertificateId,
            r.PlanTotal,
            r.Paid,
            r.PlanTotal is null ? null : r.PlanTotal - r.Paid)).ToList();
    }

    public async Task<IReadOnlyList<MyAttendanceDto>?> GetAttendanceAsync(int studentId, int enrollmentId, CancellationToken ct = default)
    {
        if (!await db.Enrollments.AnyAsync(e => e.Id == enrollmentId && e.StudentId == studentId, ct))
            return null;

        return await db.Attendances.AsNoTracking()
            .Where(a => a.EnrollmentId == enrollmentId)
            .OrderBy(a => a.ClassSession.Date).ThenBy(a => a.ClassSession.StartTime)
            .Select(a => new MyAttendanceDto(
                a.ClassSession.Date,
                a.ClassSession.StartTime,
                a.ClassSession.EndTime,
                a.ClassSession.Status,
                a.IsPresent))
            .ToListAsync(ct);
    }

    public async Task<MyPaymentsDto?> GetPaymentsAsync(int studentId, int enrollmentId, CancellationToken ct = default)
    {
        if (!await db.Enrollments.AnyAsync(e => e.Id == enrollmentId && e.StudentId == studentId, ct))
            return null;

        var plan = await db.PaymentPlans.AsNoTracking()
            .Where(p => p.EnrollmentId == enrollmentId)
            .Select(p => new
            {
                p.TotalAmount,
                Installments = p.Installments.OrderBy(i => i.Number).Select(i => new
                {
                    i.Number,
                    i.Amount,
                    i.DueDate,
                    Payments = i.Payments
                        .Where(x => x.Status == PaymentStatus.Valid)
                        .OrderBy(x => x.PaidAt)
                        .Select(x => new
                        {
                            x.PaidAt,
                            x.Currency,
                            x.AmountPaid,
                            x.AmountInSyp,
                            ReceiptId = x.Receipt == null ? (int?)null : x.Receipt.Id,
                            ReceiptNumber = x.Receipt == null ? null : x.Receipt.ReceiptNumber
                        })
                })
            })
            .FirstOrDefaultAsync(ct);

        if (plan is null)
            return new MyPaymentsDto(enrollmentId, null, 0m, null, []);

        var installments = plan.Installments.Select(i =>
        {
            var payments = i.Payments
                .Select(x => new MyPaymentDto(x.PaidAt, x.Currency, x.AmountPaid, x.AmountInSyp, x.ReceiptId, x.ReceiptNumber))
                .ToList();
            var paid = payments.Sum(x => x.AmountInSyp);
            return new MyInstallmentDto(i.Number, i.Amount, i.DueDate, paid, Math.Max(0m, i.Amount - paid), payments);
        }).ToList();

        var totalPaid = installments.Sum(i => i.PaidAmountInSyp);
        return new MyPaymentsDto(enrollmentId, plan.TotalAmount, totalPaid, plan.TotalAmount - totalPaid, installments);
    }

    public async Task<bool> OwnsCertificateAsync(int studentId, int certificateId, CancellationToken ct = default) =>
        await db.Certificates.AnyAsync(c => c.Id == certificateId && c.Enrollment.StudentId == studentId, ct);

    public async Task<bool> OwnsReceiptAsync(int studentId, int receiptId, CancellationToken ct = default) =>
        await db.Receipts.AnyAsync(
            r => r.Id == receiptId && r.Payment.Installment.PaymentPlan.Enrollment.StudentId == studentId, ct);
}