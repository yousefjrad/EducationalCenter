using FluentValidation;

namespace EducationalCenter.Application.Features.Settings;

public sealed class UpdateSettingRequestValidator : AbstractValidator<UpdateSettingRequest>
{
    public UpdateSettingRequestValidator()
    {
        RuleFor(x => x.Value).NotEmpty().MaximumLength(200);
    }
}
