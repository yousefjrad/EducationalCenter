using FluentValidation;

namespace EducationalCenter.Application.Features.Reports;

internal static class ReportRules
{
    public static readonly string[] Languages = ["ar", "en"];

    public const int MaxFinancialRangeDays = 3660;
    public const int MaxScheduleRangeDays = 366;

    public static bool RangeWithin(DateOnly from, DateOnly to, int maxDays) =>
        to >= from && to.DayNumber - from.DayNumber <= maxDays;
}

public sealed class MonthlyRevenueQueryValidator : AbstractValidator<MonthlyRevenueQuery>
{
    public MonthlyRevenueQueryValidator()
    {
        RuleFor(x => x.From).Must(d => d != default).WithMessage("'From' is required.");
        RuleFor(x => x.To)
            .Must((q, to) => ReportRules.RangeWithin(q.From, to, ReportRules.MaxFinancialRangeDays))
            .WithMessage("'To' must be on or after 'From' and within 10 years.");
    }
}

public sealed class NetProfitQueryValidator : AbstractValidator<NetProfitQuery>
{
    public NetProfitQueryValidator()
    {
        RuleFor(x => x.From).Must(d => d != default).WithMessage("'From' is required.");
        RuleFor(x => x.To)
            .Must((q, to) => ReportRules.RangeWithin(q.From, to, ReportRules.MaxFinancialRangeDays))
            .WithMessage("'To' must be on or after 'From' and within 10 years.");
    }
}

public sealed class RoomScheduleQueryValidator : AbstractValidator<RoomScheduleQuery>
{
    public RoomScheduleQueryValidator()
    {
        RuleFor(x => x.RoomId).GreaterThan(0);
        RuleFor(x => x.From).Must(d => d != default).WithMessage("'From' is required.");
        RuleFor(x => x.To)
            .Must((q, to) => ReportRules.RangeWithin(q.From, to, ReportRules.MaxScheduleRangeDays))
            .WithMessage("'To' must be on or after 'From' and within 366 days.");
    }
}

public sealed class TrainerScheduleQueryValidator : AbstractValidator<TrainerScheduleQuery>
{
    public TrainerScheduleQueryValidator()
    {
        RuleFor(x => x.TrainerId).GreaterThan(0);
        RuleFor(x => x.From).Must(d => d != default).WithMessage("'From' is required.");
        RuleFor(x => x.To)
            .Must((q, to) => ReportRules.RangeWithin(q.From, to, ReportRules.MaxScheduleRangeDays))
            .WithMessage("'To' must be on or after 'From' and within 366 days.");
    }
}

public sealed class FinancialReportExportRequestValidator : AbstractValidator<FinancialReportExportRequest>
{
    public FinancialReportExportRequestValidator()
    {
        RuleFor(x => x.Kind).IsInEnum();
        RuleFor(x => x.Format).IsInEnum();
        RuleFor(x => x.Language).Must(l => ReportRules.Languages.Contains(l))
            .WithMessage("'Language' must be 'ar' or 'en'.");

        When(x => x.Kind is FinancialReportKind.MonthlyRevenue or FinancialReportKind.NetProfit, () =>
        {
            RuleFor(x => x.From).NotNull().WithMessage("'From' is required for this report.");
            RuleFor(x => x.To).NotNull().WithMessage("'To' is required for this report.");
            RuleFor(x => x.To)
                .Must((q, to) => q.From is null || to is null
                                 || ReportRules.RangeWithin(q.From.Value, to.Value, ReportRules.MaxFinancialRangeDays))
                .WithMessage("'To' must be on or after 'From' and within 10 years.");
        });
    }
}

public sealed class OperationalReportExportRequestValidator : AbstractValidator<OperationalReportExportRequest>
{
    public OperationalReportExportRequestValidator()
    {
        RuleFor(x => x.Kind).IsInEnum();
        RuleFor(x => x.Format).IsInEnum();
        RuleFor(x => x.Language).Must(l => ReportRules.Languages.Contains(l))
            .WithMessage("'Language' must be 'ar' or 'en'.");

        When(x => x.Kind is OperationalReportKind.RoomSchedule or OperationalReportKind.TrainerSchedule, () =>
        {
            RuleFor(x => x.From).NotNull().WithMessage("'From' is required for this report.");
            RuleFor(x => x.To).NotNull().WithMessage("'To' is required for this report.");
            RuleFor(x => x.To)
                .Must((q, to) => q.From is null || to is null
                                 || ReportRules.RangeWithin(q.From.Value, to.Value, ReportRules.MaxScheduleRangeDays))
                .WithMessage("'To' must be on or after 'From' and within 366 days.");
        });

        When(x => x.Kind == OperationalReportKind.RoomSchedule, () =>
            RuleFor(x => x.RoomId).NotNull().WithMessage("'Room Id' is required for this report."));

        When(x => x.Kind == OperationalReportKind.TrainerSchedule, () =>
            RuleFor(x => x.TrainerId).NotNull().WithMessage("'Trainer Id' is required for this report."));
    }
}
