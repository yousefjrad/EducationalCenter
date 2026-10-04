using System.Globalization;
using EducationalCenter.Application.Common.Exceptions;
using EducationalCenter.Application.Common.Interfaces;
using EducationalCenter.Application.Common.Models;
using EducationalCenter.Application.Common.Money;
using EducationalCenter.Domain.Entities;
using EducationalCenter.Domain.Enums;
using EducationalCenter.Domain.Exceptions;

namespace EducationalCenter.Application.Features.TrainerPayrolls;

public sealed class TrainerPayrollService(
    IUnitOfWork uow,
    IClock clock,
    ICurrentUser currentUser,
    IAuditLogger audit) : ITrainerPayrollService
{
    public async Task<PayrollDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var payroll = await uow.TrainerPayrolls.GetWithDetailsAsync(id, ct)
            ?? throw new NotFoundException(nameof(TrainerPayroll), id);
        return payroll.ToDto();
    }

    public async Task<PagedResult<PayrollDto>> ListAsync(PayrollListQuery query, CancellationToken ct = default)
    {
        var (items, total) = await uow.TrainerPayrolls.SearchAsync(
            query.TrainerId, query.Status, query.From, query.To, query.Page, query.PageSize, ct);

        return new PagedResult<PayrollDto>(items.Select(p => p.ToDto()).ToList(), total, query.Page, query.PageSize);
    }

    public async Task<PayrollCalculationDto> CalculateAsync(PayrollRequest request, CancellationToken ct = default)
    {
        var trainer = await uow.Trainers.GetByIdAsync(request.TrainerId, ct)
            ?? throw new NotFoundException(nameof(Trainer), request.TrainerId);

        EnsurePeriodNotInFuture(request.PeriodEnd);

        var calc = await CalculateCoreAsync(trainer, request, ct);

        return new PayrollCalculationDto(
            trainer.Id, trainer.FullName, trainer.PayType, trainer.PayValue, trainer.PayCurrency,
            request.PeriodStart, request.PeriodEnd,
            calc.Amount, calc.ExchangeRate, calc.AmountInSyp, calc.Method,
            calc.Months, calc.Hours, calc.RevenueInSyp);
    }

    public Task<PayrollDto> CreateAsync(PayrollRequest request, CancellationToken ct = default)
    {
        return uow.ExecuteInTransactionAsync(async () =>
        {
            var trainer = await uow.Trainers.GetByIdAsync(request.TrainerId, ct)
                ?? throw new NotFoundException(nameof(Trainer), request.TrainerId);

            EnsurePeriodNotInFuture(request.PeriodEnd);

            if (await uow.TrainerPayrolls.HasOverlapAsync(trainer.Id, request.PeriodStart, request.PeriodEnd, ct))
                throw new ConflictException("This trainer already has a payroll overlapping that period.");

            var calc = await CalculateCoreAsync(trainer, request, ct);

            var payroll = new TrainerPayroll
            {
                TrainerId = trainer.Id,
                Trainer = trainer,
                PeriodStart = request.PeriodStart,
                PeriodEnd = request.PeriodEnd,
                CalculationMethod = calc.Method,
                Currency = trainer.PayCurrency,
                Amount = calc.Amount,
                ExchangeRate = calc.ExchangeRate,
                AmountInSyp = calc.AmountInSyp,
                Status = PayrollStatus.Due
            };

            await uow.TrainerPayrolls.AddAsync(payroll, ct);
            await uow.SaveChangesAsync(ct);

            await audit.LogAsync(
                "TrainerPayroll.Created", nameof(TrainerPayroll), payroll.Id,
                oldValues: null,
                newValues: new
                {
                    payroll.TrainerId,
                    payroll.PeriodStart,
                    payroll.PeriodEnd,
                    payroll.CalculationMethod,
                    payroll.Currency,
                    payroll.Amount,
                    payroll.ExchangeRate,
                    payroll.AmountInSyp
                },
                reason: null, ct);
            await uow.SaveChangesAsync(ct);

            return payroll.ToDto();
        }, ct);
    }

    public Task<PayrollDto> PayAsync(int id, PayPayrollRequest request, CancellationToken ct = default)
    {
        return uow.ExecuteInTransactionAsync(async () =>
        {
            var userId = currentUser.RequireUserId();

            var payroll = await uow.TrainerPayrolls.GetWithDetailsAsync(id, ct)
                ?? throw new NotFoundException(nameof(TrainerPayroll), id);

            if (payroll.Status != PayrollStatus.Due)
                throw new ConflictException("Only due payrolls can be paid.");

            var before = new { payroll.Status, payroll.ExchangeRate, payroll.AmountInSyp };

            if (request.ExchangeRate is not null)
            {
                if (payroll.Currency != Currency.Usd)
                    throw new RequestValidationException("exchangeRate", "'exchangeRate' applies only to payrolls in USD.");

                payroll.ExchangeRate = request.ExchangeRate;
                payroll.AmountInSyp = CurrencyConversion.ToSyp(payroll.Currency, payroll.Amount, request.ExchangeRate);
            }

            payroll.Status = PayrollStatus.Paid;
            payroll.PaidAt = clock.UtcNow;
            payroll.PaidByUserId = userId;

            await uow.SaveChangesAsync(ct);

            await audit.LogAsync(
                "TrainerPayroll.Paid", nameof(TrainerPayroll), payroll.Id,
                oldValues: before,
                newValues: new { payroll.Status, payroll.ExchangeRate, payroll.AmountInSyp, payroll.PaidAt },
                reason: null, ct);
            await uow.SaveChangesAsync(ct);

            return payroll.ToDto();
        }, ct);
    }

    public Task<PayrollDto> CancelAsync(int id, CancelPayrollRequest request, CancellationToken ct = default)
    {
        return uow.ExecuteInTransactionAsync(async () =>
        {
            var payroll = await uow.TrainerPayrolls.GetWithDetailsAsync(id, ct)
                ?? throw new NotFoundException(nameof(TrainerPayroll), id);

            if (payroll.Status != PayrollStatus.Due)
                throw new ConflictException("Only due payrolls can be cancelled.");

            payroll.Status = PayrollStatus.Cancelled;
            await uow.SaveChangesAsync(ct);

            await audit.LogAsync(
                "TrainerPayroll.Cancelled", nameof(TrainerPayroll), payroll.Id,
                oldValues: new { Status = PayrollStatus.Due },
                newValues: new { Status = PayrollStatus.Cancelled },
                reason: request.Reason, ct);
            await uow.SaveChangesAsync(ct);

            return payroll.ToDto();
        }, ct);
    }

    // ---------- calculation ----------

    private sealed record Calculation(
        decimal Amount, decimal? ExchangeRate, decimal AmountInSyp, string Method,
        int? Months, decimal? Hours, decimal? RevenueInSyp);

    private async Task<Calculation> CalculateCoreAsync(Trainer trainer, PayrollRequest request, CancellationToken ct)
    {
        var currency = trainer.PayCurrency;

        if (currency == Currency.Usd && request.ExchangeRate is null)
            throw new RequestValidationException("exchangeRate", "'exchangeRate' is required for trainers paid in USD.");
        if (currency == Currency.Syp && request.ExchangeRate is not null)
            throw new RequestValidationException("exchangeRate", "'exchangeRate' must be empty for trainers paid in SYP.");

        var rate = request.ExchangeRate;
        var code = currency == Currency.Usd ? "USD" : "SYP";

        decimal amount;
        string method;
        int? months = null;
        decimal? hours = null;
        decimal? revenue = null;

        switch (trainer.PayType)
        {
            case TrainerPayType.Monthly:
            {
                var monthsCovered = (request.PeriodEnd.Year * 12 + request.PeriodEnd.Month)
                                    - (request.PeriodStart.Year * 12 + request.PeriodStart.Month) + 1;
                months = monthsCovered;
                amount = trainer.PayValue * monthsCovered;
                method = $"Monthly: {monthsCovered} month(s) x {Fmt(trainer.PayValue)} {code}";
                break;
            }
            case TrainerPayType.Hourly:
            {
                var sessions = await uow.ClassSessions.GetHeldByTrainerAsync(
                    trainer.Id, request.PeriodStart, request.PeriodEnd, ct);
                var minutes = sessions.Sum(s => (s.EndTime - s.StartTime).TotalMinutes);
                var hoursWorked = Math.Round((decimal)minutes / 60m, 2);
                hours = hoursWorked;
                amount = hoursWorked * trainer.PayValue;
                method = $"Hourly: {Fmt(hoursWorked)} h x {Fmt(trainer.PayValue)} {code}/h";
                break;
            }
            case TrainerPayType.Percentage:
            {
                var collected = await uow.Payments.SumValidInSypByTrainerAsync(
                    trainer.Id, request.PeriodStart, request.PeriodEnd, ct);
                revenue = collected;
                var shareInSyp = collected * trainer.PayValue / 100m;
                amount = currency == Currency.Usd ? shareInSyp / rate!.Value : shareInSyp;
                method = $"Percentage: {Fmt(trainer.PayValue)}% of {Fmt(collected)} SYP collected";
                if (currency == Currency.Usd)
                    method += $" at rate {Fmt(rate!.Value)}";
                break;
            }
            default:
                throw new ConflictException("Unsupported pay type.");
        }

        amount = Math.Round(amount, currency == Currency.Usd ? 2 : 0, MidpointRounding.AwayFromZero);
        if (amount <= 0m)
            throw new ConflictException("There is nothing to pay this trainer for that period.");

        return new Calculation(
            amount, rate, CurrencyConversion.ToSyp(currency, amount, rate), method, months, hours, revenue);
    }

    /// <summary>One day of tolerance so local time zones ahead of UTC are not blocked.</summary>
    private void EnsurePeriodNotInFuture(DateOnly periodEnd)
    {
        var latestAllowed = DateOnly.FromDateTime(clock.UtcNow).AddDays(1);
        if (periodEnd > latestAllowed)
            throw new RequestValidationException("periodEnd", "'periodEnd' cannot be in the future.");
    }

    private static string Fmt(decimal value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}
