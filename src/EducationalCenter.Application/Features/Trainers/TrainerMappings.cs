using EducationalCenter.Domain.Entities;

namespace EducationalCenter.Application.Features.Trainers;

public static class TrainerMappings
{
    public static TrainerDto ToDto(this Trainer t) => new(
        t.Id, t.FullName, t.PhoneNumber, t.Specialty, t.PayType, t.PayValue, t.PayCurrency, t.IsActive);
}
