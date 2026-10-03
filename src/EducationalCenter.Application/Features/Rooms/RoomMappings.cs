using EducationalCenter.Domain.Entities;

namespace EducationalCenter.Application.Features.Rooms;

public static class RoomMappings
{
    public static RoomDto ToDto(this Room r) => new(r.Id, r.Name, r.Capacity, r.Description, r.Location, r.IsActive);
}
