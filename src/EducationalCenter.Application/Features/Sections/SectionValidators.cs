using FluentValidation;

namespace EducationalCenter.Application.Features.Sections;

internal static class SectionRules
{
    /// <summary>No two slots on the same weekday may overlap.</summary>
    public static bool NoOverlappingSlots(IReadOnlyList<SectionScheduleDto>? schedules)
    {
        if (schedules is null) return true;

        foreach (var day in schedules.GroupBy(s => s.DayOfWeek))
        {
            var slots = day.ToList();
            for (var i = 0; i < slots.Count; i++)
            for (var j = i + 1; j < slots.Count; j++)
            {
                if (slots[i].StartTime < slots[j].EndTime && slots[i].EndTime > slots[j].StartTime)
                    return false;
            }
        }
        return true;
    }
}

public sealed class SectionScheduleDtoValidator : AbstractValidator<SectionScheduleDto>
{
    public SectionScheduleDtoValidator()
    {
        RuleFor(x => x.DayOfWeek).IsInEnum();
        RuleFor(x => x.EndTime).GreaterThan(x => x.StartTime)
            .WithMessage("'End Time' must be later than 'Start Time'.");
    }
}

public sealed class CreateSectionRequestValidator : AbstractValidator<CreateSectionRequest>
{
    public CreateSectionRequestValidator()
    {
        RuleFor(x => x.CourseId).GreaterThan(0);
        RuleFor(x => x.TrainerId).GreaterThan(0);
        RuleFor(x => x.RoomId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);

        RuleFor(x => x.StartDate).Must(d => d != default).WithMessage("'Start Date' is required.");
        RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate);
        RuleFor(x => x.EndDate)
            .Must((x, end) => end.DayNumber - x.StartDate.DayNumber <= 366)
            .WithMessage("The section cannot span more than 366 days.");

        RuleFor(x => x.Price).Must(p => p is null || p >= 0m).WithMessage("'Price' must not be negative.");
        RuleFor(x => x.Capacity).InclusiveBetween(1, 1000);
        RuleFor(x => x.MinStudents).InclusiveBetween(0, 1000);
        RuleFor(x => x.MinStudents).LessThanOrEqualTo(x => x.Capacity);

        RuleFor(x => x.Schedules).NotEmpty();
        RuleFor(x => x.Schedules).Must(SectionRules.NoOverlappingSlots)
            .WithMessage("Weekly schedule slots on the same day must not overlap.");
        RuleForEach(x => x.Schedules).SetValidator(new SectionScheduleDtoValidator());
    }
}

public sealed class UpdateSectionRequestValidator : AbstractValidator<UpdateSectionRequest>
{
    public UpdateSectionRequestValidator()
    {
        RuleFor(x => x.TrainerId).GreaterThan(0);
        RuleFor(x => x.RoomId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);

        RuleFor(x => x.StartDate).Must(d => d != default).WithMessage("'Start Date' is required.");
        RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate);
        RuleFor(x => x.EndDate)
            .Must((x, end) => end.DayNumber - x.StartDate.DayNumber <= 366)
            .WithMessage("The section cannot span more than 366 days.");

        RuleFor(x => x.Price).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.Capacity).InclusiveBetween(1, 1000);
        RuleFor(x => x.MinStudents).InclusiveBetween(0, 1000);
        RuleFor(x => x.MinStudents).LessThanOrEqualTo(x => x.Capacity);

        RuleFor(x => x.Schedules).NotEmpty();
        RuleFor(x => x.Schedules).Must(SectionRules.NoOverlappingSlots)
            .WithMessage("Weekly schedule slots on the same day must not overlap.");
        RuleForEach(x => x.Schedules).SetValidator(new SectionScheduleDtoValidator());
    }
}

public sealed class ChangeSectionStatusRequestValidator : AbstractValidator<ChangeSectionStatusRequest>
{
    public ChangeSectionStatusRequestValidator()
    {
        RuleFor(x => x.Status).IsInEnum();
    }
}

public sealed class SectionListQueryValidator : AbstractValidator<SectionListQuery>
{
    public SectionListQueryValidator()
    {
        RuleFor(x => x.Search).MaximumLength(100);
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
