using EducationalCenter.Application.Features.Courses;

namespace EducationalCenter.Tests;

public class CourseValidatorTests
{
    private readonly CreateCourseRequestValidator _validator = new();

    private static CreateCourseRequest Req(
        string code = "PY101", string name = "Python Basics", int hours = 40, decimal price = 500_000m) =>
        new(code, name, null, null, hours, price);

    [Fact]
    public void A_complete_request_is_valid() => Check.Valid(_validator.Validate(Req()));

    [Theory]
    [InlineData(0)]
    [InlineData(10_001)]
    public void Duration_must_be_between_1_and_10000_hours(int hours) =>
        Check.ErrorOn(_validator.Validate(Req(hours: hours)), "DefaultDurationHours");

    [Theory]
    [InlineData(1)]
    [InlineData(10_000)]
    public void Duration_limits_are_inclusive(int hours) => Check.Valid(_validator.Validate(Req(hours: hours)));

    [Theory]
    [InlineData("PY 101")]
    [InlineData("PY#101")]
    [InlineData("")]
    public void The_code_allows_only_letters_digits_dash_and_underscore(string code) =>
        Check.ErrorOn(_validator.Validate(Req(code: code)), "Code");

    [Fact]
    public void The_code_is_limited_to_20_characters() =>
        Check.ErrorOn(_validator.Validate(Req(code: new string('A', 21))), "Code");

    [Fact]
    public void A_negative_price_is_rejected() =>
        Check.ErrorOn(_validator.Validate(Req(price: -1m)), "DefaultPrice");

    [Fact]
    public void A_free_course_is_allowed() => Check.Valid(_validator.Validate(Req(price: 0m)));

    [Fact]
    public void The_name_is_required() => Check.ErrorOn(_validator.Validate(Req(name: "")), "Name");
}