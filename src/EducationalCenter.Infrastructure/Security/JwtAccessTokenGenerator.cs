using System.Globalization;
using System.Text;
using EducationalCenter.Application.Common.Interfaces;
using EducationalCenter.Application.Common.Models;
using EducationalCenter.Domain.Entities;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace EducationalCenter.Infrastructure.Security;

/// <remarks>
/// Claims use short names: sub (user id), email, name, role, jti.
/// The API must validate with NameClaimType = "name" and RoleClaimType = "role".
/// Permissions are deliberately NOT in the token: they are read from the database on each request,
/// so a role change takes effect immediately.
/// </remarks>
internal sealed class JwtAccessTokenGenerator(JwtOptions options, IClock clock) : IAccessTokenGenerator
{
    private readonly SigningCredentials _credentials = new(
        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SecretKey)),
        SecurityAlgorithms.HmacSha256);

    private readonly JsonWebTokenHandler _handler = new();

    public AccessTokenResult Generate(User user, string roleName)
    {
        var now = clock.UtcNow;
        var expires = now.AddMinutes(options.AccessTokenMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = options.Issuer,
            Audience = options.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expires,
            SigningCredentials = _credentials,
            Claims = new Dictionary<string, object>
            {
                ["sub"] = user.Id.ToString(CultureInfo.InvariantCulture),
                ["email"] = user.Email,
                ["name"] = user.FullName,
                ["role"] = roleName,
                ["jti"] = Guid.NewGuid().ToString("N")
            }
        };

        return new AccessTokenResult(_handler.CreateToken(descriptor), expires);
    }
}
