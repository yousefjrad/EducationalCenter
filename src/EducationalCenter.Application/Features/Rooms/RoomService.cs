using EducationalCenter.Application.Common.Interfaces;
using EducationalCenter.Application.Common.Models;
using EducationalCenter.Domain.Entities;
using EducationalCenter.Domain.Exceptions;

namespace EducationalCenter.Application.Features.Rooms;

public sealed class RoomService(IUnitOfWork uow) : IRoomService
{
    public async Task<RoomDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var room = await uow.Rooms.GetByIdAsync(id, ct) ?? throw new NotFoundException(nameof(Room), id);
        return room.ToDto();
    }

    public async Task<PagedResult<RoomDto>> ListAsync(RoomListQuery query, CancellationToken ct = default)
    {
        var (items, total) = await uow.Rooms.SearchAsync(
            query.Search?.Trim(), query.IsActive, query.Page, query.PageSize, ct);
        return new PagedResult<RoomDto>(items.Select(r => r.ToDto()).ToList(), total, query.Page, query.PageSize);
    }

    public async Task<RoomDto> CreateAsync(CreateRoomRequest request, CancellationToken ct = default)
    {
        var name = request.Name.Trim();
        if (await uow.Rooms.NameExistsAsync(name, null, ct))
            throw new ConflictException($"A room named '{name}' already exists.");

        var room = new Room
        {
            Name = name,
            Capacity = request.Capacity,
            Description = request.Description?.Trim(),
            Location = request.Location?.Trim(),
            IsActive = true
        };

        await uow.Rooms.AddAsync(room, ct);
        await uow.SaveChangesAsync(ct);
        return room.ToDto();
    }

    public async Task<RoomDto> UpdateAsync(int id, UpdateRoomRequest request, CancellationToken ct = default)
    {
        var room = await uow.Rooms.GetByIdAsync(id, ct) ?? throw new NotFoundException(nameof(Room), id);

        var name = request.Name.Trim();
        if (await uow.Rooms.NameExistsAsync(name, id, ct))
            throw new ConflictException($"A room named '{name}' already exists.");

        room.Name = name;
        room.Capacity = request.Capacity;
        room.Description = request.Description?.Trim();
        room.Location = request.Location?.Trim();
        room.IsActive = request.IsActive;

        uow.Rooms.Update(room);
        await uow.SaveChangesAsync(ct);
        return room.ToDto();
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var room = await uow.Rooms.GetByIdAsync(id, ct) ?? throw new NotFoundException(nameof(Room), id);

        if (await uow.Rooms.IsInUseAsync(id, ct))
            throw new ConflictException("This room is used by sections or sessions and cannot be deleted. Deactivate it instead.");

        uow.Rooms.Remove(room);
        await uow.SaveChangesAsync(ct);
    }
}
