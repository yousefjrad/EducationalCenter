using EducationalCenter.Application.Common.Interfaces;
using EducationalCenter.Application.Common.Services;
using EducationalCenter.Application.Features.Attendances;
using EducationalCenter.Application.Features.Alerts;
using EducationalCenter.Application.Features.AuditLogs;
using EducationalCenter.Application.Features.Auth;
using EducationalCenter.Application.Features.CertificateTemplates;
using EducationalCenter.Application.Features.Certificates;
using EducationalCenter.Application.Features.ClassSessions;
using EducationalCenter.Application.Features.Courses;
using EducationalCenter.Application.Features.Enrollments;
using EducationalCenter.Application.Features.Expenses;
using EducationalCenter.Application.Features.Grades;
using EducationalCenter.Application.Features.Imports;
using EducationalCenter.Application.Features.PaymentPlans;
using EducationalCenter.Application.Features.Payments;
using EducationalCenter.Application.Features.Receipts;
using EducationalCenter.Application.Features.Reports;
using EducationalCenter.Application.Features.Roles;
using EducationalCenter.Application.Features.Settings;
using EducationalCenter.Application.Features.Rooms;
using EducationalCenter.Application.Features.Sections;
using EducationalCenter.Application.Features.Students;
using EducationalCenter.Application.Features.TrainerPayrolls;
using EducationalCenter.Application.Features.Trainers;
using EducationalCenter.Application.Features.Users;
using EducationalCenter.Application.Features.WaitingList;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace EducationalCenter.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Registers every AbstractValidator<T> in this assembly.
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddScoped<IAuditLogger, AuditLogger>();

        services.AddScoped<ICourseService, CourseService>();
        services.AddScoped<IRoomService, RoomService>();
        services.AddScoped<ITrainerService, TrainerService>();
        services.AddScoped<IStudentService, StudentService>();
        services.AddScoped<IStudentAccountService, StudentAccountService>();
        services.AddScoped<ISectionService, SectionService>();
        services.AddScoped<IClassSessionService, ClassSessionService>();
        services.AddScoped<IEnrollmentService, EnrollmentService>();
        services.AddScoped<IWaitingListService, WaitingListService>();
        services.AddScoped<IAttendanceService, AttendanceService>();
        services.AddScoped<IGradeService, GradeService>();
        services.AddScoped<ICertificateTemplateService, CertificateTemplateService>();
        services.AddScoped<ICertificateService, CertificateService>();
        services.AddScoped<IPaymentPlanService, PaymentPlanService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IReceiptService, ReceiptService>();
        services.AddScoped<IExpenseService, ExpenseService>();
        services.AddScoped<ITrainerPayrollService, TrainerPayrollService>();
        services.AddScoped<ISettingService, SettingService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IFinancialReportService, FinancialReportService>();
        services.AddScoped<IOperationalReportService, OperationalReportService>();
        services.AddScoped<IImporter, StudentImporter>();
        services.AddScoped<IImporter, CourseImporter>();
        services.AddScoped<IImporter, SectionImporter>();
        services.AddScoped<IImporter, LegacyPaymentImporter>();
        services.AddScoped<IImportService, ImportService>();
        services.AddScoped<IAlertService, AlertService>();
        // Later features are registered here, following the same pattern.

        return services;
    }
}
