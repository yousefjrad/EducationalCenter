using EducationalCenter.Application.Common.Interfaces.Repositories;
using EducationalCenter.Domain.Entities;
using EducationalCenter.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EducationalCenter.Infrastructure.Persistence.Repositories;

internal sealed class StudentRepository(AppDbContext context) : Repository<Student>(context), IStudentRepository
{
    public Task<bool> ExistsAsync(string fullName, string phoneNumber, int? excludeId, CancellationToken ct = default) =>
        Set.AnyAsync(s => s.FullName == fullName && s.PhoneNumber == phoneNumber
                          && (excludeId == null || s.Id != excludeId), ct);

    public Task<Student?> FindByNameAndPhoneAsync(string fullName, string phoneNumber, CancellationToken ct = default) =>
        Set.AsNoTracking().FirstOrDefaultAsync(s => s.FullName == fullName && s.PhoneNumber == phoneNumber, ct);

    public async Task<bool> HasEnrollmentsAsync(int studentId, CancellationToken ct = default) =>
        await Db.Enrollments.AnyAsync(e => e.StudentId == studentId, ct)
        || await Db.WaitingListEntries.AnyAsync(w => w.StudentId == studentId, ct);

    public Task<(IReadOnlyList<Student> Items, int TotalCount)> SearchAsync(
        string? search, bool? isActive, int page, int pageSize, CancellationToken ct = default)
    {
        var query = Set.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(s => s.FullName.Contains(search) || s.PhoneNumber.Contains(search));
        if (isActive.HasValue)
            query = query.Where(s => s.IsActive == isActive.Value);

        return query.OrderBy(s => s.FullName).ThenBy(s => s.Id).ToPageAsync(page, pageSize, ct);
    }
}

internal sealed class EnrollmentRepository(AppDbContext context) : Repository<Enrollment>(context), IEnrollmentRepository
{
    public Task<int> CountSeatsTakenAsync(int sectionId, DateTime utcNow, CancellationToken ct = default) =>
        Set.CountAsync(e => e.SectionId == sectionId
                            && (e.Status == EnrollmentStatus.Confirmed
                                || (e.Status == EnrollmentStatus.Pending && e.HoldExpiresAt > utcNow)), ct);

    public Task<bool> HasActiveEnrollmentAsync(int studentId, int sectionId, DateTime utcNow, CancellationToken ct = default) =>
        Set.AnyAsync(e => e.StudentId == studentId && e.SectionId == sectionId
                          && (e.Status == EnrollmentStatus.Confirmed
                              || e.Status == EnrollmentStatus.Completed
                              || (e.Status == EnrollmentStatus.Pending && e.HoldExpiresAt > utcNow)), ct);

    public Task<Enrollment?> GetWithDetailsAsync(int id, CancellationToken ct = default) =>
        Set.Include(e => e.Student)
            .Include(e => e.Section).ThenInclude(s => s.Course)
            .Include(e => e.PaymentPlan)
            .Include(e => e.Certificate)
            .Include(e => e.Grade)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<IReadOnlyList<Enrollment>> GetExpiredHoldsAsync(DateTime utcNow, CancellationToken ct = default) =>
        await Set.Where(e => e.Status == EnrollmentStatus.Pending && e.HoldExpiresAt <= utcNow).ToListAsync(ct);

    public Task<(IReadOnlyList<Enrollment> Items, int TotalCount)> SearchAsync(
        int? studentId, int? sectionId, EnrollmentStatus? status, int page, int pageSize, CancellationToken ct = default)
    {
        var query = Set.AsNoTracking()
            .Include(e => e.Student)
            .Include(e => e.Section).ThenInclude(s => s.Course)
            .AsQueryable();

        if (studentId.HasValue)
            query = query.Where(e => e.StudentId == studentId.Value);
        if (sectionId.HasValue)
            query = query.Where(e => e.SectionId == sectionId.Value);
        if (status.HasValue)
            query = query.Where(e => e.Status == status.Value);

        return query.OrderByDescending(e => e.EnrolledAt).ThenByDescending(e => e.Id).ToPageAsync(page, pageSize, ct);
    }

    public async Task<IReadOnlyList<Enrollment>> GetEnrolledBySectionAsync(int sectionId, CancellationToken ct = default) =>
        await Set.Include(e => e.Student)
            .Where(e => e.SectionId == sectionId
                        && (e.Status == EnrollmentStatus.Confirmed || e.Status == EnrollmentStatus.Completed))
            .OrderBy(e => e.Student.FullName)
            .ToListAsync(ct);

    public async Task<decimal> GetPaidAmountInSypAsync(int enrollmentId, CancellationToken ct = default)
    {
        var total = await Db.Payments
            .Where(p => p.Status == PaymentStatus.Valid && p.Installment.PaymentPlan.EnrollmentId == enrollmentId)
            .SumAsync(p => (decimal?)p.AmountInSyp, ct);

        return total ?? 0m;
    }

    public Task<Enrollment?> GetForLegacyImportAsync(int studentId, int sectionId, CancellationToken ct = default) =>
        Set.Include(e => e.PaymentPlan).ThenInclude(p => p!.Installments).ThenInclude(i => i.Payments)
            .AsSplitQuery()
            .FirstOrDefaultAsync(e => e.StudentId == studentId && e.SectionId == sectionId
                                      && (e.Status == EnrollmentStatus.Confirmed || e.Status == EnrollmentStatus.Completed), ct);
}

internal sealed class WaitingListRepository(AppDbContext context) : Repository<WaitingListEntry>(context), IWaitingListRepository
{
    public Task<WaitingListEntry?> GetWithDetailsAsync(int id, CancellationToken ct = default) =>
        Set.Include(w => w.Student).Include(w => w.Section).FirstOrDefaultAsync(w => w.Id == id, ct);

