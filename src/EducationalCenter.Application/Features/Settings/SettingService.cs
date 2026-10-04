using System.Globalization;
using EducationalCenter.Application.Common.Exceptions;
using EducationalCenter.Application.Common.Interfaces;
using EducationalCenter.Domain.Constants;
using EducationalCenter.Domain.Exceptions;

namespace EducationalCenter.Application.Features.Settings;

public sealed class SettingService(IUnitOfWork uow, IClock clock, ICurrentUser currentUser) : ISettingService
{
    public async Task<IReadOnlyList<SettingDto>> ListAsync(CancellationToken ct = default)
    {
        var settings = await uow.Settings.ListAsync(ct);
        return settings.Select(s => s.ToDto()).ToList();
    }

    public async Task<SettingDto> GetByKeyAsync(string key, CancellationToken ct = default)
    {
        var setting = await uow.Settings.GetByKeyAsync(key, ct) ?? throw new NotFoundException("Setting", key);
        return setting.ToDto();
    }

    public async Task<SettingDto> UpdateAsync(string key, UpdateSettingRequest request, CancellationToken ct = default)
    {
        var setting = await uow.Settings.GetByKeyAsync(key, ct) ?? throw new NotFoundException("Setting", key);

        var value = request.Value.Trim();
        Validate(setting.Key, value);

        setting.Value = value;
        setting.UpdatedAt = clock.UtcNow;
        setting.UpdatedByUserId = currentUser.UserId;

        await uow.SaveChangesAsync(ct);
        return setting.ToDto();
    }

    private static void Validate(string key, string value)
    {
        switch (key)
        {
            case SettingKeys.PassingScore:
                RequireWholeNumber(value, 1, 100, "PassingScore must be a whole number between 1 and 100.");
                break;
            case SettingKeys.EnrollmentHoldHours:
                RequireWholeNumber(value, 1, 720, "EnrollmentHoldHours must be a whole number between 1 and 720.");
                break;
            case SettingKeys.CenterName:
                if (value.Length > 150)
                    throw new RequestValidationException("value", "CenterName cannot exceed 150 characters.");
                break;
        }
    }

    private static void RequireWholeNumber(string value, int min, int max, string message)
    {
        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var number)
            || number < min || number > max)
            throw new RequestValidationException("value", message);
    }
}
