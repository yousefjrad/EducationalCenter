using EducationalCenter.Domain.Entities;

namespace EducationalCenter.Application.Features.Branches;

public static class BranchMappings
{
    public static BranchDto ToDto(this Branch b) =>
        new(b.Id, b.Code, b.Name, b.Governorate, b.Address, b.Phone, b.IsActive, b.CreatedAt);
}