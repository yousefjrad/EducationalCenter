using FluentValidation;

namespace EducationalCenter.Application.Features.Attendances;

public sealed class AttendanceMarkDtoValidator : AbstractValidator<AttendanceMarkDto>
{
    public AttendanceMarkDtoValidator()
    {
        RuleFor(x => x.EnrollmentId).GreaterThan(0);
    }
}

public sealed class RecordAttendanceRequestValidator : AbstractValidator<RecordAttendanceRequest>
{
    public RecordAttendanceRequestValidator()
    {
        RuleFor(x => x.Marks).NotEmpty();
        RuleFor(x => x.Marks)
            .Must(marks => marks is null || marks.Count <= 500)
            .WithMessage("A session cannot have more than 500 attendance marks.");
        RuleFor(x => x.Marks)
            .Must(marks => marks is null || marks.Select(m => m.EnrollmentId).Distinct().Count() == marks.Count)
            .WithMessage("Each enrollment can appear only once.");
        RuleForEach(x => x.Marks).SetValidator(new AttendanceMarkDtoValidator());
    }
}
