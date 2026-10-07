using FluentValidation.Results;

namespace EducationalCenter.Tests;

internal static class Check
{
    public static void ErrorOn(ValidationResult result, string property) =>
        Assert.True(
            result.Errors.Any(e => e.PropertyName == property),
            $"Expected an error on '{property}' but got: [{string.Join(", ", result.Errors.Select(e => e.PropertyName))}]");

    public static void Valid(ValidationResult result) =>
        Assert.True(
            result.IsValid,
            string.Join("; ", result.Errors.Select(e => e.PropertyName + ": " + e.ErrorMessage)));
}