    public async Task<IReadOnlyList<WaitingListEntry>> GetWaitingBySectionAsync(int sectionId, CancellationToken ct = default) =>
        await Set.Where(w => w.SectionId == sectionId && w.Status == WaitingListStatus.Waiting)
            .OrderBy(w => w.Position)
            .ToListAsync(ct);

    public Task<WaitingListEntry?> GetWaitingEntryAsync(int studentId, int sectionId, CancellationToken ct = default) =>
        Set.FirstOrDefaultAsync(w => w.StudentId == studentId && w.SectionId == sectionId
                                     && w.Status == WaitingListStatus.Waiting, ct);

    public Task<(IReadOnlyList<WaitingListEntry> Items, int TotalCount)> SearchAsync(
        int? sectionId, int? studentId, WaitingListStatus? status, int page, int pageSize, CancellationToken ct = default)
    {
        var query = Set.AsNoTracking().Include(w => w.Student).Include(w => w.Section).AsQueryable();

        if (sectionId.HasValue)
            query = query.Where(w => w.SectionId == sectionId.Value);
        if (studentId.HasValue)
            query = query.Where(w => w.StudentId == studentId.Value);
        if (status.HasValue)
            query = query.Where(w => w.Status == status.Value);

        return query.OrderBy(w => w.SectionId).ThenBy(w => w.Status).ThenBy(w => w.Position).ThenBy(w => w.AddedAt)
            .ToPageAsync(page, pageSize, ct);
    }
}

internal sealed class AttendanceRepository(AppDbContext context) : Repository<Attendance>(context), IAttendanceRepository
{
    public async Task<IReadOnlyList<Attendance>> GetBySessionAsync(int sessionId, CancellationToken ct = default) =>
        await Set.Where(a => a.ClassSessionId == sessionId).ToListAsync(ct);

    public async Task<IReadOnlyList<Attendance>> GetByEnrollmentAsync(int enrollmentId, CancellationToken ct = default) =>
        await Set.AsNoTracking()
            .Include(a => a.ClassSession)
            .Where(a => a.EnrollmentId == enrollmentId)
            .OrderBy(a => a.ClassSession.Date).ThenBy(a => a.ClassSession.StartTime)
            .ToListAsync(ct);
}

internal sealed class GradeRepository(AppDbContext context) : Repository<Grade>(context), IGradeRepository
{
    public Task<Grade?> GetByEnrollmentAsync(int enrollmentId, CancellationToken ct = default) =>
        Set.AsNoTracking()
            .Include(g => g.Enrollment).ThenInclude(e => e.Student)
            .Include(g => g.Enrollment).ThenInclude(e => e.Section)
            .FirstOrDefaultAsync(g => g.EnrollmentId == enrollmentId, ct);

    public async Task<IReadOnlyList<Grade>> GetBySectionAsync(int sectionId, CancellationToken ct = default) =>
        await Set.AsNoTracking()
            .Include(g => g.Enrollment).ThenInclude(e => e.Student)
            .Include(g => g.Enrollment).ThenInclude(e => e.Section)
            .Where(g => g.Enrollment.SectionId == sectionId)
            .OrderBy(g => g.Enrollment.Student.FullName)
            .ToListAsync(ct);
}

internal sealed class CertificateRepository(AppDbContext context) : Repository<Certificate>(context), ICertificateRepository
{
    private IQueryable<Certificate> Detailed() =>
        Set.AsNoTracking()
            .Include(c => c.Enrollment).ThenInclude(e => e.Student)
            .Include(c => c.Enrollment).ThenInclude(e => e.Section).ThenInclude(s => s.Course);

    public Task<Certificate?> GetWithDetailsAsync(int id, CancellationToken ct = default) =>
        Detailed().FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<int> GetNextSequenceAsync(int year, CancellationToken ct = default)
    {
        var prefix = $"CERT-{year}-";

        // Soft-deleted certificates still hold their number, so the filter is ignored here.
        var numbers = await Set.IgnoreQueryFilters()
            .Where(c => c.CertificateNumber.StartsWith(prefix))
            .Select(c => c.CertificateNumber)
            .ToListAsync(ct);

        var highest = 0;
        foreach (var number in numbers)
        {
            if (int.TryParse(number.AsSpan(prefix.Length), out var sequence) && sequence > highest)
                highest = sequence;
        }

        return highest + 1;
    }

    public Task<(IReadOnlyList<Certificate> Items, int TotalCount)> SearchAsync(
        int? studentId, int? sectionId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = Detailed();

        if (studentId.HasValue)
            query = query.Where(c => c.Enrollment.StudentId == studentId.Value);
        if (sectionId.HasValue)
            query = query.Where(c => c.Enrollment.SectionId == sectionId.Value);

        return query.OrderByDescending(c => c.IssuedAt).ThenByDescending(c => c.Id).ToPageAsync(page, pageSize, ct);
    }
}

internal sealed class CertificateTemplateRepository(AppDbContext context)
    : Repository<CertificateTemplate>(context), ICertificateTemplateRepository
{
    public async Task<IReadOnlyList<CertificateTemplate>> GetByLanguageAsync(string language, CancellationToken ct = default) =>
        await Set.Where(t => t.Language == language).ToListAsync(ct);

    public Task<CertificateTemplate?> GetDefaultAsync(string language, CancellationToken ct = default) =>
        Set.AsNoTracking().FirstOrDefaultAsync(t => t.Language == language && t.IsDefault, ct);
}
