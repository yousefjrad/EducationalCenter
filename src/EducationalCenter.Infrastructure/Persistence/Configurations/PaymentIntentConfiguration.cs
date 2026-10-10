using EducationalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EducationalCenter.Infrastructure.Persistence.Configurations;

public sealed class PaymentIntentConfiguration : IEntityTypeConfiguration<PaymentIntent>
{
    public void Configure(EntityTypeBuilder<PaymentIntent> builder)
    {
        builder.Property(x => x.Reference).IsRequired().HasMaxLength(40);
        builder.HasIndex(x => x.Reference).IsUnique();
        builder.Property(x => x.Provider).IsRequired().HasMaxLength(30);
        builder.Property(x => x.ProviderReference).HasMaxLength(100);
        builder.Property(x => x.RedirectUrl).HasMaxLength(500);
        builder.Property(x => x.FailureReason).HasMaxLength(500);
        builder.HasIndex(x => new { x.UserId, x.Status });
    }
}