namespace EducationalCenter.Application.Features.Rooms;

public sealed record RoomDto(int Id, string Name, int Capacity, string? Description, string? Location, bool IsActive);

public sealed record CreateRoomRequest(string Name, int Capacity, string? Description, string? Location);

public sealed record UpdateRoomRequest(string Name, int Capacity, string? Description, string? Location, bool IsActive);

public sealed record RoomListQuery(string? Search = null, bool? IsActive = null, int Page = 1, int PageSize = 20);
