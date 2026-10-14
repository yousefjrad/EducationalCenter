using EducationalCenter.Application.Features.Branches;
using EducationalCenter.Application.Features.Rooms;
using Microsoft.Extensions.DependencyInjection;

namespace EducationalCenter.Tests.Integration;

[Collection("integration")]
public sealed class PerBranchNamesTests(AppFixture fx)
{
    private async Task<BranchDto> NewBranchAsync()
    {
        fx.Branch.IsSuperAdmin = true;
        fx.Branch.BranchId = null;
        await using var scope = fx.NewScope();
        var code = ("T" + Guid.NewGuid().ToString("N")[..4]).ToUpperInvariant();
        return await scope.ServiceProvider.GetRequiredService<IBranchService>()
            .CreateAsync(new CreateBranchRequest("Names " + code, code, null, null, null));
    }

    private async Task<RoomDto> CreateRoomAsync(int branchId, string name)
    {
        fx.Branch.IsSuperAdmin = false;
        fx.Branch.BranchId = branchId;
        await using var scope = fx.NewScope();
        return await scope.ServiceProvider.GetRequiredService<IRoomService>()
            .CreateAsync(new CreateRoomRequest(name, 10, null, null));
    }

    [Fact]
    public async Task Two_branches_can_use_the_same_room_name_but_one_branch_cannot_repeat_it()
    {
        try
        {
            var a = await NewBranchAsync();
            var b = await NewBranchAsync();
            var name = "Hall " + Guid.NewGuid().ToString("N")[..6];

            var roomA = await CreateRoomAsync(a.Id, name);
            var roomB = await CreateRoomAsync(b.Id, name);

            Assert.NotEqual(roomA.Id, roomB.Id);

            await Assert.ThrowsAnyAsync<Exception>(() => CreateRoomAsync(a.Id, name));
        }
        finally
        {
            fx.Branch.IsSuperAdmin = false;
            fx.Branch.BranchId = 1;
        }
    }
}