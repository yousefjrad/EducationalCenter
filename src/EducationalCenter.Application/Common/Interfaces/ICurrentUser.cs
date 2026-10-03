namespace EducationalCenter.Application.Common.Interfaces;

/// <summary>The authenticated user of the current request (implemented in the API layer).</summary>
public interface ICurrentUser
{
    int? UserId { get; }
    bool IsAuthenticated { get; }
}
