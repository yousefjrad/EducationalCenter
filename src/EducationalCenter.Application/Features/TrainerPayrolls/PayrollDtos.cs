using EducationalCenter.Domain.Enums;

namespace EducationalCenter.Application.Features.TrainerPayrolls;

public sealed record PayrollDto(
    int Id,
    int TrainerId,
    string TrainerName,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    string CalculationMethod,
    Currency Currency,
    decimal Amount,
    decimal? ExchangeRate,
    decimal AmountInSyp,
    PayrollStatus Status,
    DateTime? PaidAt,
    int? PaidByUserId);

/// <summary>What the system would pay for the period, with the figures behind it. Nothing is saved.</summary>
public sealed record PayrollCalculationDto(
    int TrainerId,
    string TrainerName,
    TrainerPayType PayType,
    decimal PayValue,
    Currency Currency,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    decimal Amount,
    decimal? ExchangeRate,
    decimal AmountInSyp,
    string CalculationMethod,
    int? MonthsCovered,
    decimal? HoursWorked,
    decimal? RevenueCollectedInSyp);

/// <summary>Used both to preview and to create a payroll; the amount is always calculated by the server.</summary>
/// <param name="ExchangeRate">SYP per 1 USD. Required for trainers paid in USD, must be empty otherwise.</param>
public sealed record PayrollRequest(int TrainerId, DateOnly PeriodStart, DateOnly PeriodEnd, decimal? ExchangeRate);

/// <param name="ExchangeRate">Optional new rate at payment time (USD payrolls only); the SYP amount is recalculated.</param>
public sealed record PayPayrollRequest(decimal? ExchangeRate);

public sealed record CancelPayrollRequest(string Reason);

public sealed record PayrollListQuery(
    int? TrainerId = null,
    PayrollStatus? Status = null,
    DateOnly? From = null,
    DateOnly? To = null,
    int Page = 1,
    int PageSize = 20);
