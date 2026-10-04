using EducationalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EducationalCenter.Infrastructure.Persistence.Configurations;

public sealed class PaymentPlanConfiguration : IEntityTypeConfiguration<PaymentPlan>
{
    public void Configure(EntityTypeBuilder<PaymentPlan> b)
    {
        b.HasOne(x => x.Enrollment).WithOne(e => e.PaymentPlan).HasForeignKey<PaymentPlan>(x => x.EnrollmentId);

        b.HasIndex(x => x.EnrollmentId).IsUnique().HasFilter(Db.NotDeleted);
        b.HasIndex(x => x.Status);
    }
}

public sealed class InstallmentConfiguration : IEntityTypeConfiguration<Installment>
{
    public void Configure(EntityTypeBuilder<Installment> b)
    {
        b.HasOne(x => x.PaymentPlan).WithMany(p => p.Installments).HasForeignKey(x => x.PaymentPlanId);

        b.HasIndex(x => new { x.PaymentPlanId, x.Number }).IsUnique().HasFilter(Db.NotDeleted);
        b.HasIndex(x => new { x.Status, x.DueDate });
    }
}

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> b)
    {
        b.Property(x => x.ExchangeRate).HasPrecision(18, 4);
        b.Property(x => x.CancelReason).HasMaxLength(500);

        b.HasOne(x => x.Installment).WithMany(i => i.Payments).HasForeignKey(x => x.InstallmentId);
        b.HasOne(x => x.ReceivedByUser).WithMany().HasForeignKey(x => x.ReceivedByUserId);

        b.HasIndex(x => x.InstallmentId);
        b.HasIndex(x => x.PaidAt);
        b.HasIndex(x => x.Status);
    }
}

public sealed class ReceiptConfiguration : IEntityTypeConfiguration<Receipt>
{
    public void Configure(EntityTypeBuilder<Receipt> b)
    {
        b.Property(x => x.ReceiptNumber).HasMaxLength(30).IsRequired();

        b.HasOne(x => x.Payment).WithOne(p => p.Receipt).HasForeignKey<Receipt>(x => x.PaymentId);
        b.HasOne(x => x.IssuedByUser).WithMany().HasForeignKey(x => x.IssuedByUserId);

        // Sequential and never reused, so no soft-delete filter on the unique indexes.
        b.HasIndex(x => x.ReceiptNumber).IsUnique();
        b.HasIndex(x => x.PaymentId).IsUnique();
    }
}

public sealed class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> b)
    {
        b.Property(x => x.Category).HasMaxLength(100).IsRequired();
        b.Property(x => x.Description).HasMaxLength(500).IsRequired();
        b.Property(x => x.ExchangeRate).HasPrecision(18, 4);

        b.HasOne(x => x.RecordedByUser).WithMany().HasForeignKey(x => x.RecordedByUserId);

        b.HasIndex(x => x.ExpenseDate);
        b.HasIndex(x => x.Category);
    }
}

public sealed class TrainerPayrollConfiguration : IEntityTypeConfiguration<TrainerPayroll>
{
    public void Configure(EntityTypeBuilder<TrainerPayroll> b)
    {
        b.Property(x => x.CalculationMethod).HasMaxLength(500).IsRequired();
        b.Property(x => x.ExchangeRate).HasPrecision(18, 4);

        b.HasOne(x => x.Trainer).WithMany(t => t.Payrolls).HasForeignKey(x => x.TrainerId);
        b.HasOne(x => x.PaidByUser).WithMany().HasForeignKey(x => x.PaidByUserId);

        b.HasIndex(x => new { x.TrainerId, x.PeriodStart, x.PeriodEnd });
        b.HasIndex(x => x.Status);
    }
}
