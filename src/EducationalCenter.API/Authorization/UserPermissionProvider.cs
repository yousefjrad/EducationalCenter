using EducationalCenter.Application.Common.Interfaces;

namespace EducationalCenter.API.Authorization;

/// <summary>
/// Reads the user's permissions from the database, once per request (an endpoint may check several).
/// Nothing is kept between requests, so changing a role takes effect on the user's very next request.
/// </summary>
public sealed class UserPermissionProvider(IUnitOfWork uow)
{
    private int _userId;
    private HashSet<string>? _permissions;

    public async Task<bool> HasAsync(int userId, string permission, CancellationToken ct)
    {
        if (_permissions is null || _userId != userId)
        {
            _permissions = (await uow.Users.GetPermissionNamesAsync(userId, ct)).ToHashSet();
            _userId = userId;
        }

        return _permissions.Contains(permission);
    }
}
