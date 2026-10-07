using EducationalCenter.Application.Features.Enrollments;
using EducationalCenter.Application.Features.PaymentPlans;
using EducationalCenter.Application.Features.Payments;
using EducationalCenter.Domain.Enums;
using EducationalCenter.Domain.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace EducationalCenter.Tests.Integration;

[Collection("integration")]
public sealed class PaymentTests(AppFixture fx)
{
    private static readonly DateOnly FirstDue = new(2027, 1, 15);

    private static async Task<EnrollmentDto> ConfirmedEnrollmentAsync(IServiceProvider sp)
    {
        var seed = new Seeder(sp);
        var (course, trainer, room) = await seed.BaseAsync();
        var section = await seed.SectionAsync(course, trainer, room);
        var (_, enrollment) = await seed.EnrollAsync(section.Id);
        return enrollment;
    }

    private static Task<PaymentPlanDto> PlanAsync(IServiceProvider sp, int enrollmentId, int installments) =>
        sp.GetRequiredService<IPaymentPlanService>()
            .CreateAsync(new CreatePaymentPlanRequest(enrollmentId, installments, FirstDue));

    private static Task<PaymentDto> PaySypAsync(IServiceProvider sp, int installmentId, decimal amount) =>
        sp.GetRequiredService<IPaymentService>()
            .RecordAsync(new RecordPaymentRequest(installmentId, Currency.Syp, amount, null));

    [Fact]
    public async Task The_remainder_of_an_uneven_split_goes_on_the_first_installment()
    {
        await using var scope = fx.NewScope();
        var enrollment = await ConfirmedEnrollmentAsync(scope.ServiceProvider);

        var plan = await PlanAsync(scope.ServiceProvider, enrollment.Id, 3);

        Assert.Equal(500_000m, plan.TotalAmount);
        Assert.Equal(new[] { 166_668m, 166_666m, 166_666m }, plan.Installments.Select(i => i.Amount).ToArray());
    }

    [Fact]
    public async Task Installments_fall_on_the_same_day_of_each_following_month()
    {
        await using var scope = fx.NewScope();
        var enrollment = await ConfirmedEnrollmentAsync(scope.ServiceProvider);

        var plan = await PlanAsync(scope.ServiceProvider, enrollment.Id, 3);

        Assert.Equal(
            new[] { new DateOnly(2027, 1, 15), new DateOnly(2027, 2, 15), new DateOnly(2027, 3, 15) },
            plan.Installments.Select(i => i.DueDate).ToArray());
    }

    [Fact]
    public async Task An_enrollment_gets_only_one_payment_plan()
    {
        await using var scope = fx.NewScope();
        var enrollment = await ConfirmedEnrollmentAsync(scope.ServiceProvider);
        await PlanAsync(scope.ServiceProvider, enrollment.Id, 2);

        await Assert.ThrowsAsync<ConflictException>(() => PlanAsync(scope.ServiceProvider, enrollment.Id, 2));
    }

    [Fact]
    public async Task A_usd_payment_is_converted_with_the_given_rate()
    {
        await using var scope = fx.NewScope();
        var enrollment = await ConfirmedEnrollmentAsync(scope.ServiceProvider);
        var plan = await PlanAsync(scope.ServiceProvider, enrollment.Id, 3);

        var payment = await scope.ServiceProvider.GetRequiredService<IPaymentService>()
            .RecordAsync(new RecordPaymentRequest(plan.Installments[0].Id, Currency.Usd, 5m, 12_000m));

        Assert.Equal(60_000m, payment.AmountInSyp);
        Assert.Equal(12_000m, payment.ExchangeRate);
    }

    [Fact]
    public async Task A_payment_above_the_remaining_amount_is_rejected()
    {
        await using var scope = fx.NewScope();
        var enrollment = await ConfirmedEnrollmentAsync(scope.ServiceProvider);
        var plan = await PlanAsync(scope.ServiceProvider, enrollment.Id, 3);

        await Assert.ThrowsAsync<ConflictException>(
            () => PaySypAsync(scope.ServiceProvider, plan.Installments[0].Id, 9_999_999m));
    }

    [Fact]
    public async Task A_partial_payment_marks_the_installment_partially_paid()
    {
        await using var scope = fx.NewScope();
        var enrollment = await ConfirmedEnrollmentAsync(scope.ServiceProvider);
        var plan = await PlanAsync(scope.ServiceProvider, enrollment.Id, 3);
        await PaySypAsync(scope.ServiceProvider, plan.Installments[0].Id, 50_000m);

        var after = await scope.ServiceProvider.GetRequiredService<IPaymentPlanService>().GetByEnrollmentAsync(enrollment.Id);

        Assert.Equal(50_000m, after.PaidAmountInSyp);
        Assert.Equal(450_000m, after.RemainingInSyp);
        Assert.Equal(InstallmentStatus.PartiallyPaid, after.Installments[0].Status);
    }

    [Fact]
    public async Task Receipt_numbers_are_sequential()
    {
        await using var scope = fx.NewScope();
        var enrollment = await ConfirmedEnrollmentAsync(scope.ServiceProvider);
        var plan = await PlanAsync(scope.ServiceProvider, enrollment.Id, 3);

        var first = await PaySypAsync(scope.ServiceProvider, plan.Installments[0].Id, 1_000m);
        var second = await PaySypAsync(scope.ServiceProvider, plan.Installments[0].Id, 1_000m);

        Assert.StartsWith("RCP-", first.ReceiptNumber);
        Assert.Equal(int.Parse(first.ReceiptNumber![4..]) + 1, int.Parse(second.ReceiptNumber![4..]));
    }

    [Fact]
    public async Task Cancelling_a_payment_restores_the_balance_and_cannot_be_repeated()
    {
        await using var scope = fx.NewScope();
        var enrollment = await ConfirmedEnrollmentAsync(scope.ServiceProvider);
        var plan = await PlanAsync(scope.ServiceProvider, enrollment.Id, 3);
        var payment = await PaySypAsync(scope.ServiceProvider, plan.Installments[0].Id, 50_000m);
        var payments = scope.ServiceProvider.GetRequiredService<IPaymentService>();

        var cancelled = await payments.CancelAsync(payment.Id, new CancelPaymentRequest("test"));
        var after = await scope.ServiceProvider.GetRequiredService<IPaymentPlanService>().GetByEnrollmentAsync(enrollment.Id);

        Assert.Equal(PaymentStatus.Cancelled, cancelled.Status);
        Assert.Equal(0m, after.PaidAmountInSyp);
        await Assert.ThrowsAsync<ConflictException>(
            () => payments.CancelAsync(payment.Id, new CancelPaymentRequest("again")));
    }

    [Fact]
    public async Task Paying_every_installment_closes_the_plan()
    {
        await using var scope = fx.NewScope();
        var enrollment = await ConfirmedEnrollmentAsync(scope.ServiceProvider);
        var plan = await PlanAsync(scope.ServiceProvider, enrollment.Id, 3);

        foreach (var installment in plan.Installments)
            await PaySypAsync(scope.ServiceProvider, installment.Id, installment.Amount);

        var after = await scope.ServiceProvider.GetRequiredService<IPaymentPlanService>().GetByEnrollmentAsync(enrollment.Id);

        Assert.Equal(PaymentPlanStatus.FullyPaid, after.Status);
        Assert.Equal(0m, after.RemainingInSyp);
        await Assert.ThrowsAsync<ConflictException>(
            () => PaySypAsync(scope.ServiceProvider, plan.Installments[0].Id, 1m));
    }
}