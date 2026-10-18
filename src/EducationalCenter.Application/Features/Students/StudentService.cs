using EducationalCenter.Application.Common.Interfaces;
using EducationalCenter.Application.Common.Models;
using EducationalCenter.Application.Common.Services;
using EducationalCenter.Domain.Entities;
using EducationalCenter.Domain.Exceptions;

namespace EducationalCenter.Application.Features.Students;

public sealed class StudentService(IUnitOfWork uow, IClock clock, IAuditLogger audit) : IStudentService
{
    public async Task<StudentDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var student = await uow.Students.GetByIdAsync(id, ct) ?? throw new NotFoundException(nameof(Student), id);
        return student.ToDto();
    }

    public async Task<PagedResult<StudentDto>> ListAsync(StudentListQuery query, CancellationToken ct = default)
    {
        var (items, total) = await uow.Students.SearchAsync(
            query.Search?.Trim(), query.IsActive, query.Page, query.PageSize, ct);
        return new PagedResult<StudentDto>(items.Select(s => s.ToDto()).ToList(), total, query.Page, query.PageSize);
    }

    public async Task<StudentDto> CreateAsync(CreateStudentRequest request, CancellationToken ct = default)
    {
        var name = request.FullName.Trim();
        var phone = request.PhoneNumber.Trim();

        if (await uow.Students.ExistsAsync(name, phone, null, ct))
            throw new ConflictException($"A student named '{name}' with this phone number already exists.");

        var student = new Student { FullName = name, PhoneNumber = phone, IsActive = true };

        await uow.Students.AddAsync(student, ct);
        await uow.SaveChangesAsync(ct);
        return student.ToDto();
    }

    public async Task<StudentDto> UpdateAsync(int id, UpdateStudentRequest request, CancellationToken ct = default)
    {
        var student = await uow.Students.GetByIdAsync(id, ct) ?? throw new NotFoundException(nameof(Student), id);

        var name = request.FullName.Trim();
        var phone = request.PhoneNumber.Trim();

        if (await uow.Students.ExistsAsync(name, phone, id, ct))
            throw new ConflictException($"A student named '{name}' with this phone number already exists.");

        student.FullName = name;
        student.PhoneNumber = phone;
        student.IsActive = request.IsActive;

        uow.Students.Update(student);
        await uow.SaveChangesAsync(ct);
        return student.ToDto();
    }

    public async Task<StudentTransferResultDto> TransferAsync(int id, TransferStudentRequest request, CancellationToken ct = default)
    {
        var student = await uow.Students.GetByIdAsync(id, ct) ?? throw new NotFoundException(nameof(Student), id);

        var fromBranchId = student.BranchId;
        if (request.TargetBranchId == fromBranchId)
            throw new ConflictException("The student already belongs to this branch.");

        var target = await uow.Branches.GetByIdAsync(request.TargetBranchId, ct)
            ?? throw new NotFoundException(nameof(Branch), request.TargetBranchId);
        if (!target.IsActive)
            throw new ConflictException("The target branch is closed.");

        var now = clock.UtcNow;
        if (await uow.Students.HasOpenWorkAsync(id, now, ct))
            throw new ConflictException("The student has a confirmed or held enrollment, or waits in a list. Cancel or complete it first.");

        await uow.ExecuteInTransactionAsync(async () =>
        {
            student.BranchId = target.Id;
            uow.Students.Update(student);

            if (student.UserId is int userId)
            {
                var user = await uow.Users.GetWithRoleAsync(userId, ct);
                if (user is not null)
                {
                    user.BranchId = target.Id;
                    await RefreshTokenRevoker.RevokeAllAsync(uow, user.Id, now, ct);
                }
            }

            await audit.LogAsync(
                "Student.Transferred", nameof(Student), student.Id,
                new { BranchId = fromBranchId }, new { BranchId = target.Id }, request.Reason, ct);

            await uow.SaveChangesAsync(ct);
        }, ct);

        return new StudentTransferResultDto(student.Id, student.FullName, fromBranchId, target.Id);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var student = await uow.Students.GetByIdAsync(id, ct) ?? throw new NotFoundException(nameof(Student), id);

        if (await uow.Students.HasEnrollmentsAsync(id, ct))
            throw new ConflictException("This student has enrollments and cannot be deleted. Deactivate them instead.");

        await audit.LogAsync("Student.Delete", nameof(Student), student.Id, new { student.FullName, student.PhoneNumber, student.BranchId }, null, null, ct);

        uow.Students.Remove(student);
        await uow.SaveChangesAsync(ct);
    }
}
