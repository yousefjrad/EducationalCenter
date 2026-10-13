using EducationalCenter.Application.Common.Interfaces;
using EducationalCenter.Application.Features.Branches;
using Microsoft.Extensions.DependencyInjection;

namespace EducationalCenter.Tests.Integration;

[Collection("integration")]
public sealed class BranchNumberingTests(AppFixture fx)
{
    [Fact]
    public async Task Receipt_numbers_carry_the_branch_code_and_each_branch_has_its_own_sequence()
    {
        try
        {
            fx.Branch.IsSuperAdmin = true;
            fx.Branch.BranchId = null;

            await using var scope = fx.NewScope();
            var code = ("T" + Guid.NewGuid().ToString("N")[..4]).ToUpperInvariant();
            var branch = await scope.ServiceProvider.GetRequiredService<IBranchService>()
                .CreateAsync(new CreateBranchRequest("Numbers " + code, code, null, null, null));

            var generator = scope.ServiceProvider.GetRequiredService<IReceiptNumberGenerator>();

            var first = await generator.NextAsync(branch.Id);
            Assert.Equal($"{code}-RCP-00001", first);

            var main = await generator.NextAsync(1);
            Assert.StartsWith("MN-RCP-", main);
        }
        finally
        {
            fx.Branch.IsSuperAdmin = false;
            fx.Branch.BranchId = 1;
        }
    }

    [Fact]
    public async Task Certificate_sequences_are_counted_per_prefix()
    {
        await using var scope = fx.NewScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var unused = $"ZZ{Guid.NewGuid().ToString("N")[..3].ToUpperInvariant()}-CERT-2099-";

        Assert.Equal(1, await uow.Certificates.GetNextSequenceAsync(unused));
    }
}