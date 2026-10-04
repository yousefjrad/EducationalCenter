using EducationalCenter.Application.Common.Models;

namespace EducationalCenter.Application.Features.Rooms;

public interface IRoomService
{
    Task<RoomDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<PagedResult<RoomDto>> ListAsync(RoomListQuery query, CancellationToken ct = default);
    Task<RoomDto> CreateAsync(CreateRoomRequest request, CancellationToken ct = default);
    Task<RoomDto> UpdateAsync(int id, UpdateRoomRequest request, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
