using EducationalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EducationalCenter.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.Property(x => x.FullName).HasMaxLength(150).IsRequired();
        b.Property(x => x.Email).HasMaxLength(200).IsRequired();
        b.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();

        b.HasOne(x => x.Role).WithMany(r => r.Users).HasForeignKey(x => x.RoleId);

        b.HasIndex(x => x.Email).IsUnique().HasFilter(Db.NotDeleted);
        b.HasIndex(x => x.RoleId);
    }
}

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        // SHA-256 as hex text is always 64 characters.
        b.Property(x => x.TokenHash).HasMaxLength(64).IsRequired();

        b.HasOne(x => x.User).WithMany(u => u.RefreshTokens).HasForeignKey(x => x.UserId);

        b.HasIndex(x => x.TokenHash).IsUnique();
        b.HasIndex(x => x.UserId);
    }
}

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> b)
    {
        b.Property(x => x.Name).HasMaxLength(50).IsRequired();
        b.Property(x => x.Description).HasMaxLength(200);

        b.HasIndex(x => x.Name).IsUnique().HasFilter(Db.NotDeleted);
    }
}

public sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> b)
    {
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();

        b.HasIndex(x => x.Name).IsUnique();
    }
}

public sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> b)
    {
        b.HasKey(x => new { x.RoleId, x.PermissionId });

        b.HasOne(x => x.Role).WithMany(r => r.RolePermissions).HasForeignKey(x => x.RoleId);
        b.HasOne(x => x.Permission).WithMany(p => p.RolePermissions).HasForeignKey(x => x.PermissionId);

        // Not a BaseEntity, so it follows its role: a soft-deleted role grants nothing.
        b.HasQueryFilter(x => !x.Role.IsDeleted);
    }
}

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.Property(x => x.Action).HasMaxLength(100).IsRequired();
        b.Property(x => x.EntityName).HasMaxLength(100).IsRequired();
        b.Property(x => x.OldValues).HasColumnType("nvarchar(max)");
        b.Property(x => x.NewValues).HasColumnType("nvarchar(max)");
        b.Property(x => x.Reason).HasMaxLength(500);

        b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);

        b.HasIndex(x => new { x.EntityName, x.EntityId });
        b.HasIndex(x => x.CreatedAt);
        b.HasIndex(x => x.UserId);
    }
}

public sealed class CertificateTemplateConfiguration : IEntityTypeConfiguration<CertificateTemplate>
{
    public void Configure(EntityTypeBuilder<CertificateTemplate> b)
    {
        b.Property(x => x.CenterName).HasMaxLength(150).IsRequired();
        b.Property(x => x.LogoPath).HasMaxLength(500);
        b.Property(x => x.SignerName).HasMaxLength(150).IsRequired();
        b.Property(x => x.SignerTitle).HasMaxLength(100).IsRequired();
        b.Property(x => x.BodyText).HasMaxLength(2000).IsRequired();
        b.Property(x => x.Language).HasMaxLength(2).IsRequired();

        b.HasIndex(x => new { x.Language, x.IsDefault });
    }
}

public sealed class SettingConfiguration : IEntityTypeConfiguration<Setting>
{
    public void Configure(EntityTypeBuilder<Setting> b)
    {
        b.Property(x => x.Key).HasMaxLength(100).IsRequired();
        b.Property(x => x.Value).HasMaxLength(200).IsRequired();
        b.Property(x => x.Description).HasMaxLength(300);

        b.HasOne(x => x.UpdatedByUser).WithMany().HasForeignKey(x => x.UpdatedByUserId);

        b.HasIndex(x => x.Key).IsUnique();
    }
}
