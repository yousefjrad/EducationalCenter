using FluentValidation;

namespace EducationalCenter.Application.Features.PaymentPlans;

public sealed class CreatePaymentPlanRequestValidator : AbstractValidator<CreatePaymentPlanRequest>
{
    public CreatePaymentPlanRequestValidator()
    {
        RuleFor(x => x.EnrollmentId).GreaterThan(0);
        RuleFor(x => x.InstallmentsCount).InclusiveBetween(1, 24);
        RuleFor(x => x.FirstDueDate).Must(d => d != default).WithMessage("'First Due Date' is required.");
    }
}
