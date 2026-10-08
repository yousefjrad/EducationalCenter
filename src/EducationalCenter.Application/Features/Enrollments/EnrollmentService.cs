using EducationalCenter.Application.Common.Interfaces;
using EducationalCenter.Application.Common.Models;
using EducationalCenter.Application.Features.WaitingList;
using EducationalCenter.Domain.Constants;
using EducationalCenter.Domain.Entities;
using EducationalCenter.Domain.Enums;
using EducationalCenter.Domain.Exceptions;

namespace EducationalCenter.Application.Features.Enrollments;

public sealed class EnrollmentService(IUnitOfWork uow, IClock clock, ISettingsProvider settings) : IEnrollmentService
{
    private const int DefaultHoldHours = 24;

    public async Task<EnrollmentDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var enrollment = await uow.Enrollments.GetWithDetailsAsync(id, ct)
            ?? throw new NotFoundException(nameof(Enrollment), id);
        return enrollment.ToDto();
    }

    public async Task<PagedResult<EnrollmentDto>> ListAsync(EnrollmentListQuery query, CancellationToken ct = default)
    {
        var (items, total) = await uow.Enrollments.SearchAsync(
            query.StudentId, query.SectionId, query.Status, query.Page, query.PageSize, ct);

        return new PagedResult<EnrollmentDto>(items.Select(e => e.ToDto()).ToList(), total, query.Page, query.PageSize);
    }

    public Task<EnrollmentDto> CreateAsync(CreateEnrollmentRequest request, CancellationToken ct = default)
    {
        return uow.ExecuteInTransactionAsync(async () =>
        {
            // Serializes concurrent enrollments of one section so the last seat cannot be sold twice.
            await uow.LockSectionAsync(request.SectionId, ct);

            var student = await uow.Students.GetByIdAsync(request.StudentId, ct)
                ?? throw new NotFoundException(nameof(Student), request.StudentId);
            if (!student.IsActive)
                throw new ConflictException("The student is inactive.");

            var section = await uow.Sections.GetWithDetailsAsync(request.SectionId, ct)
                ?? throw new NotFoundException(nameof(Section), request.SectionId);
            EnrollmentRules.EnsureSectionAccepts(section);

            var now = clock.UtcNow;

            if (await uow.Enrollments.HasActiveEnrollmentAsync(student.Id, section.Id, now, ct))
                throw new ConflictException("The student is already enrolled in this section.");

            var seatsTaken = await uow.Enrollments.CountSeatsTakenAsync(section.Id, now, ct);
            if (seatsTaken >= section.Capacity)
                throw new ConflictException(
                    $"The section is full ({section.Capacity} seats). Add the student to the waiting list.");

            DateTime? holdExpiresAt = null;
            if (request.AsHold)
            {
                var hours = await settings.GetIntAsync(SettingKeys.EnrollmentHoldHours, DefaultHoldHours, ct);
                holdExpiresAt = now.AddHours(hours);
            }

            var enrollment = new Enrollment
            {
                StudentId = student.Id,
                Student = student,
                SectionId = section.Id,
                Section = section,
                AgreedPrice = section.Price,
                Status = request.AsHold ? EnrollmentStatus.Pending : EnrollmentStatus.Confirmed,
                EnrolledAt = now,
                HoldExpiresAt = holdExpiresAt
            };

            await uow.Enrollments.AddAsync(enrollment, ct);
            await ResolveWaitingEntryAsync(student.Id, section.Id, enrollment, ct);
            await uow.SaveChangesAsync(ct);

            return enrollment.ToDto();
        }, ct);
    }

    public Task<EnrollmentDto> ConfirmAsync(int id, CancellationToken ct = default)
    {
        return uow.ExecuteInTransactionAsync(async () =>
        {
            var enrollment = await uow.Enrollments.GetWithDetailsAsync(id, ct)
                ?? throw new NotFoundException(nameof(Enrollment), id);

            if (enrollment.Status != EnrollmentStatus.Pending)
                throw new ConflictException("Only pending enrollments can be confirmed.");

            // Same lock as in CreateAsync: an expired hold is re-checked against the seats taken right now.
            await uow.LockSectionAsync(enrollment.SectionId, ct);

            EnrollmentRules.EnsureSectionAccepts(enrollment.Section);

            var now = clock.UtcNow;
            if (enrollment.HoldExpiresAt <= now)
            {
                // The expired hold no longer reserves a seat, so capacity must be checked again.
                var seatsTaken = await uow.Enrollments.CountSeatsTakenAsync(enrollment.SectionId, now, ct);
                if (seatsTaken >= enrollment.Section.Capacity)
                    throw new ConflictException("The hold expired and the section is now full.");
            }

            enrollment.Status = EnrollmentStatus.Confirmed;
            enrollment.HoldExpiresAt = null;

            uow.Enrollments.Update(enrollment);
            await uow.SaveChangesAsync(ct);
            return enrollment.ToDto();
        }, ct);
    }

    public Task<EnrollmentDto> CancelAsync(int id, CancellationToken ct = default)
    {
        return uow.ExecuteInTransactionAsync(async () =>
        {
            var enrollment = await uow.Enrollments.GetWithDetailsAsync(id, ct)
                ?? throw new NotFoundException(nameof(Enrollment), id);

            if (enrollment.Status is not (EnrollmentStatus.Pending or EnrollmentStatus.Confirmed))
                throw new ConflictException("Only pending or confirmed enrollments can be cancelled.");

            if (enrollment.Certificate is not null)
                throw new ConflictException("A certificate was issued for this enrollment, so it cannot be cancelled.");

            enrollment.Status = EnrollmentStatus.Cancelled;
            enrollment.HoldExpiresAt = null;

            // Payments already received are kept; only the plan stops expecting new installments.
            if (enrollment.PaymentPlan is { Status: PaymentPlanStatus.Open } plan)
                plan.Status = PaymentPlanStatus.Cancelled;

            uow.Enrollments.Update(enrollment);
            await uow.SaveChangesAsync(ct);
            return enrollment.ToDto();
        }, ct);
    }

    public Task<EnrollmentDto> TransferAsync(int id, TransferEnrollmentRequest request, CancellationToken ct = default)
    {
        return uow.ExecuteInTransactionAsync(async () =>
        {
            var source = await uow.Enrollments.GetWithDetailsAsync(id, ct)
                ?? throw new NotFoundException(nameof(Enrollment), id);

            if (source.Status != EnrollmentStatus.Confirmed)
                throw new ConflictException("Only confirmed enrollments can be transferred.");

            if (request.TargetSectionId == source.SectionId)
                throw new ConflictException("The target section is the same as the current section.");

            // The target section gains a student, so its seats are checked under the same lock.
            await uow.LockSectionAsync(request.TargetSectionId, ct);

            var target = await uow.Sections.GetWithDetailsAsync(request.TargetSectionId, ct)
                ?? throw new NotFoundException(nameof(Section), request.TargetSectionId);

            if (target.CourseId != source.Section.CourseId)
                throw new ConflictException("The target section must belong to the same course.");

            EnrollmentRules.EnsureSectionAccepts(target);

            var now = clock.UtcNow;

            if (await uow.Enrollments.HasActiveEnrollmentAsync(source.StudentId, target.Id, now, ct))
                throw new ConflictException("The student is already enrolled in the target section.");

            var seatsTaken = await uow.Enrollments.CountSeatsTakenAsync(target.Id, now, ct);
            if (seatsTaken >= target.Capacity)
                throw new ConflictException($"The target section is full ({target.Capacity} seats).");

            // The agreed price carries over, so the existing payment plan stays valid.
            var transferred = new Enrollment
            {
                StudentId = source.StudentId,
                Student = source.Student,
                SectionId = target.Id,
                Section = target,
                AgreedPrice = source.AgreedPrice,
                Status = EnrollmentStatus.Confirmed,
                EnrolledAt = now,
                TransferredFromEnrollmentId = source.Id,
                TransferredFromEnrollment = source
            };

            source.Status = EnrollmentStatus.Transferred;

            await uow.Enrollments.AddAsync(transferred, ct);
            uow.Enrollments.Update(source);

            // The payment plan (and its installments and payments) follows the student.
            if (source.PaymentPlan is not null)
                source.PaymentPlan.Enrollment = transferred;

            await ResolveWaitingEntryAsync(source.StudentId, target.Id, transferred, ct);
            await uow.SaveChangesAsync(ct);

            return transferred.ToDto();
        }, ct);
    }

    public async Task<int> ExpireHoldsAsync(CancellationToken ct = default)
    {
        var expired = await uow.Enrollments.GetExpiredHoldsAsync(clock.UtcNow, ct);
        if (expired.Count == 0)
            return 0;

        foreach (var enrollment in expired)
        {
            enrollment.Status = EnrollmentStatus.Cancelled;
            enrollment.HoldExpiresAt = null;
            uow.Enrollments.Update(enrollment);
        }

        await uow.SaveChangesAsync(ct);
        return expired.Count;
    }

    /// <summary>If the student was waiting for this section, that entry becomes Promoted.</summary>
    private async Task ResolveWaitingEntryAsync(int studentId, int sectionId, Enrollment enrollment, CancellationToken ct)
    {
        var entry = await uow.WaitingList.GetWaitingEntryAsync(studentId, sectionId, ct);
        if (entry is null)
            return;

        entry.Status = WaitingListStatus.Promoted;
        entry.PromotedEnrollment = enrollment;
        uow.WaitingList.Update(entry);

        await WaitingListQueue.CompactAsync(uow, sectionId, entry.Id, ct);
    }
}
