using EducationalCenter.Application.Common.Interfaces;
using EducationalCenter.Application.Common.Models;
using EducationalCenter.Application.Features.Enrollments;
using EducationalCenter.Domain.Entities;
using EducationalCenter.Domain.Enums;
using EducationalCenter.Domain.Exceptions;

namespace EducationalCenter.Application.Features.WaitingList;

public sealed class WaitingListService(IUnitOfWork uow, IClock clock, IEnrollmentService enrollments)
    : IWaitingListService
{
    public async Task<WaitingListEntryDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var entry = await uow.WaitingList.GetWithDetailsAsync(id, ct)
            ?? throw new NotFoundException(nameof(WaitingListEntry), id);
        return entry.ToDto();
    }

    public async Task<PagedResult<WaitingListEntryDto>> ListAsync(WaitingListQuery query, CancellationToken ct = default)
    {
        var (items, total) = await uow.WaitingList.SearchAsync(
            query.SectionId, query.StudentId, query.Status, query.Page, query.PageSize, ct);

        return new PagedResult<WaitingListEntryDto>(
            items.Select(e => e.ToDto()).ToList(), total, query.Page, query.PageSize);
    }

    public Task<WaitingListEntryDto> AddAsync(AddToWaitingListRequest request, CancellationToken ct = default)
    {
        return uow.ExecuteInTransactionAsync(async () =>
        {
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

            if (await uow.WaitingList.GetWaitingEntryAsync(student.Id, section.Id, ct) is not null)
                throw new ConflictException("The student is already on the waiting list for this section.");

            var seatsTaken = await uow.Enrollments.CountSeatsTakenAsync(section.Id, now, ct);
            if (seatsTaken < section.Capacity)
                throw new ConflictException("The section still has free seats. Enroll the student directly.");

            var queue = await uow.WaitingList.GetWaitingBySectionAsync(section.Id, ct);

            var entry = new WaitingListEntry
            {
                StudentId = student.Id,
                Student = student,
                SectionId = section.Id,
                Section = section,
                Position = queue.Count + 1,
                Status = WaitingListStatus.Waiting,
                AddedAt = now
            };

            await uow.WaitingList.AddAsync(entry, ct);
            await uow.SaveChangesAsync(ct);
            return entry.ToDto();
        }, ct);
    }

    public Task<WaitingListEntryDto> CancelAsync(int id, CancellationToken ct = default)
    {
        return uow.ExecuteInTransactionAsync(async () =>
        {
            var entry = await uow.WaitingList.GetWithDetailsAsync(id, ct)
                ?? throw new NotFoundException(nameof(WaitingListEntry), id);

            if (entry.Status != WaitingListStatus.Waiting)
                throw new ConflictException("Only waiting entries can be cancelled.");

            entry.Status = WaitingListStatus.Cancelled;
            uow.WaitingList.Update(entry);

            await WaitingListQueue.CompactAsync(uow, entry.SectionId, entry.Id, ct);
            await uow.SaveChangesAsync(ct);
            return entry.ToDto();
        }, ct);
    }

    public async Task<EnrollmentDto> PromoteAsync(int id, PromoteWaitingListEntryRequest request, CancellationToken ct = default)
    {
        var entry = await uow.WaitingList.GetWithDetailsAsync(id, ct)
            ?? throw new NotFoundException(nameof(WaitingListEntry), id);

        if (entry.Status != WaitingListStatus.Waiting)
            throw new ConflictException("Only waiting entries can be promoted.");

        // Creating the enrollment also marks this waiting entry as Promoted and renumbers the queue,
        // all in one transaction (see EnrollmentService.CreateAsync).
        return await enrollments.CreateAsync(
            new CreateEnrollmentRequest(entry.StudentId, entry.SectionId, request.AsHold), ct);
    }
}
