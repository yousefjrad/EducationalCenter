using EducationalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EducationalCenter.Infrastructure.Persistence.Configurations;

public sealed class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> b)
    {
        b.Property(x => x.FullName).HasMaxLength(150).IsRequired();
        b.Property(x => x.PhoneNumber).HasMaxLength(20).IsRequired();

        b.HasIndex(x => new { x.FullName, x.PhoneNumber });
        b.HasIndex(x => x.PhoneNumber);
    }
}

public sealed class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> b)
    {
        b.HasOne(x => x.Student).WithMany(s => s.Enrollments).HasForeignKey(x => x.StudentId);
        b.HasOne(x => x.Section).WithMany(s => s.Enrollments).HasForeignKey(x => x.SectionId);
        b.HasOne(x => x.TransferredFromEnrollment).WithMany().HasForeignKey(x => x.TransferredFromEnrollmentId);

        b.HasIndex(x => new { x.SectionId, x.Status });
        b.HasIndex(x => new { x.StudentId, x.SectionId });
        b.HasIndex(x => new { x.Status, x.HoldExpiresAt });
    }
}

public sealed class WaitingListEntryConfiguration : IEntityTypeConfiguration<WaitingListEntry>
{
    public void Configure(EntityTypeBuilder<WaitingListEntry> b)
    {
        b.HasOne(x => x.Student).WithMany(s => s.WaitingListEntries).HasForeignKey(x => x.StudentId);
        b.HasOne(x => x.Section).WithMany(s => s.WaitingList).HasForeignKey(x => x.SectionId);
        b.HasOne(x => x.PromotedEnrollment).WithMany().HasForeignKey(x => x.PromotedEnrollmentId);

        b.HasIndex(x => new { x.SectionId, x.Status, x.Position });
        b.HasIndex(x => new { x.StudentId, x.SectionId });
    }
}

public sealed class AttendanceConfiguration : IEntityTypeConfiguration<Attendance>
{
    public void Configure(EntityTypeBuilder<Attendance> b)
    {
        b.HasOne(x => x.ClassSession).WithMany(s => s.Attendances).HasForeignKey(x => x.ClassSessionId);
        b.HasOne(x => x.Enrollment).WithMany(e => e.Attendances).HasForeignKey(x => x.EnrollmentId);
        b.HasOne(x => x.RecordedByUser).WithMany().HasForeignKey(x => x.RecordedByUserId);

        b.HasIndex(x => new { x.ClassSessionId, x.EnrollmentId }).IsUnique().HasFilter(Db.NotDeleted);
        b.HasIndex(x => x.EnrollmentId);
    }
}

public sealed class GradeConfiguration : IEntityTypeConfiguration<Grade>
{
    public void Configure(EntityTypeBuilder<Grade> b)
    {
        b.Property(x => x.Score).HasPrecision(7, 2);
        b.Property(x => x.MaxScore).HasPrecision(7, 2);

        b.HasOne(x => x.Enrollment).WithOne(e => e.Grade).HasForeignKey<Grade>(x => x.EnrollmentId);
        b.HasOne(x => x.RecordedByUser).WithMany().HasForeignKey(x => x.RecordedByUserId);

        b.HasIndex(x => x.EnrollmentId).IsUnique().HasFilter(Db.NotDeleted);
    }
}

public sealed class CertificateConfiguration : IEntityTypeConfiguration<Certificate>
{
    public void Configure(EntityTypeBuilder<Certificate> b)
    {
        b.Property(x => x.CertificateNumber).HasMaxLength(30).IsRequired();
        b.Property(x => x.OverrideReason).HasMaxLength(500);

        b.HasOne(x => x.Enrollment).WithOne(e => e.Certificate).HasForeignKey<Certificate>(x => x.EnrollmentId);
        b.HasOne(x => x.IssuedByUser).WithMany().HasForeignKey(x => x.IssuedByUserId);

        // Numbers are never reused, not even after a soft delete, so this index has no filter.
        b.HasIndex(x => x.CertificateNumber).IsUnique();
        b.HasIndex(x => x.EnrollmentId).IsUnique().HasFilter(Db.NotDeleted);
    }
}
