using System.Linq.Expressions;
using EducationalCenter.Domain.Common;
using EducationalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EducationalCenter.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    // Academic
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Trainer> Trainers => Set<Trainer>();
    public DbSet<Section> Sections => Set<Section>();
    public DbSet<SectionSchedule> SectionSchedules => Set<SectionSchedule>();
    public DbSet<ClassSession> ClassSessions => Set<ClassSession>();

    // Students
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<WaitingListEntry> WaitingListEntries => Set<WaitingListEntry>();
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<Grade> Grades => Set<Grade>();
    public DbSet<Certificate> Certificates => Set<Certificate>();

    // Finance
    public DbSet<PaymentPlan> PaymentPlans => Set<PaymentPlan>();
    public DbSet<Installment> Installments => Set<Installment>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentIntent> PaymentIntents => Set<PaymentIntent>();
    public DbSet<Receipt> Receipts => Set<Receipt>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<TrainerPayroll> TrainerPayrolls => Set<TrainerPayroll>();

    // Users and system
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<CertificateTemplate> CertificateTemplates => Set<CertificateTemplate>();
    public DbSet<Setting> Settings => Set<Setting>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Money by default; exchange rates and scores override this in their configurations.
        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        ApplySoftDeleteFilters(modelBuilder);
        ConfigureDeleteBehaviors(modelBuilder);
    }

    /// <summary>Every BaseEntity hides its soft-deleted rows from all queries.</summary>
    private static void ApplySoftDeleteFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                     .Where(t => typeof(BaseEntity).IsAssignableFrom(t.ClrType))
                     .ToList())
        {
            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var isDeleted = Expression.Property(parameter, nameof(BaseEntity.IsDeleted));
            var filter = Expression.Lambda(Expression.Not(isDeleted), parameter);

            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
        }
    }

    /// <summary>
    /// Nothing cascades by default (rows are soft-deleted, and SQL Server rejects overlapping cascade paths).
    /// Only these owned children are removed together with their parent.
    /// </summary>
    private static void ConfigureDeleteBehaviors(ModelBuilder modelBuilder)
    {
        (Type Principal, Type Dependent)[] cascades =
        [
            (typeof(Section), typeof(SectionSchedule)),
            (typeof(Role), typeof(RolePermission)),
            (typeof(Permission), typeof(RolePermission)),
            (typeof(User), typeof(RefreshToken))
        ];

        foreach (var foreignKey in modelBuilder.Model.GetEntityTypes().SelectMany(t => t.GetForeignKeys()))
        {
            var cascade = cascades.Any(c =>
                c.Principal == foreignKey.PrincipalEntityType.ClrType
                && c.Dependent == foreignKey.DeclaringEntityType.ClrType);

            foreignKey.DeleteBehavior = cascade ? DeleteBehavior.Cascade : DeleteBehavior.Restrict;
        }
    }
}
