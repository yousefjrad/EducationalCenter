using EducationalCenter.Application.Common.Interfaces.Repositories;

namespace EducationalCenter.Application.Common.Interfaces;

public interface IUnitOfWork
{
    ICourseRepository Courses { get; }
    IRoomRepository Rooms { get; }
    ITrainerRepository Trainers { get; }
    IStudentRepository Students { get; }
    ISectionRepository Sections { get; }
    IClassSessionRepository ClassSessions { get; }
    IEnrollmentRepository Enrollments { get; }
    IWaitingListRepository WaitingList { get; }
    IAttendanceRepository Attendances { get; }
    IGradeRepository Grades { get; }
    ICertificateRepository Certificates { get; }
    ICertificateTemplateRepository CertificateTemplates { get; }
    IPaymentPlanRepository PaymentPlans { get; }
    IPaymentRepository Payments { get; }
    IReceiptRepository Receipts { get; }
    IExpenseRepository Expenses { get; }
    ITrainerPayrollRepository TrainerPayrolls { get; }
    IUserRepository Users { get; }
    IRoleRepository Roles { get; }
    IPermissionRepository Permissions { get; }
    ISettingRepository Settings { get; }
    IRefreshTokenRepository RefreshTokens { get; }
    IReportRepository Reports { get; }
    IAuditLogRepository AuditLogs { get; }
    // More repositories are added here as later features need them.

    /// <summary>Takes an update lock on the section row until the current transaction ends, so concurrent seat checks run one after another.</summary>
    Task LockSectionAsync(int sectionId, CancellationToken ct = default);

    Task<int> SaveChangesAsync(CancellationToken ct = default);

    /// <summary>Runs the action in one DB transaction (payments, enrollment...). Rolls back on exception.</summary>
    Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken ct = default);

    Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action, CancellationToken ct = default);
}
