using EducationalCenter.Domain.Entities;

namespace EducationalCenter.Application.Features.Branches;

public static class BranchMappings
{
    public static BranchDto ToDto(this Branch b) =>
        new(b.Id, b.Name, b.Code, b.Governorate, b.Address, b.Phone, b.IsActive, b.CreatedAt);
}