using EducationalCenter.Application.Common.Exceptions;
using EducationalCenter.Application.Features.Alerts;
using EducationalCenter.Application.Features.Branches;
using EducationalCenter.Application.Features.Budgets;
using EducationalCenter.Application.Features.Expenses;
using EducationalCenter.Domain.Enums;
using EducationalCenter.Domain.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace EducationalCenter.Tests.Integration;

[Collection("integration")]
public sealed class BudgetTests(AppFixture fx)
{
    private static string NewCode() => ("T" + Guid.NewGuid().ToString("N")[..4]).ToUpperInvariant();

    private static DateOnly ThisMonth()
    {
        var now = DateTime.UtcNow;
        return new DateOnly(now.Year, now.Month, 1);
    }

    private void SuperAdmin()
    {
        fx.Branch.IsSuperAdmin = true;
        fx.Branch.BranchId = null;
    }

    private void InBranch(int id)
    {
        fx.Branch.IsSuperAdmin = false;
        fx.Branch.BranchId = id;
    }

    private void Reset() => InBranch(1);

    private async Task<BranchDto> NewBranchAsync()
    {
        SuperAdmin();
        await using var scope = fx.NewScope();
        var code = NewCode();
        return await scope.ServiceProvider.GetRequiredService<IBranchService>()
            .CreateAsync(new CreateBranchRequest("Budget " + code, code, null, null, null));
    }

    private async Task<T> BudgetsAsync<T>(Func<IBudgetService, Task<T>> action)
    {
        await using var scope = fx.NewScope();
        return await action(scope.ServiceProvider.GetRequiredService<IBudgetService>());
    }

    [Fact]
    public async Task A_budget_goes_from_draft_to_submitted_to_approved_with_the_rules_enforced()
    {
        try
        {
            var branch = await NewBranchAsync();
            InBranch(branch.Id);

            var created = await BudgetsAsync(s => s.CreateAsync(new CreateBudgetRequest(ThisMonth(), 1_000_000m, "first")));
            Assert.Equal(BudgetStatus.Draft, created.Status);
            Assert.Equal(branch.Id, created.BranchId);

            await Assert.ThrowsAsync<ConflictException>(() =>
                BudgetsAsync(s => s.CreateAsync(new CreateBudgetRequest(ThisMonth(), 5m, null))));
            await Assert.ThrowsAsync<RequestValidationException>(() =>
                BudgetsAsync(s => s.CreateAsync(new CreateBudgetRequest(ThisMonth().AddMonths(-1), 5m, null))));

            var updated = await BudgetsAsync(s => s.UpdateAsync(created.Id, new UpdateBudgetRequest(1_200_000m, "more")));
            Assert.Equal(1_200_000m, updated.RequestedAmountSyp);

            var submitted = await BudgetsAsync(s => s.SubmitAsync(created.Id));
            Assert.Equal(BudgetStatus.Submitted, submitted.Status);

            await Assert.ThrowsAsync<ConflictException>(() =>
                BudgetsAsync(s => s.UpdateAsync(created.Id, new UpdateBudgetRequest(1m, null))));

            SuperAdmin();
            var approved = await BudgetsAsync(s => s.ApproveAsync(created.Id, new ApproveBudgetRequest(1_000_000m)));
            Assert.Equal(BudgetStatus.Approved, approved.Status);
            Assert.Equal(1_000_000m, approved.ApprovedAmountSyp);

            await Assert.ThrowsAsync<ConflictException>(() =>
                BudgetsAsync(s => s.RejectAsync(created.Id, new RejectBudgetRequest("too late"))));
        }
        finally { Reset(); }
    }

    [Fact]
    public async Task A_rejected_budget_shows_the_reason_and_can_be_edited_and_submitted_again()
    {
        try
        {
            var branch = await NewBranchAsync();
            InBranch(branch.Id);

            var created = await BudgetsAsync(s => s.CreateAsync(new CreateBudgetRequest(ThisMonth().AddMonths(1), 900_000m, null)));
            await BudgetsAsync(s => s.SubmitAsync(created.Id));

            SuperAdmin();
            var rejected = await BudgetsAsync(s => s.RejectAsync(created.Id, new RejectBudgetRequest("Too high")));
            Assert.Equal(BudgetStatus.Rejected, rejected.Status);
            Assert.Contains("Too high", rejected.Notes);

            InBranch(branch.Id);
            var edited = await BudgetsAsync(s => s.UpdateAsync(created.Id, new UpdateBudgetRequest(700_000m, "lower")));
            Assert.Equal(BudgetStatus.Draft, edited.Status);

            var again = await BudgetsAsync(s => s.SubmitAsync(created.Id));
            Assert.Equal(BudgetStatus.Submitted, again.Status);
        }
        finally { Reset(); }
    }

    [Fact]
    public async Task Spending_over_the_approved_budget_raises_an_alert_for_that_branch_only()
    {
        try
        {
            var branch = await NewBranchAsync();
            InBranch(branch.Id);

            var created = await BudgetsAsync(s => s.CreateAsync(new CreateBudgetRequest(ThisMonth(), 1_000_000m, null)));
            await BudgetsAsync(s => s.SubmitAsync(created.Id));
            SuperAdmin();
            await BudgetsAsync(s => s.ApproveAsync(created.Id, new ApproveBudgetRequest(null)));

            InBranch(branch.Id);
            await using (var scope = fx.NewScope())
            {
                await scope.ServiceProvider.GetRequiredService<IExpenseService>().CreateAsync(new CreateExpenseRequest(
                    "Test", "Over budget", Currency.Syp, 1_500_000m, null, DateOnly.FromDateTime(DateTime.UtcNow)));
            }

            var shown = await BudgetsAsync(s => s.GetByIdAsync(created.Id));
            Assert.Equal(1_500_000m, shown.SpentSyp);
            Assert.Equal(-500_000m, shown.RemainingSyp);

            // The branch manager of this branch sees it, and the Super Admin sees it with the branch name.
            await using (var scope = fx.NewScope())
            {
                var alerts = await scope.ServiceProvider.GetRequiredService<IAlertService>().GetAlertsAsync();
                Assert.Contains(alerts.BudgetOverruns!.Items, r => r.BranchId == branch.Id && r.OverBySyp == 500_000m);
            }

            SuperAdmin();
            await using (var scope = fx.NewScope())
            {
                var alerts = await scope.ServiceProvider.GetRequiredService<IAlertService>().GetAlertsAsync();
                Assert.Contains(alerts.BudgetOverruns!.Items, r => r.BranchId == branch.Id && r.BranchName == branch.Name);
            }

            // Another branch does not see this branch's overrun.
            InBranch(1);
            await using (var scope = fx.NewScope())
            {
                var alerts = await scope.ServiceProvider.GetRequiredService<IAlertService>().GetAlertsAsync();
                Assert.DoesNotContain(alerts.BudgetOverruns!.Items, r => r.BranchId == branch.Id);
            }
        }
        finally { Reset(); }
    }
}