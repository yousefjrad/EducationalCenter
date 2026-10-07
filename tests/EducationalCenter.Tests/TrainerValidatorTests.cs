using EducationalCenter.Application.Features.Trainers;
using EducationalCenter.Domain.Enums;

namespace EducationalCenter.Tests;

public class TrainerValidatorTests
{
    private readonly CreateTrainerRequestValidator _validator = new();

    private static CreateTrainerRequest Req(
        TrainerPayType type = TrainerPayType.Monthly,
        decimal value = 3_000_000m,
        string phone = "+963991234567",
        string name = "Ahmad") =>
        new(name, phone, "Programming", type, value, Currency.Syp);

    [Fact]
    public void A_complete_request_is_valid() => Check.Valid(_validator.Validate(Req()));

    [Fact]
    public void A_percentage_of_100_is_allowed() =>
        Check.Valid(_validator.Validate(Req(TrainerPayType.Percentage, 100m)));

    [Fact]
    public void A_percentage_above_100_is_rejected() =>
        Check.ErrorOn(_validator.Validate(Req(TrainerPayType.Percentage, 100.01m)), "PayValue");

    [Fact]
    public void A_large_monthly_salary_is_not_treated_as_a_percentage() =>
        Check.Valid(_validator.Validate(Req(TrainerPayType.Monthly, 5_000_000m)));

    [Fact]
    public void A_negative_pay_value_is_rejected() =>
        Check.ErrorOn(_validator.Validate(Req(value: -1m)), "PayValue");

    [Fact]
    public void An_invalid_phone_number_is_rejected() =>
        Check.ErrorOn(_validator.Validate(Req(phone: "abc")), "PhoneNumber");

    [Fact]
    public void The_name_is_required() =>
        Check.ErrorOn(_validator.Validate(Req(name: "")), "FullName");

    [Fact]
    public void An_undefined_pay_type_is_rejected() =>
        Check.ErrorOn(_validator.Validate(Req(type: (TrainerPayType)99)), "PayType");
}