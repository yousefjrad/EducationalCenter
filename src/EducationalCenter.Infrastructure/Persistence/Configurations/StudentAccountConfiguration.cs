using EducationalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EducationalCenter.Infrastructure.Persistence.Configurations;

/// <summary>Links a student to the optional sign-in account. At most one student per account.</summary>
public sealed class StudentAccountConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .IsRequired(false);

        builder.HasIndex(s => s.UserId)
            .IsUnique()
            .HasFilter("[UserId] IS NOT NULL");
    }
}