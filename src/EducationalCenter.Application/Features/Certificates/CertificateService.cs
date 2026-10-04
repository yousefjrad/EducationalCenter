using System.Globalization;
using EducationalCenter.Application.Common.Exceptions;
using EducationalCenter.Application.Common.Interfaces;
using EducationalCenter.Application.Common.Models;
using EducationalCenter.Application.Features.CertificateTemplates;
using EducationalCenter.Domain.Entities;
using EducationalCenter.Domain.Enums;
using EducationalCenter.Domain.Exceptions;

namespace EducationalCenter.Application.Features.Certificates;

public sealed class CertificateService(
    IUnitOfWork uow,
    IClock clock,
    ICurrentUser currentUser,
    ICertificatePdfGenerator pdfGenerator) : ICertificateService
{
    public async Task<CertificateDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var certificate = await uow.Certificates.GetWithDetailsAsync(id, ct)
            ?? throw new NotFoundException(nameof(Certificate), id);
        return certificate.ToDto(certificate.Enrollment);
    }

    public async Task<PagedResult<CertificateDto>> ListAsync(CertificateListQuery query, CancellationToken ct = default)
    {
        var (items, total) = await uow.Certificates.SearchAsync(
            query.StudentId, query.SectionId, query.Page, query.PageSize, ct);

        return new PagedResult<CertificateDto>(
            items.Select(c => c.ToDto(c.Enrollment)).ToList(), total, query.Page, query.PageSize);
    }

    public async Task<CertificateEligibilityDto> GetEligibilityAsync(int enrollmentId, CancellationToken ct = default)
    {
        var enrollment = await uow.Enrollments.GetWithDetailsAsync(enrollmentId, ct)
            ?? throw new NotFoundException(nameof(Enrollment), enrollmentId);

        var alreadyIssued = enrollment.Certificate is not null;
        var blockers = await GetBlockersAsync(enrollment, ct);

        return new CertificateEligibilityDto(enrollment.Id, alreadyIssued, !alreadyIssued && blockers.Count == 0, blockers);
    }

    public Task<CertificateDto> IssueAsync(int enrollmentId, CancellationToken ct = default) =>
        IssueCoreAsync(enrollmentId, overrideReason: null, allowOverride: false, ct);

    public Task<CertificateDto> IssueWithOverrideAsync(
        int enrollmentId, IssueCertificateOverrideRequest request, CancellationToken ct = default) =>
        IssueCoreAsync(enrollmentId, request.Reason, allowOverride: true, ct);

    public async Task<CertificateFileDto> GetPdfAsync(int id, string language = "ar", CancellationToken ct = default)
    {
        if (!CertificateTemplateRules.Languages.Contains(language))
            throw new RequestValidationException("language", "'language' must be 'ar' or 'en'.");

        var certificate = await uow.Certificates.GetWithDetailsAsync(id, ct)
            ?? throw new NotFoundException(nameof(Certificate), id);

        var template = await uow.CertificateTemplates.GetDefaultAsync(language, ct)
            ?? throw new ConflictException($"No default certificate template is configured for language '{language}'.");

        var enrollment = certificate.Enrollment;
        var issueDate = DateOnly.FromDateTime(certificate.IssuedAt);

        var values = new Dictionary<string, string>
        {
            ["StudentName"] = enrollment.Student.FullName,
            ["CourseName"] = enrollment.Section.Course.Name,
            ["SectionName"] = enrollment.Section.Name,
            ["CertificateNumber"] = certificate.CertificateNumber,
            ["IssueDate"] = issueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["CenterName"] = template.CenterName
        };

        var body = values.Aggregate(template.BodyText, (text, pair) => text.Replace("{" + pair.Key + "}", pair.Value));

        var model = new CertificatePdfModel(
            template.CenterName,
            template.LogoPath,
            template.SignerName,
            template.SignerTitle,
            body,
            certificate.CertificateNumber,
            issueDate,
            template.Language);

        return new CertificateFileDto(pdfGenerator.Generate(model), $"certificate-{certificate.CertificateNumber}.pdf");
    }

    private Task<CertificateDto> IssueCoreAsync(int enrollmentId, string? overrideReason, bool allowOverride, CancellationToken ct)
    {
        return uow.ExecuteInTransactionAsync(async () =>
        {
            var userId = currentUser.RequireUserId();

            var enrollment = await uow.Enrollments.GetWithDetailsAsync(enrollmentId, ct)
                ?? throw new NotFoundException(nameof(Enrollment), enrollmentId);

            if (enrollment.Status is not (EnrollmentStatus.Confirmed or EnrollmentStatus.Completed))
                throw new ConflictException("Certificates can only be issued for confirmed or completed enrollments.");

            if (enrollment.Certificate is not null)
                throw new ConflictException("A certificate was already issued for this enrollment.");

            var blockers = await GetBlockersAsync(enrollment, ct);
            var bypassed = blockers.Count > 0;

            if (bypassed && !allowOverride)
                throw new ConflictException("The certificate cannot be issued: " + string.Join("; ", blockers) + ".");

            var now = clock.UtcNow;
            var sequence = await uow.Certificates.GetNextSequenceAsync(now.Year, ct);

            var certificate = new Certificate
            {
                EnrollmentId = enrollment.Id,
                Enrollment = enrollment,
                CertificateNumber = $"CERT-{now.Year}-{sequence:D5}",
                IssuedAt = now,
                IssuedByUserId = userId,
                IsOverride = bypassed,
                OverrideReason = bypassed ? overrideReason?.Trim() : null
            };

            await uow.Certificates.AddAsync(certificate, ct);
            await uow.SaveChangesAsync(ct);
            return certificate.ToDto(enrollment);
        }, ct);
    }

    /// <summary>A certificate needs a passing final grade and a fully paid enrollment.</summary>
    private async Task<IReadOnlyList<string>> GetBlockersAsync(Enrollment enrollment, CancellationToken ct)
    {
        var blockers = new List<string>();

        if (enrollment.Grade is null)
            blockers.Add("no final grade has been recorded");
        else if (!enrollment.Grade.IsPassed)
            blockers.Add("the student did not reach the passing score");

        var paid = await uow.Enrollments.GetPaidAmountInSypAsync(enrollment.Id, ct);
        var remaining = enrollment.AgreedPrice - paid;
        if (remaining > 0m)
            blockers.Add($"an unpaid balance of {remaining.ToString("0.##", CultureInfo.InvariantCulture)} SYP remains");

        return blockers;
    }
}
