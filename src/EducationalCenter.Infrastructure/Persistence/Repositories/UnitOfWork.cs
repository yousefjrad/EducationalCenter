using EducationalCenter.Application.Common.Interfaces;
using EducationalCenter.Application.Common.Interfaces.Repositories;

namespace EducationalCenter.Infrastructure.Persistence.Repositories;

/// <summary>One per request. Repositories share the same DbContext, so one SaveChanges saves them all.</summary>
internal sealed class UnitOfWork(AppDbContext db) : IUnitOfWork
{
    private ICourseRepository? _courses;
    private IRoomRepository? _rooms;
    private ITrainerRepository? _trainers;
    private IStudentRepository? _students;
    private ISectionRepository? _sections;
    private IClassSessionRepository? _classSessions;
    private IEnrollmentRepository? _enrollments;
    private IWaitingListRepository? _waitingList;
    private IAttendanceRepository? _attendances;
    private IGradeRepository? _grades;
    private ICertificateRepository? _certificates;
    private ICertificateTemplateRepository? _certificateTemplates;
    private IPaymentPlanRepository? _paymentPlans;
    private IPaymentRepository? _payments;
    private IReceiptRepository? _receipts;
    private IExpenseRepository? _expenses;
    private ITrainerPayrollRepository? _trainerPayrolls;
    private IUserRepository? _users;
    private IRoleRepository? _roles;
    private IPermissionRepository? _permissions;
    private ISettingRepository? _settings;
    private IRefreshTokenRepository? _refreshTokens;
    private IReportRepository? _reports;
    private IAuditLogRepository? _auditLogs;

    public ICourseRepository Courses => _courses ??= new CourseRepository(db);
    public IRoomRepository Rooms => _rooms ??= new RoomRepository(db);
    public ITrainerRepository Trainers => _trainers ??= new TrainerRepository(db);
    public IStudentRepository Students => _students ??= new StudentRepository(db);
    public ISectionRepository Sections => _sections ??= new SectionRepository(db);
    public IClassSessionRepository ClassSessions => _classSessions ??= new ClassSessionRepository(db);
    public IEnrollmentRepository Enrollments => _enrollments ??= new EnrollmentRepository(db);
    public IWaitingListRepository WaitingList => _waitingList ??= new WaitingListRepository(db);
    public IAttendanceRepository Attendances => _attendances ??= new AttendanceRepository(db);
    public IGradeRepository Grades => _grades ??= new GradeRepository(db);
    public ICertificateRepository Certificates => _certificates ??= new CertificateRepository(db);
    public ICertificateTemplateRepository CertificateTemplates => _certificateTemplates ??= new CertificateTemplateRepository(db);
    public IPaymentPlanRepository PaymentPlans => _paymentPlans ??= new PaymentPlanRepository(db);
    public IPaymentRepository Payments => _payments ??= new PaymentRepository(db);
    public IReceiptRepository Receipts => _receipts ??= new ReceiptRepository(db);
    public IExpenseRepository Expenses => _expenses ??= new ExpenseRepository(db);
    public ITrainerPayrollRepository TrainerPayrolls => _trainerPayrolls ??= new TrainerPayrollRepository(db);
    public IUserRepository Users => _users ??= new UserRepository(db);
    public IRoleRepository Roles => _roles ??= new RoleRepository(db);
    public IPermissionRepository Permissions => _permissions ??= new PermissionRepository(db);
    public ISettingRepository Settings => _settings ??= new SettingRepository(db);
    public IRefreshTokenRepository RefreshTokens => _refreshTokens ??= new RefreshTokenRepository(db);
    public IReportRepository Reports => _reports ??= new ReportRepository(db);
    public IAuditLogRepository AuditLogs => _auditLogs ??= new AuditLogRepository(db);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);

    public Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken ct = default) =>
        ExecuteInTransactionAsync<bool>(async () =>
        {
            await action();
            return true;
        }, ct);

    /// <summary>
    /// Everything the action saves is committed together or rolled back together if it throws.
    /// A call made while a transaction is already open simply joins that transaction.
    /// </summary>
    public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action, CancellationToken ct = default)
    {
        if (db.Database.CurrentTransaction is not null)
            return await action();

        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        var result = await action();

        await transaction.CommitAsync(ct);
        return result;
    }
}
