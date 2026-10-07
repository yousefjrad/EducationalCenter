using EducationalCenter.Application.Features.PaymentPlans;
using EducationalCenter.Domain.Entities;
using EducationalCenter.Domain.Enums;

namespace EducationalCenter.Tests;

public class PaymentMathTests
{
    private static readonly DateOnly Today = new(2026, 10, 7);

    private static Payment Pay(decimal syp, PaymentStatus status = PaymentStatus.Valid) =>
        new() { AmountInSyp = syp, Status = status };

    private static Installment MakeInstallment(InstallmentStatus status, DateOnly due, params Payment[] payments)
    {
        var installment = new Installment { Amount = 100m, DueDate = due, Status = status };
        foreach (var payment in payments)
            installment.Payments.Add(payment);
        return installment;
    }

    [Fact]
    public void PaidSyp_counts_only_valid_payments()
    {
        var installment = MakeInstallment(
            InstallmentStatus.PartiallyPaid, Today, Pay(100m), Pay(50m, PaymentStatus.Cancelled), Pay(25m));

        Assert.Equal(125m, PaymentMath.PaidSyp(installment));
    }

    [Fact]
    public void PaidSyp_is_zero_without_payments() =>
        Assert.Equal(0m, PaymentMath.PaidSyp(MakeInstallment(InstallmentStatus.Unpaid, Today)));

    [Theory]
    [InlineData(0, 100, InstallmentStatus.Unpaid)]
    [InlineData(1, 100, InstallmentStatus.PartiallyPaid)]
    [InlineData(99, 100, InstallmentStatus.PartiallyPaid)]
    [InlineData(100, 100, InstallmentStatus.Paid)]
    [InlineData(150, 100, InstallmentStatus.Paid)]
    public void StoredStatus_follows_the_paid_amount(int paid, int amount, InstallmentStatus expected) =>
        Assert.Equal(expected, PaymentMath.StoredStatus(paid, amount));

    [Theory]
    [InlineData(InstallmentStatus.Unpaid)]
    [InlineData(InstallmentStatus.PartiallyPaid)]
    public void DisplayStatus_is_overdue_when_open_unpaid_and_past_due(InstallmentStatus stored)
    {
        var installment = MakeInstallment(stored, Today.AddDays(-1));

        Assert.Equal(
            InstallmentStatus.Overdue,
            PaymentMath.DisplayStatus(installment, PaymentPlanStatus.Open, Today));
    }

    [Fact]
    public void DisplayStatus_is_not_overdue_on_the_due_date()
    {
        var installment = MakeInstallment(InstallmentStatus.Unpaid, Today);

        Assert.Equal(
            InstallmentStatus.Unpaid,
            PaymentMath.DisplayStatus(installment, PaymentPlanStatus.Open, Today));
    }

    [Fact]
    public void DisplayStatus_keeps_paid_even_when_past_due()
    {
        var installment = MakeInstallment(InstallmentStatus.Paid, Today.AddDays(-30));

        Assert.Equal(
            InstallmentStatus.Paid,
            PaymentMath.DisplayStatus(installment, PaymentPlanStatus.Open, Today));
    }

    [Theory]
    [InlineData(PaymentPlanStatus.FullyPaid)]
    [InlineData(PaymentPlanStatus.Cancelled)]
    public void DisplayStatus_is_never_overdue_for_a_closed_plan(PaymentPlanStatus plan)
    {
        var installment = MakeInstallment(InstallmentStatus.Unpaid, Today.AddDays(-30));

        Assert.Equal(InstallmentStatus.Unpaid, PaymentMath.DisplayStatus(installment, plan, Today));
    }

    [Fact]
    public void DisplayStatus_keeps_the_stored_status_before_the_due_date()
    {
        var installment = MakeInstallment(InstallmentStatus.PartiallyPaid, Today.AddDays(5));

        Assert.Equal(
            InstallmentStatus.PartiallyPaid,
            PaymentMath.DisplayStatus(installment, PaymentPlanStatus.Open, Today));
    }
}