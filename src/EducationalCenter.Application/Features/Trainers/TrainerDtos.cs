using EducationalCenter.Domain.Enums;

namespace EducationalCenter.Application.Features.Trainers;

public sealed record TrainerDto(
    int Id,
    string FullName,
    string PhoneNumber,
    string? Specialty,
    TrainerPayType PayType,
    decimal PayValue,
    Currency PayCurrency,
    bool IsActive);

public sealed record CreateTrainerRequest(
    string FullName,
    string PhoneNumber,
    string? Specialty,
    TrainerPayType PayType,
    decimal PayValue,
    Currency PayCurrency);

public sealed record UpdateTrainerRequest(
    string FullName,
    string PhoneNumber,
    string? Specialty,
    TrainerPayType PayType,
    decimal PayValue,
    Currency PayCurrency,
    bool IsActive);

public sealed record TrainerListQuery(string? Search = null, bool? IsActive = null, int Page = 1, int PageSize = 20);
