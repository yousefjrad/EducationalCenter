using EducationalCenter.Domain.Constants;
using FluentValidation;

namespace EducationalCenter.Application.Features.Roles;

internal static class RoleRuleExtensions
{
    public static IRuleBuilderOptions<T, IReadOnlyList<string>> ValidPermissionNames<T>(
        this IRuleBuilder<T, IReadOnlyList<string>> rule) =>
        rule.NotEmpty()
            .Must(names => names is null || names.All(n => Permissions.All.Contains(n)))
            .WithMessage("One or more permission names do not exist.");
}

public sealed class CreateRoleRequestValidator : AbstractValidator<CreateRoleRequest>
{
    public CreateRoleRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Description).MaximumLength(200);
        RuleFor(x => x.PermissionNames).ValidPermissionNames();
    }
}

public sealed class UpdateRoleRequestValidator : AbstractValidator<UpdateRoleRequest>
{
    public UpdateRoleRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Description).MaximumLength(200);
        RuleFor(x => x.PermissionNames).ValidPermissionNames();
    }
}
