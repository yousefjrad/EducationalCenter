using EducationalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EducationalCenter.Infrastructure.Persistence.Configurations;

public sealed class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> b)
    {
        b.Property(x => x.Code).HasMaxLength(20).IsRequired();
        b.Property(x => x.Name).HasMaxLength(150).IsRequired();
        b.Property(x => x.Description).HasMaxLength(1000);
        b.Property(x => x.Level).HasMaxLength(50);

        b.HasIndex(x => x.Code).IsUnique().HasFilter(Db.NotDeleted);
    }
}

public sealed class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> b)
    {
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.Property(x => x.Description).HasMaxLength(500);
        b.Property(x => x.Location).HasMaxLength(200);

        b.HasIndex(x => x.Name).IsUnique().HasFilter(Db.NotDeleted);
    }
}

public sealed class TrainerConfiguration : IEntityTypeConfiguration<Trainer>
{
    public void Configure(EntityTypeBuilder<Trainer> b)
    {
        b.Property(x => x.FullName).HasMaxLength(150).IsRequired();
        b.Property(x => x.PhoneNumber).HasMaxLength(20).IsRequired();
        b.Property(x => x.Specialty).HasMaxLength(100);

        b.HasIndex(x => x.FullName);
    }
}

public sealed class SectionConfiguration : IEntityTypeConfiguration<Section>
{
    public void Configure(EntityTypeBuilder<Section> b)
    {
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();

        b.HasOne(x => x.Course).WithMany(c => c.Sections).HasForeignKey(x => x.CourseId);
        b.HasOne(x => x.Trainer).WithMany(t => t.Sections).HasForeignKey(x => x.TrainerId);
        b.HasOne(x => x.Room).WithMany(r => r.Sections).HasForeignKey(x => x.RoomId);

        b.HasIndex(x => new { x.CourseId, x.Name }).IsUnique().HasFilter(Db.NotDeleted);
        b.HasIndex(x => x.TrainerId);
        b.HasIndex(x => x.RoomId);
        b.HasIndex(x => x.Status);
    }
}

public sealed class SectionScheduleConfiguration : IEntityTypeConfiguration<SectionSchedule>
{
    public void Configure(EntityTypeBuilder<SectionSchedule> b)
    {
        b.HasOne(x => x.Section).WithMany(s => s.Schedules).HasForeignKey(x => x.SectionId);
        b.HasIndex(x => x.SectionId);

        // Not a BaseEntity, so it follows its parent: a soft-deleted section hides its schedule.
        b.HasQueryFilter(x => !x.Section.IsDeleted);
    }
}

public sealed class ClassSessionConfiguration : IEntityTypeConfiguration<ClassSession>
{
    public void Configure(EntityTypeBuilder<ClassSession> b)
    {
        // Cancel and postpone append short lines here, so it needs room.
        b.Property(x => x.Notes).HasMaxLength(2000);

        b.HasOne(x => x.Section).WithMany(s => s.Sessions).HasForeignKey(x => x.SectionId);
        b.HasOne(x => x.Room).WithMany(r => r.ClassSessions).HasForeignKey(x => x.RoomId);
        b.HasOne(x => x.Trainer).WithMany(t => t.ClassSessions).HasForeignKey(x => x.TrainerId);

        // Conflict checks and schedule reports filter by room/trainer and date.
        b.HasIndex(x => new { x.RoomId, x.Date });
        b.HasIndex(x => new { x.TrainerId, x.Date });
        b.HasIndex(x => new { x.SectionId, x.Date });
    }
}
