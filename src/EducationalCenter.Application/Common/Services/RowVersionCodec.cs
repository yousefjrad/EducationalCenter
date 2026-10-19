using EducationalCenter.Application.Common.Exceptions;

namespace EducationalCenter.Application.Common.Services;

/// <summary>Row versions travel to the client and back as base64 text.</summary>
public static class RowVersionCodec
{
    public static string? Encode(byte[]? version) =>
        version is { Length: > 0 } ? Convert.ToBase64String(version) : null;

    public static byte[]? Decode(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        try { return Convert.FromBase64String(text); }
        catch (FormatException) { throw new RequestValidationException("rowVersion", "Invalid record version."); }
    }
}