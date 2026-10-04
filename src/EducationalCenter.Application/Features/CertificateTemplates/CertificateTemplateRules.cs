namespace EducationalCenter.Application.Features.CertificateTemplates;

public static class CertificateTemplateRules
{
    public static readonly string[] Languages = ["ar", "en"];

    public static readonly string[] AllowedVariables =
        ["StudentName", "CourseName", "SectionName", "CertificateNumber", "IssueDate", "CenterName"];
}
