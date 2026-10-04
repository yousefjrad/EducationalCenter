using System.Security.Cryptography;
using System.Text;

namespace EducationalCenter.Application.Features.Auth;

/// <summary>The raw refresh token is shown to the client once; only its SHA-256 hash is stored.</summary>
internal static class RefreshTokenCodec
{
    public static string Generate() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
