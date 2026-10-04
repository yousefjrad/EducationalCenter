using EducationalCenter.Application.Common.Exceptions;
using EducationalCenter.Application.Common.Interfaces;
using EducationalCenter.Domain.Entities;
using EducationalCenter.Domain.Exceptions;

namespace EducationalCenter.Application.Features.CertificateTemplates;

/// <remarks>Exactly one default template per language.</remarks>
public sealed class CertificateTemplateService(IUnitOfWork uow) : ICertificateTemplateService
{
    public async Task<CertificateTemplateDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var template = await uow.CertificateTemplates.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(CertificateTemplate), id);
        return template.ToDto();
    }

    public async Task<IReadOnlyList<CertificateTemplateDto>> ListAsync(string? language = null, CancellationToken ct = default)
    {
        IReadOnlyList<CertificateTemplate> templates;

        if (language is null)
        {
            templates = await uow.CertificateTemplates.ListAsync(ct);
        }
        else
        {
            EnsureValidLanguage(language);
            templates = await uow.CertificateTemplates.GetByLanguageAsync(language, ct);
        }

        return templates
            .OrderBy(t => t.Language).ThenByDescending(t => t.IsDefault).ThenBy(t => t.CenterName)
            .Select(t => t.ToDto())
            .ToList();
    }

    public async Task<CertificateTemplateDto> CreateAsync(CreateCertificateTemplateRequest request, CancellationToken ct = default)
    {
        var sameLanguage = await uow.CertificateTemplates.GetByLanguageAsync(request.Language, ct);

        // The first template of a language is always its default.
        var makeDefault = request.IsDefault || sameLanguage.Count == 0;
        if (makeDefault)
            UnsetDefaults(sameLanguage);

        var template = new CertificateTemplate
        {
            CenterName = request.CenterName.Trim(),
            LogoPath = request.LogoPath?.Trim(),
            SignerName = request.SignerName.Trim(),
            SignerTitle = request.SignerTitle.Trim(),
            BodyText = request.BodyText.Trim(),
            Language = request.Language,
            IsDefault = makeDefault
        };

        await uow.CertificateTemplates.AddAsync(template, ct);
        await uow.SaveChangesAsync(ct);
        return template.ToDto();
    }

    public async Task<CertificateTemplateDto> UpdateAsync(int id, UpdateCertificateTemplateRequest request, CancellationToken ct = default)
    {
        var template = await uow.CertificateTemplates.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(CertificateTemplate), id);

        if (template.IsDefault && !request.IsDefault)
            throw new ConflictException("This is the default template. Set another template as default instead.");

        if (request.IsDefault && !template.IsDefault)
        {
            var sameLanguage = await uow.CertificateTemplates.GetByLanguageAsync(template.Language, ct);
            UnsetDefaults(sameLanguage);
            template.IsDefault = true;
        }

        template.CenterName = request.CenterName.Trim();
        template.LogoPath = request.LogoPath?.Trim();
        template.SignerName = request.SignerName.Trim();
        template.SignerTitle = request.SignerTitle.Trim();
        template.BodyText = request.BodyText.Trim();

        uow.CertificateTemplates.Update(template);
        await uow.SaveChangesAsync(ct);
        return template.ToDto();
    }

    public async Task<CertificateTemplateDto> SetDefaultAsync(int id, CancellationToken ct = default)
    {
        var template = await uow.CertificateTemplates.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(CertificateTemplate), id);

        if (!template.IsDefault)
        {
            var sameLanguage = await uow.CertificateTemplates.GetByLanguageAsync(template.Language, ct);
            UnsetDefaults(sameLanguage);
            template.IsDefault = true;
            uow.CertificateTemplates.Update(template);
            await uow.SaveChangesAsync(ct);
        }

        return template.ToDto();
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var template = await uow.CertificateTemplates.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(CertificateTemplate), id);

        if (template.IsDefault)
            throw new ConflictException("The default template cannot be deleted. Set another template as default first.");

        uow.CertificateTemplates.Remove(template);
        await uow.SaveChangesAsync(ct);
    }

    private void UnsetDefaults(IEnumerable<CertificateTemplate> templates)
    {
        foreach (var other in templates.Where(t => t.IsDefault))
        {
            other.IsDefault = false;
            uow.CertificateTemplates.Update(other);
        }
    }

    private static void EnsureValidLanguage(string language)
    {
        if (!CertificateTemplateRules.Languages.Contains(language))
            throw new RequestValidationException("language", "'language' must be 'ar' or 'en'.");
    }
}
