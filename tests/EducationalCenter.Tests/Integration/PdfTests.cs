using EducationalCenter.Application.Features.Certificates;
using EducationalCenter.Application.Features.PaymentPlans;
using EducationalCenter.Application.Features.Payments;
using EducationalCenter.Application.Features.Receipts;
using EducationalCenter.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace EducationalCenter.Tests.Integration;

[Collection("integration")]
public sealed class PdfTests(AppFixture fx)
{
    private static bool IsPdf(byte[] bytes) =>
        bytes.Length > 1000 && bytes[0] == '%' && bytes[1] == 'P' && bytes[2] == 'D' && bytes[3] == 'F';

    private static async Task<int> EnrollmentIdAsync(IServiceProvider sp)
    {
        var seed = new Seeder(sp);
        var (course, trainer, room) = await seed.BaseAsync();
        var section = await seed.SectionAsync(course, trainer, room);
        var (_, enrollment) = await seed.EnrollAsync(section.Id);
        return enrollment.Id;
    }

    [Theory]
    [InlineData("ar")]
    [InlineData("en")]
    public async Task A_receipt_is_a_real_pdf(string language)
    {
        await using var scope = fx.NewScope();
        var sp = scope.ServiceProvider;
        var enrollmentId = await EnrollmentIdAsync(sp);
        var plan = await sp.GetRequiredService<IPaymentPlanService>()
            .CreateAsync(new CreatePaymentPlanRequest(enrollmentId, 2, new DateOnly(2027, 1, 15)));
        var payment = await sp.GetRequiredService<IPaymentService>()
            .RecordAsync(new RecordPaymentRequest(plan.Installments[0].Id, Currency.Syp, 100_000m, null));

        var file = await sp.GetRequiredService<IReceiptService>().GetPdfAsync(payment.ReceiptId!.Value, language);

        Assert.True(IsPdf(file.Content));
    }

    [Fact]
    public async Task A_cancelled_payments_receipt_is_still_a_pdf()
    {
        await using var scope = fx.NewScope();
        var sp = scope.ServiceProvider;
        var enrollmentId = await EnrollmentIdAsync(sp);
        var plan = await sp.GetRequiredService<IPaymentPlanService>()
            .CreateAsync(new CreatePaymentPlanRequest(enrollmentId, 2, new DateOnly(2027, 1, 15)));
        var payments = sp.GetRequiredService<IPaymentService>();
        var payment = await payments.RecordAsync(new RecordPaymentRequest(plan.Installments[0].Id, Currency.Syp, 100_000m, null));
        await payments.CancelAsync(payment.Id, new CancelPaymentRequest("test"));

        var file = await sp.GetRequiredService<IReceiptService>().GetPdfAsync(payment.ReceiptId!.Value, "ar");

        Assert.True(IsPdf(file.Content));
    }

    [Theory]
    [InlineData("ar")]
    [InlineData("en")]
    public async Task A_certificate_is_a_real_pdf(string language)
    {
        await using var scope = fx.NewScope();
        var sp = scope.ServiceProvider;
        var enrollmentId = await EnrollmentIdAsync(sp);
        var certificates = sp.GetRequiredService<ICertificateService>();
        var certificate = await certificates.IssueWithOverrideAsync(enrollmentId, new IssueCertificateOverrideRequest("test"));

        var file = await certificates.GetPdfAsync(certificate.Id, language);

        Assert.True(IsPdf(file.Content));
    }
}