using EducationalCenter.Application.Common.Models;
using EducationalCenter.Domain.Entities;

namespace EducationalCenter.Application.Common.Interfaces;

/// <summary>Creates the short-lived JWT access token. Implemented in Infrastructure/API (keys come from configuration).</summary>
public interface IAccessTokenGenerator
{
    AccessTokenResult Generate(User user, string roleName);
}
