using EducationalCenter.Application.Common.Exceptions;
using EducationalCenter.Application.Features.Branches;
using EducationalCenter.Application.Features.Expenses;
using EducationalCenter.Application.Features.Reports;
using EducationalCenter.Domain.Constants;
using EducationalCenter.Domain.Enums;
using EducationalCenter.Domain.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace EducationalCenter.Tests.Integration;

[Collection("integration")]
public sealed class BranchComparisonTests(AppFixture fx)
{
    private static DateOnly Today() => DateOnly.FromDateTime(DateTime.UtcNow);

    private void SuperAdmin(int? branchId = null)
    {
        fx.Branch.IsSuperAdmin = true;
        fx.Branch.BranchId = branchId;
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
        var code = ("T" + Guid.NewGuid().ToString("N")[..4]).ToUpperInvariant();
        return await scope.ServiceProvider.GetRequiredService<IBranchService>()
            .CreateAsync(new CreateBranchRequest("Compare " + code, code, null, null, null));
    }

    private async Task<BranchComparisonReportDto> CompareAsync(BranchComparisonQuery query)
    {
        await using var scope = fx.NewScope();
        return await scope.ServiceProvider.GetRequiredService<IBranchComparisonService>().GetAsync(query);
    }

    [Fact]
    public async Task Each_branch_gets_its_own_numbers_and_the_scope_does_not_hide_other_branches()
    {
        try
        {
            var a = await NewBranchAsync();
            var b = await NewBranchAsync();

            InBranch(a.Id);
            await using (var scope = fx.NewScope())
            {
                var seed = new Seeder(scope.ServiceProvider);
                var (course, trainer, room) = await seed.BaseAsync();
                var section = await seed.SectionAsync(course, trainer, room);
                await seed.EnrollAsync(section.Id);

                await scope.ServiceProvider.GetRequiredService<IExpenseService>().CreateAsync(
                    new CreateExpenseRequest("Test", "rent", Currency.Syp, 1000m, null, Today()));
            }

            var query = new BranchComparisonQuery(Today(), Today());

            foreach (var scopeBranch in new int?[] { null, a.Id, b.Id })
            {
                SuperAdmin(scopeBranch);
                var report = await CompareAsync(query);

                var rowA = report.Branches.Single(r => r.BranchId == a.Id);
                var rowB = report.Branches.Single(r => r.BranchId == b.Id);

                Assert.Equal(1000m, rowA.ExpensesSyp);
                Assert.Equal(-1000m, rowA.NetProfitSyp);
                Assert.Equal(1, rowA.StudentCount);
                Assert.Equal(1, rowA.NewEnrollments);
                Assert.Equal(1, rowA.OverdueInstallments + 1);
                Assert.NotNull(rowA.SectionFillRatePercent);
                Assert.True(rowA.SectionFillRatePercent > 0m);

                Assert.Equal(0m, rowB.ExpensesSyp);
                Assert.Equal(0, rowB.StudentCount);
                Assert.Equal(0, rowB.NewEnrollments);
                Assert.Null(rowB.SectionFillRatePercent);

                Assert.True(report.Totals.ExpensesSyp >= 1000m);
            }
        }
        finally { Reset(); }
    }

    [Fact]
    public async Task An_invalid_period_is_refused()
    {
        try
        {
            SuperAdmin();
            await Assert.ThrowsAsync<RequestValidationException>(() =>
                CompareAsync(new BranchComparisonQuery(Today(), Today().AddDays(-1))));
        }
        finally { Reset(); }
    }

    [Fact]
    public void Only_the_super_admin_role_holds_the_cross_branch_permission()
    {
        Assert.Contains(Permissions.Reports.CrossBranch, SystemRoles.AdminPermissions);
        Assert.DoesNotContain(Permissions.Reports.CrossBranch, SystemRoles.BranchManagerPermissions);
        Assert.DoesNotContain(Permissions.Reports.CrossBranch, SystemRoles.ReceptionistPermissions);
    }
}