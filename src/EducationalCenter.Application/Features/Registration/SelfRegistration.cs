using EducationalCenter.Application.Common.Interfaces;
using EducationalCenter.Application.Common.Validation;
using EducationalCenter.Application.Features.Auth;
using EducationalCenter.Domain.Constants;
using EducationalCenter.Domain.Entities;
using EducationalCenter.Domain.Exceptions;
using FluentValidation;

namespace EducationalCenter.Application.Features.Registration;

public sealed record RegisterRequest(string FullName, string PhoneNumber, string Email, string Password);

public interface ISelfRegistrationService
{
    /// <summary>Creates a student and a "Student" sign-in account together, then signs the new student in.</summary>
    Task<AuthResultDto> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
}

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(30).Matches(@"^[0-9+\-\s()]{6,30}$");
        RuleFor(x => x.Email).NotEmpty().MaximumLength(200).EmailAddress();
        RuleFor(x => x.Password).ValidPassword();
    }
}

public sealed class SelfRegistrationService(IUnitOfWork uow, IPasswordHasher hasher, IAuthService auth) : ISelfRegistrationService
{
    // One message for every refusal, so the form cannot be used to find out who is already registered.
    private const string Refused =
        "We could not complete the registration. If you are already a student, please contact the center.";

    public async Task<AuthResultDto> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var name = request.FullName.Trim();
        var phone = request.PhoneNumber.Trim();
        var email = request.Email.Trim().ToLowerInvariant();

        if (await uow.Students.ExistsAsync(name, phone, null, ct) || await uow.Users.EmailExistsAsync(email, null, ct))
            throw new ConflictException(Refused);

        var role = (await uow.Roles.ListWithPermissionsAsync(ct)).FirstOrDefault(r => r.Name == SystemRoles.Student)
            ?? throw new NotFoundException(nameof(Role), SystemRoles.Student);

        await uow.ExecuteInTransactionAsync(async () =>
        {
            var user = new User
            {
                FullName = name,
                Email = email,
                PasswordHash = hasher.Hash(request.Password),
                RoleId = role.Id,
                IsActive = true
            };

            await uow.Users.AddAsync(user, ct);
            await uow.SaveChangesAsync(ct);

            await uow.Students.AddAsync(new Student
            {
                FullName = name,
                PhoneNumber = phone,
                IsActive = true,
                UserId = user.Id
            }, ct);
            await uow.SaveChangesAsync(ct);
        }, ct);

        return await auth.LoginAsync(new LoginRequest(email, request.Password), ct);
    }
}