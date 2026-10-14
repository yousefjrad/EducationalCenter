using EducationalCenter.Application.Common.Exceptions;
using EducationalCenter.Application.Features.Branches;
using EducationalCenter.Application.Features.Students;
using EducationalCenter.Domain.Constants;
using EducationalCenter.Domain.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace EducationalCenter.Tests.Integration;

[Collection("integration")]
public sealed class StudentTransferTests(AppFixture fx)
{
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
        var code = ("T" + Guid.NewGuid().ToString("N")[..4]).ToUpperInvariant();
        return await scope.ServiceProvider.GetRequiredService<IBranchService>()
            .CreateAsync(new CreateBranchRequest("Move " + code, code, null, null, null));
    }

    private async Task<T> InStudentsAsync<T>(Func<IStudentService, Task<T>> action)
    {
        await using var scope = fx.NewScope();
        return await action(scope.ServiceProvider.GetRequiredService<IStudentService>());
    }

    [Fact]
    public async Task A_student_moves_to_another_branch_and_only_that_branch_sees_them_afterwards()
    {
        try
        {
            var a = await NewBranchAsync();
            var b = await NewBranchAsync();

            InBranch(a.Id);
            StudentDto student;
            await using (var scope = fx.NewScope())
                student = await new Seeder(scope.ServiceProvider).StudentAsync();

            SuperAdmin();
            var result = await InStudentsAsync(s => s.TransferAsync(student.Id, new TransferStudentRequest(b.Id, "moved house")));
            Assert.Equal(a.Id, result.FromBranchId);
            Assert.Equal(b.Id, result.ToBranchId);

            InBranch(b.Id);
            Assert.Equal(student.Id, (await InStudentsAsync(s => s.GetByIdAsync(student.Id))).Id);

            InBranch(a.Id);
            await Assert.ThrowsAsync<NotFoundException>(() => InStudentsAsync(s => s.GetByIdAsync(student.Id)));
        }
        finally { Reset(); }
    }

    [Fact]
    public async Task A_transfer_is_refused_to_the_same_branch_and_while_the_student_has_an_open_enrollment()
    {
        try
        {
            var a = await NewBranchAsync();
            var b = await NewBranchAsync();

            InBranch(a.Id);
            StudentDto enrolled;
            await using (var scope = fx.NewScope())
            {
                var seed = new Seeder(scope.ServiceProvider);
                var (course, trainer, room) = await seed.BaseAsync();
                var section = await seed.SectionAsync(course, trainer, room);
                enrolled = (await seed.EnrollAsync(section.Id)).Student;
            }

            SuperAdmin();
            await Assert.ThrowsAsync<ConflictException>(() =>
                InStudentsAsync(s => s.TransferAsync(enrolled.Id, new TransferStudentRequest(a.Id, "same"))));
            await Assert.ThrowsAsync<ConflictException>(() =>
                InStudentsAsync(s => s.TransferAsync(enrolled.Id, new TransferStudentRequest(b.Id, "busy"))));
        }
        finally { Reset(); }
    }

    [Fact]
    public void Only_the_super_admin_role_can_transfer_students()
    {
        Assert.Contains(Permissions.Students.Transfer, SystemRoles.AdminPermissions);
        Assert.DoesNotContain(Permissions.Students.Transfer, SystemRoles.BranchManagerPermissions);
    }
}