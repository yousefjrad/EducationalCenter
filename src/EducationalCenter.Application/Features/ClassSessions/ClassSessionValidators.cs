using FluentValidation;

namespace EducationalCenter.Application.Features.ClassSessions;

public sealed class CreateClassSessionRequestValidator : AbstractValidator<CreateClassSessionRequest>
{
    public CreateClassSessionRequestValidator()
    {
        RuleFor(x => x.SectionId).GreaterThan(0);
        RuleFor(x => x.Date).Must(d => d != default).WithMessage("'Date' is required.");
        RuleFor(x => x.EndTime).GreaterThan(x => x.StartTime)
            .WithMessage("'End Time' must be later than 'Start Time'.");
        RuleFor(x => x.RoomId).Must(v => v is null || v > 0).WithMessage("'Room Id' is invalid.");
        RuleFor(x => x.TrainerId).Must(v => v is null || v > 0).WithMessage("'Trainer Id' is invalid.");
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}

public sealed class UpdateClassSessionRequestValidator : AbstractValidator<UpdateClassSessionRequest>
{
    public UpdateClassSessionRequestValidator()
    {
        RuleFor(x => x.Date).Must(d => d != default).WithMessage("'Date' is required.");
        RuleFor(x => x.EndTime).GreaterThan(x => x.StartTime)
            .WithMessage("'End Time' must be later than 'Start Time'.");
        RuleFor(x => x.RoomId).GreaterThan(0);
        RuleFor(x => x.TrainerId).GreaterThan(0);
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}

public sealed class PostponeClassSessionRequestValidator : AbstractValidator<PostponeClassSessionRequest>
{
    public PostponeClassSessionRequestValidator()
    {
        RuleFor(x => x.NewDate).Must(d => d != default).WithMessage("'New Date' is required.");
        RuleFor(x => x.NewEndTime).GreaterThan(x => x.NewStartTime)
            .WithMessage("'New End Time' must be later than 'New Start Time'.");
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}

public sealed class CancelClassSessionRequestValidator : AbstractValidator<CancelClassSessionRequest>
{
    public CancelClassSessionRequestValidator()
    {
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}

public sealed class ClassSessionListQueryValidator : AbstractValidator<ClassSessionListQuery>
{
    public ClassSessionListQueryValidator()
    {
        RuleFor(x => x.To)
            .Must((q, to) => to is null || q.From is null || to >= q.From)
            .WithMessage("'To' must be on or after 'From'.");
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
    }
}
