using EducationalCenter.Application.Common.Interfaces;
using EducationalCenter.Application.Common.Validation;
using EducationalCenter.Application.Features.Users;
using EducationalCenter.Domain.Constants;
using EducationalCenter.Domain.Entities;
using EducationalCenter.Domain.Exceptions;
using FluentValidation;

namespace EducationalCenter.Application.Features.Students;

public sealed record CreateStudentAccountRequest(string Email, string Password);

public interface IStudentAccountService
{
    /// <summary>Creates the student's sign-in account (role "Student") and links it. One account per student.</summary>
    Task<UserDto> CreateAsync(int studentId, CreateStudentAccountRequest request, CancellationToken ct = default);
}

public sealed class CreateStudentAccountRequestValidator : AbstractValidator<CreateStudentAccountRequest>
{
    public CreateStudentAccountRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().MaximumLength(200).EmailAddress();
        RuleFor(x => x.Password).ValidPassword();
    }
}

public sealed class StudentAccountService(IUnitOfWork uow, IPasswordHasher hasher) : IStudentAccountService
{
    public async Task<UserDto> CreateAsync(int studentId, CreateStudentAccountRequest request, CancellationToken ct = default)
    {
        var student = await uow.Students.GetByIdAsync(studentId, ct)
            ?? throw new NotFoundException(nameof(Student), studentId);

        if (!student.IsActive)
            throw new ConflictException("This student is inactive.");

        if (student.UserId is not null)
            throw new ConflictException("This student already has an account.");

        var email = request.Email.Trim().ToLowerInvariant();
        if (await uow.Users.EmailExistsAsync(email, null, ct))
            throw new ConflictException($"The email '{email}' is already in use.");

        var role = (await uow.Roles.ListWithPermissionsAsync(ct)).FirstOrDefault(r => r.Name == SystemRoles.Student)
            ?? throw new NotFoundException(nameof(Role), SystemRoles.Student);

        return await uow.ExecuteInTransactionAsync(async () =>
        {
            var user = new User
            {
                FullName = student.FullName,
                Email = email,
                PasswordHash = hasher.Hash(request.Password),
                RoleId = role.Id,
                IsActive = true
            };

            await uow.Users.AddAsync(user, ct);
            await uow.SaveChangesAsync(ct);

            student.UserId = user.Id;
            uow.Students.Update(student);
            await uow.SaveChangesAsync(ct);

            return new UserDto(user.Id, user.FullName, user.Email, role.Id, role.Name, user.IsActive, user.LastLoginAt, user.LockedUntil);
        }, ct);
    }
}