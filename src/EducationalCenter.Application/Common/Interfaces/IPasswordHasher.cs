namespace EducationalCenter.Application.Common.Interfaces;

/// <summary>Implemented in Infrastructure with a slow, salted algorithm (never a plain hash).</summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}
