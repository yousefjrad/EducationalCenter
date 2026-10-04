using FluentValidation;

namespace EducationalCenter.Application.Features.AuditLogs;

public sealed class AuditLogListQueryValidator : AbstractValidator<AuditLogListQuery>
{
    public AuditLogListQueryValidator()
    {
        RuleFor(x => x.EntityName).MaximumLength(100);
        RuleFor(x => x.Action).MaximumLength(100);
        RuleFor(x => x.To)
            .Must((q, to) => to is null || q.From is null || to >= q.From)
            .WithMessage("'To' must be on or after 'From'.");
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
    }
}
