using EducationalCenter.Application.Common.Validation;
using FluentValidation;

namespace EducationalCenter.Application.Features.Users;

public sealed class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Email).NotEmpty().MaximumLength(200).EmailAddress();
        RuleFor(x => x.Password).ValidPassword();
        RuleFor(x => x.RoleId).GreaterThan(0);
    }
}

public sealed class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Email).NotEmpty().MaximumLength(200).EmailAddress();
        RuleFor(x => x.RoleId).GreaterThan(0);
    }
}

public sealed class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.NewPassword).ValidPassword();
    }
}

public sealed class UserListQueryValidator : AbstractValidator<UserListQuery>
{
    public UserListQueryValidator()
    {
        RuleFor(x => x.Search).MaximumLength(100);
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
