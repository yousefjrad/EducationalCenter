using EducationalCenter.Application.Common.Interfaces;
using EducationalCenter.Domain.Constants;
using EducationalCenter.Domain.Entities;
using EducationalCenter.Domain.Exceptions;

namespace EducationalCenter.Application.Features.Roles;

public sealed class RoleService(IUnitOfWork uow) : IRoleService
{
    public async Task<RoleDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var role = await uow.Roles.GetWithPermissionsAsync(id, ct) ?? throw new NotFoundException(nameof(Role), id);
        return role.ToDto();
    }

    public async Task<IReadOnlyList<RoleDto>> ListAsync(CancellationToken ct = default)
    {
        var roles = await uow.Roles.ListWithPermissionsAsync(ct);
        return roles.Select(r => r.ToDto()).ToList();
    }

    public Task<IReadOnlyList<PermissionGroupDto>> ListPermissionsAsync(CancellationToken ct = default)
    {
        IReadOnlyList<PermissionGroupDto> groups = Permissions.All
            .GroupBy(name => name[..name.IndexOf('.')])
            .Select(g => new PermissionGroupDto(g.Key, g.OrderBy(n => n).ToList()))
            .OrderBy(g => g.Module)
            .ToList();

        return Task.FromResult(groups);
    }

    public async Task<RoleDto> CreateAsync(CreateRoleRequest request, CancellationToken ct = default)
    {
        var name = request.Name.Trim();
        if (await uow.Roles.NameExistsAsync(name, null, ct))
            throw new ConflictException($"A role named '{name}' already exists.");

        var permissions = await LoadPermissionsAsync(request.PermissionNames, ct);

        var role = new Role
        {
            Name = name,
            Description = request.Description?.Trim(),
            IsSystem = false
        };

        foreach (var permission in permissions)
            role.RolePermissions.Add(new RolePermission { Role = role, Permission = permission, PermissionId = permission.Id });

        await uow.Roles.AddAsync(role, ct);
        await uow.SaveChangesAsync(ct);
        return role.ToDto();
    }

    public async Task<RoleDto> UpdateAsync(int id, UpdateRoleRequest request, CancellationToken ct = default)
    {
        var role = await uow.Roles.GetWithPermissionsAsync(id, ct) ?? throw new NotFoundException(nameof(Role), id);

        if (role.Name == SystemRoles.Admin)
            throw new ConflictException("The Admin role always has every permission and cannot be modified.");

        var name = request.Name.Trim();
        if (role.IsSystem && !string.Equals(name, role.Name, StringComparison.Ordinal))
            throw new ConflictException("A system role cannot be renamed.");

        if (!string.Equals(name, role.Name, StringComparison.OrdinalIgnoreCase)
            && await uow.Roles.NameExistsAsync(name, id, ct))
            throw new ConflictException($"A role named '{name}' already exists.");

        var requested = request.PermissionNames.Distinct().ToList();
        var permissions = await LoadPermissionsAsync(requested, ct);

        role.Name = name;
        role.Description = request.Description?.Trim();

        var toRemove = role.RolePermissions.Where(rp => !requested.Contains(rp.Permission.Name)).ToList();
        uow.Roles.RemoveRolePermissions(toRemove);
        foreach (var rolePermission in toRemove)
            role.RolePermissions.Remove(rolePermission);

        var currentNames = role.RolePermissions.Select(rp => rp.Permission.Name).ToHashSet();
        foreach (var permission in permissions.Where(p => !currentNames.Contains(p.Name)))
            role.RolePermissions.Add(new RolePermission { Role = role, Permission = permission, PermissionId = permission.Id });

        await uow.SaveChangesAsync(ct);
        return role.ToDto();
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var role = await uow.Roles.GetByIdAsync(id, ct) ?? throw new NotFoundException(nameof(Role), id);

        if (role.IsSystem)
            throw new ConflictException("System roles cannot be deleted.");

        if (await uow.Roles.HasUsersAsync(id, ct))
            throw new ConflictException("This role still has users. Move them to another role first.");

        uow.Roles.Remove(role);
        await uow.SaveChangesAsync(ct);
    }

    private async Task<IReadOnlyList<Permission>> LoadPermissionsAsync(IEnumerable<string> names, CancellationToken ct)
    {
        var distinct = names.Distinct().ToList();
        var permissions = await uow.Permissions.GetByNamesAsync(distinct, ct);

        if (permissions.Count != distinct.Count)
            throw new ConflictException("Some permissions are missing from the database. Restart the application so they are seeded.");

        return permissions;
    }
}
