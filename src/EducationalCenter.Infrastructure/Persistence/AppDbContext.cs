using System.Linq.Expressions;
using EducationalCenter.Application.Common.Exceptions;
using EducationalCenter.Application.Common.Interfaces;
using EducationalCenter.Domain.Common;
using EducationalCenter.Domain.Entities;
using EducationalCenter.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace EducationalCenter.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options, IBranchContext? branchContext = null) : DbContext(options)
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

    // Branches
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<TrainerBranch> TrainerBranches => Set<TrainerBranch>();
    public DbSet<BranchCourse> BranchCourses => Set<BranchCourse>();
    public DbSet<Budget> Budgets => Set<Budget>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Money by default; exchange rates and scores override this in their configurations.
        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        ConfigureBranches(modelBuilder);

        ApplySoftDeleteFilters(modelBuilder);
        ApplyBranchFilters(modelBuilder);
        ConfigureDeleteBehaviors(modelBuilder);
    }

    private int? CurrentBranchId => branchContext?.BranchId;

    /// <summary>
    /// Branch-owned records are hidden from every other branch, and soft-deleted rows from everyone.
    /// A request with no branch scope (Super Admin viewing all branches, or a background job) sees all branches.
    /// </summary>
    private void ApplyBranchFilters(ModelBuilder modelBuilder)
    {
        var method = typeof(AppDbContext).GetMethod(nameof(ApplyBranchFilter), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;

        foreach (var type in modelBuilder.Model.GetEntityTypes()
                     .Where(t => typeof(BaseEntity).IsAssignableFrom(t.ClrType) && typeof(IBranchOwned).IsAssignableFrom(t.ClrType))
                     .Select(t => t.ClrType)
                     .ToList())
            method.MakeGenericMethod(type).Invoke(this, [modelBuilder]);

        modelBuilder.Entity<AuditLog>().HasQueryFilter(e => CurrentBranchId == null || e.BranchId == CurrentBranchId);
    }

    private void ApplyBranchFilter<T>(ModelBuilder modelBuilder) where T : BaseEntity, IBranchOwned =>
        modelBuilder.Entity<T>().HasQueryFilter(e => !e.IsDeleted && (CurrentBranchId == null || e.BranchId == CurrentBranchId));

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        StampBranch();
        EnsureBranchesOpen();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        StampBranch();
        EnsureBranchesOpen();
        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>
    /// New branch-owned records get the request's branch, never a value the caller sent.
    /// With no scope the branch is taken from a tracked parent (for example the section of a new enrollment), or the save is refused.
    /// </summary>
    private void StampBranch()
    {
        var scoped = branchContext?.BranchId;

        foreach (var entry in ChangeTracker.Entries().Where(e => e.State == EntityState.Added).ToList())
        {
            if (entry.Entity is IBranchOwned owned)
            {
                if (scoped is int own)
                {
                    if (own <= 0)
                        throw new RequestValidationException("branch", "Your account is not assigned to a branch.");

                    owned.BranchId = own;
                }
                else if (owned.BranchId == 0)
                {
                    owned.BranchId = InferBranch(entry)
                        ?? throw new RequestValidationException("branch", "Select a branch first: this record must belong to one branch (send the X-Branch-Id header).");
                }
            }
            else if (entry.Entity is Trainer newTrainer && scoped is > 0)
            {
                TrainerBranches.Add(new TrainerBranch { Trainer = newTrainer, BranchId = scoped.Value });
            }
            else if (entry.Entity is Course newCourse && scoped is > 0)
            {
                BranchCourses.Add(new BranchCourse { Course = newCourse, BranchId = scoped.Value, IsActive = true });
            }
            else if (entry.Entity is AuditLog log && log.BranchId is null && scoped is > 0)
            {
                log.BranchId = scoped;
            }
        }
    }

    /// <summary>A closed branch keeps its data for reading but accepts no new records.</summary>
    private void EnsureBranchesOpen()
    {
        var ids = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added && e.Entity is IBranchOwned)
            .Select(e => ((IBranchOwned)e.Entity).BranchId)
            .Distinct()
            .ToList();

        if (ids.Count == 0)
            return;

        if (Branches.AsNoTracking().Any(b => ids.Contains(b.Id) && !b.IsActive))
            throw new ConflictException("This branch is closed and accepts no new records.");
    }

    private int? InferBranch(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        foreach (var foreignKey in entry.Metadata.GetForeignKeys())
        {
            if (!typeof(IBranchOwned).IsAssignableFrom(foreignKey.PrincipalEntityType.ClrType))
                continue;

            var value = entry.Property(foreignKey.Properties[0].Name).CurrentValue;
            if (value is null)
                continue;

            var keyName = foreignKey.PrincipalKey.Properties[0].Name;
            var parent = ChangeTracker.Entries().FirstOrDefault(e =>
                e.Metadata == foreignKey.PrincipalEntityType && Equals(e.Property(keyName).CurrentValue, value));

            if (parent?.Entity is IBranchOwned owner && owner.BranchId != 0)
                return owner.BranchId;
        }

        return null;
    }
    /// <summary>Branches, their links, budgets, and a foreign key from every branch-owned record to its branch.</summary>
    private static void ConfigureBranches(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Branch>(b =>
        {
            b.Property(x => x.Name).HasMaxLength(100).IsRequired();
            b.Property(x => x.Code).HasMaxLength(5).IsRequired();
            b.Property(x => x.Governorate).HasMaxLength(100);
            b.Property(x => x.Address).HasMaxLength(250);
            b.Property(x => x.Phone).HasMaxLength(30);
            b.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<TrainerBranch>(b =>
        {
            b.HasKey(x => new { x.TrainerId, x.BranchId });
            b.HasOne(x => x.Trainer).WithMany().HasForeignKey(x => x.TrainerId);
            b.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId);
        });

        modelBuilder.Entity<BranchCourse>(b =>
        {
            b.HasKey(x => new { x.CourseId, x.BranchId });
            b.HasOne(x => x.Course).WithMany().HasForeignKey(x => x.CourseId);
            b.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId);
        });

        modelBuilder.Entity<Budget>(b =>
        {
            b.Property(x => x.Notes).HasMaxLength(500);
            b.HasIndex(x => new { x.BranchId, x.Month }).IsUnique().HasFilter("[IsDeleted] = 0");
            b.HasOne(x => x.RequestedByUser).WithMany().HasForeignKey(x => x.RequestedByUserId);
            b.HasOne(x => x.DecidedByUser).WithMany().HasForeignKey(x => x.DecidedByUserId);
        });

        foreach (var type in modelBuilder.Model.GetEntityTypes()
                     .Where(t => typeof(IBranchOwned).IsAssignableFrom(t.ClrType) && t.ClrType != typeof(Budget))
                     .Select(t => t.ClrType)
                     .ToList())
        {
            var builder = modelBuilder.Entity(type);
            builder.HasOne(typeof(Branch)).WithMany().HasForeignKey("BranchId").OnDelete(DeleteBehavior.Restrict);
            builder.HasIndex("BranchId");
        }

        modelBuilder.Entity<User>().HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<AuditLog>().HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
    /// <summary>Every BaseEntity hides its soft-deleted rows from all queries.</summary>
    private void ApplySoftDeleteFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                     .Where(t => typeof(BaseEntity).IsAssignableFrom(t.ClrType) && !typeof(IBranchOwned).IsAssignableFrom(t.ClrType))
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
