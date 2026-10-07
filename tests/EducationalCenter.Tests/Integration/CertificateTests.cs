using EducationalCenter.Application.Features.Certificates;
using EducationalCenter.Application.Features.Enrollments;
using EducationalCenter.Application.Features.Grades;
using EducationalCenter.Application.Features.PaymentPlans;
using EducationalCenter.Application.Features.Payments;
using EducationalCenter.Domain.Enums;
using EducationalCenter.Domain.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace EducationalCenter.Tests.Integration;

[Collection("integration")]
public sealed class CertificateTests(AppFixture fx)
{
    private static async Task<EnrollmentDto> EnrollmentAsync(IServiceProvider sp)
    {
        var seed = new Seeder(sp);
        var (course, trainer, room) = await seed.BaseAsync();
        var section = await seed.SectionAsync(course, trainer, room);
        var (_, enrollment) = await seed.EnrollAsync(section.Id);
        return enrollment;
    }

    private static Task<GradeDto> GradeAsync(IServiceProvider sp, int enrollmentId, decimal score) =>
        sp.GetRequiredService<IGradeService>().RecordAsync(new RecordGradeRequest(enrollmentId, score));

    private static async Task PayInFullAsync(IServiceProvider sp, int enrollmentId)
    {
        var plan = await sp.GetRequiredService<IPaymentPlanService>()
            .CreateAsync(new CreatePaymentPlanRequest(enrollmentId, 1, new DateOnly(2027, 1, 15)));
        await sp.GetRequiredService<IPaymentService>()
            .RecordAsync(new RecordPaymentRequest(plan.Installments[0].Id, Currency.Syp, plan.TotalAmount, null));
    }

    private static Task<CertificateEligibilityDto> EligibilityAsync(IServiceProvider sp, int enrollmentId) =>
        sp.GetRequiredService<ICertificateService>().GetEligibilityAsync(enrollmentId);

    [Fact]
    public async Task Without_a_grade_or_payment_there_is_no_eligibility()
    {
        await using var scope = fx.NewScope();
        var enrollment = await EnrollmentAsync(scope.ServiceProvider);

        var eligibility = await EligibilityAsync(scope.ServiceProvider, enrollment.Id);

        Assert.False(eligibility.IsEligible);
        Assert.NotEmpty(eligibility.Blockers);
    }

    [Fact]
    public async Task A_passing_grade_alone_is_not_enough()
    {
        await using var scope = fx.NewScope();
        var enrollment = await EnrollmentAsync(scope.ServiceProvider);
        await GradeAsync(scope.ServiceProvider, enrollment.Id, 85m);

        var eligibility = await EligibilityAsync(scope.ServiceProvider, enrollment.Id);

        Assert.False(eligibility.IsEligible);
    }

    [Fact]
    public async Task Full_payment_alone_is_not_enough()
    {
        await using var scope = fx.NewScope();
        var enrollment = await EnrollmentAsync(scope.ServiceProvider);
        await PayInFullAsync(scope.ServiceProvider, enrollment.Id);
        await GradeAsync(scope.ServiceProvider, enrollment.Id, 10m);

        var eligibility = await EligibilityAsync(scope.ServiceProvider, enrollment.Id);

        Assert.False(eligibility.IsEligible);
    }

    [Fact]
    public async Task A_passing_grade_and_full_payment_allow_a_normal_certificate_only_once()
    {
        await using var scope = fx.NewScope();
        var enrollment = await EnrollmentAsync(scope.ServiceProvider);
        await PayInFullAsync(scope.ServiceProvider, enrollment.Id);
        await GradeAsync(scope.ServiceProvider, enrollment.Id, 85m);
        var certificates = scope.ServiceProvider.GetRequiredService<ICertificateService>();

        var before = await EligibilityAsync(scope.ServiceProvider, enrollment.Id);
        var certificate = await certificates.IssueAsync(enrollment.Id);
        var after = await EligibilityAsync(scope.ServiceProvider, enrollment.Id);

        Assert.True(before.IsEligible);
        Assert.False(certificate.IsOverride);
        Assert.True(after.AlreadyIssued);
        await Assert.ThrowsAsync<ConflictException>(() => certificates.IssueAsync(enrollment.Id));
    }

    [Fact]
    public async Task A_normal_certificate_is_refused_while_something_blocks_it()
    {
        await using var scope = fx.NewScope();
        var enrollment = await EnrollmentAsync(scope.ServiceProvider);

        await Assert.ThrowsAsync<ConflictException>(
            () => scope.ServiceProvider.GetRequiredService<ICertificateService>().IssueAsync(enrollment.Id));
    }

    [Fact]
    public async Task An_override_issues_the_certificate_despite_the_blockers()
    {
        await using var scope = fx.NewScope();
        var enrollment = await EnrollmentAsync(scope.ServiceProvider);

        var certificate = await scope.ServiceProvider.GetRequiredService<ICertificateService>()
            .IssueWithOverrideAsync(enrollment.Id, new IssueCertificateOverrideRequest("test"));

        Assert.True(certificate.IsOverride);
    }

    [Fact]
    public async Task An_enrollment_with_a_certificate_cannot_be_cancelled_or_regraded()
    {
        await using var scope = fx.NewScope();
        var enrollment = await EnrollmentAsync(scope.ServiceProvider);
        await scope.ServiceProvider.GetRequiredService<ICertificateService>()
            .IssueWithOverrideAsync(enrollment.Id, new IssueCertificateOverrideRequest("test"));

        await Assert.ThrowsAsync<ConflictException>(
            () => scope.ServiceProvider.GetRequiredService<IEnrollmentService>().CancelAsync(enrollment.Id));
        await Assert.ThrowsAnyAsync<Exception>(() => GradeAsync(scope.ServiceProvider, enrollment.Id, 90m));
    }
}