using EducationalCenter.Application.Common.Interfaces;
using EducationalCenter.Application.Common.Models;
using EducationalCenter.Application.Common.Money;
using EducationalCenter.Application.Features.PaymentPlans;
using EducationalCenter.Domain.Entities;
using EducationalCenter.Domain.Enums;

namespace EducationalCenter.Application.Features.Imports;

/// <remarks>
/// Rows are processed in file order, because later rows of the same student and section build on earlier ones.
/// Missing enrollments are created (Confirmed, or Completed for a completed section) with a one-installment
/// plan at the section price. Every payment goes to the first installment that still has a balance and may not
/// exceed it. No receipts are issued; each payment is written to the audit log.
/// </remarks>
public sealed class LegacyPaymentImporter(
    IUnitOfWork uow,
    IClock clock,
    ICurrentUser currentUser,
    IAuditLogger audit) : IImporter
{
    public ImportKind Kind => ImportKind.LegacyPayments;

    public IReadOnlyList<string> Columns { get; } =
    [
        "StudentName", "StudentPhone", "CourseCode", "SectionName", "PaidAt", "Currency", "AmountPaid", "ExchangeRate"
    ];

    public ImportTemplate GetTemplate(string language) => ImportTemplates.LegacyPayments(language);

    public async Task<ImportOutcome> RunAsync(ImportSheet sheet, bool commit, CancellationToken ct = default)
    {
        var userId = currentUser.RequireUserId();
        var latestAllowed = DateOnly.FromDateTime(clock.UtcNow).AddDays(1);

        var errors = new ImportErrorCollector();
        var students = new Dictionary<string, Student?>(StringComparer.OrdinalIgnoreCase);
        var courses = new Dictionary<string, Course?>(StringComparer.OrdinalIgnoreCase);
        var sections = new Dictionary<string, Section?>(StringComparer.OrdinalIgnoreCase);
        var enrollments = new Dictionary<(int StudentId, int SectionId), Enrollment>();
        var newEnrollments = new List<Enrollment>();
        var newPayments = new List<Payment>();

        foreach (var row in sheet.Rows)
        {
            var before = errors.Count;

            // ----- parse the row -----
            var studentName = row.Get("StudentName");
            var studentPhone = row.Get("StudentPhone");
            var courseCode = row.Get("CourseCode").ToUpperInvariant();
            var sectionName = row.Get("SectionName");

            if (studentName.Length == 0) errors.Add(row.RowNumber, "StudentName", "Required.");
            if (studentPhone.Length == 0) errors.Add(row.RowNumber, "StudentPhone", "Required.");
            if (courseCode.Length == 0) errors.Add(row.RowNumber, "CourseCode", "Required.");
            if (sectionName.Length == 0) errors.Add(row.RowNumber, "SectionName", "Required.");

            if (!ImportParsing.TryParseDate(row.Get("PaidAt"), out var paidDate))
                errors.Add(row.RowNumber, "PaidAt", "Must be a date like 2026-09-10.");
            else if (paidDate > latestAllowed)
                errors.Add(row.RowNumber, "PaidAt", "The payment date cannot be in the future.");

            Currency currency = default;
            var currencyText = row.Get("Currency").ToUpperInvariant();
            if (currencyText == "SYP") currency = Currency.Syp;
            else if (currencyText == "USD") currency = Currency.Usd;
            else errors.Add(row.RowNumber, "Currency", "Must be SYP or USD.");

            if (!ImportParsing.TryParseDecimal(row.Get("AmountPaid"), out var amount) || amount <= 0m)
                errors.Add(row.RowNumber, "AmountPaid", "Must be a number greater than zero.");
            else if (currencyText == "SYP" && amount != Math.Truncate(amount))
                errors.Add(row.RowNumber, "AmountPaid", "Amounts in SYP must be whole numbers.");
            else if (currencyText == "USD" && Math.Round(amount, 2) != amount)
                errors.Add(row.RowNumber, "AmountPaid", "Amounts in USD can have at most two decimals.");

            decimal? rate = null;
            var rateText = row.Get("ExchangeRate");
            if (currencyText == "USD")
            {
                if (!ImportParsing.TryParseDecimal(rateText, out var parsedRate) || parsedRate <= 0m || parsedRate > 1_000_000m)
                    errors.Add(row.RowNumber, "ExchangeRate", "Required for USD: a number between 0 and 1,000,000.");
                else
                    rate = parsedRate;
            }
            else if (currencyText == "SYP" && rateText.Length > 0)
            {
                errors.Add(row.RowNumber, "ExchangeRate", "Must be empty for SYP payments.");
            }

            if (errors.Count > before)
                continue;

            // ----- find the student and the section -----
            var studentKey = $"{studentName}|{studentPhone}";
            if (!students.TryGetValue(studentKey, out var student))
            {
                student = await uow.Students.FindByNameAndPhoneAsync(studentName, studentPhone, ct);
                students[studentKey] = student;
            }
            if (student is null)
            {
                errors.Add(row.RowNumber, "StudentName", "No student with this name and phone. Import the students first.");
                continue;
            }

            if (!courses.TryGetValue(courseCode, out var course))
            {
                course = await uow.Courses.GetByCodeAsync(courseCode, ct);
                courses[courseCode] = course;
            }
            if (course is null)
            {
                errors.Add(row.RowNumber, "CourseCode", $"No course with the code '{courseCode}'.");
                continue;
            }

            var sectionKey = $"{course.Id}|{sectionName}";
            if (!sections.TryGetValue(sectionKey, out var section))
            {
                section = await uow.Sections.GetByNameAsync(course.Id, sectionName, ct);
                sections[sectionKey] = section;
            }
            if (section is null)
            {
                errors.Add(row.RowNumber, "SectionName", $"The course has no section named '{sectionName}'.");
                continue;
            }
            if (section.Status == SectionStatus.Cancelled)
            {
                errors.Add(row.RowNumber, "SectionName", "The section is cancelled.");
                continue;
            }

            // ----- enrollment and plan -----
            var paidAtUtc = paidDate.ToDateTime(new TimeOnly(12, 0), DateTimeKind.Utc);
            var enrollmentKey = (student.Id, section.Id);

            if (!enrollments.TryGetValue(enrollmentKey, out var enrollment))
            {
                var existing = await uow.Enrollments.GetForLegacyImportAsync(student.Id, section.Id, ct);

                if (existing is null)
                {
                    if (section.Price <= 0m)
                    {
                        errors.Add(row.RowNumber, "SectionName", "The section price is zero, so it cannot have payments.");
                        continue;
                    }

                    existing = new Enrollment
                    {
                        StudentId = student.Id,
                        SectionId = section.Id,
                        AgreedPrice = section.Price,
                        Status = section.Status == SectionStatus.Completed ? EnrollmentStatus.Completed : EnrollmentStatus.Confirmed,
                        EnrolledAt = paidAtUtc
                    };

                    var newPlan = new PaymentPlan
                    {
                        Enrollment = existing,
                        TotalAmount = section.Price,
                        InstallmentsCount = 1,
                        Status = PaymentPlanStatus.Open
                    };
                    newPlan.Installments.Add(new Installment
                    {
                        Number = 1,
                        Amount = section.Price,
                        DueDate = paidDate,
                        Status = InstallmentStatus.Unpaid
                    });
                    existing.PaymentPlan = newPlan;

                    newEnrollments.Add(existing);
                }

                enrollments[enrollmentKey] = existing;
                enrollment = existing;
            }

            var plan = enrollment.PaymentPlan;
            if (plan is null)
            {
                errors.Add(row.RowNumber, "SectionName", "The student's enrollment has no payment plan. Create one first.");
                continue;
            }
            if (plan.Status != PaymentPlanStatus.Open)
            {
                errors.Add(row.RowNumber, "SectionName", "The payment plan is not open for payments.");
                continue;
            }

            var installment = plan.Installments
                .OrderBy(i => i.Number)
                .FirstOrDefault(i => i.Amount - PaymentMath.PaidSyp(i) > 0m);
            if (installment is null)
            {
                errors.Add(row.RowNumber, "AmountPaid", "Every installment of this enrollment is already fully paid.");
                continue;
            }

            var remaining = installment.Amount - PaymentMath.PaidSyp(installment);
            var amountInSyp = CurrencyConversion.ToSyp(currency, amount, rate);

            if (amountInSyp <= 0m)
            {
                errors.Add(row.RowNumber, "AmountPaid", "The amount is too small.");
                continue;
            }
            if (amountInSyp > remaining)
            {
                errors.Add(row.RowNumber, "AmountPaid",
                    $"The payment ({amountInSyp:0.##} SYP) exceeds what remains of installment {installment.Number} ({remaining:0.##} SYP).");
                continue;
            }

            // ----- apply the payment -----
            var payment = new Payment
            {
                Installment = installment,
                Currency = currency,
                AmountPaid = amount,
                ExchangeRate = currency == Currency.Usd ? rate : null,
                AmountInSyp = amountInSyp,
                PaidAt = paidAtUtc,
                ReceivedByUserId = userId,
                Status = PaymentStatus.Valid
            };

            // Adding it to the installment lets later rows of the same file see the new balance.
            installment.Payments.Add(payment);
            installment.Status = PaymentMath.StoredStatus(PaymentMath.PaidSyp(installment), installment.Amount);

            if (plan.Installments.All(i => PaymentMath.PaidSyp(i) >= i.Amount))
                plan.Status = PaymentPlanStatus.FullyPaid;

            newPayments.Add(payment);
        }

        var imported = 0;
        if (commit && errors.Count == 0 && newPayments.Count > 0)
        {
            await uow.ExecuteInTransactionAsync(async () =>
            {
                if (newEnrollments.Count > 0)
                    await uow.Enrollments.AddRangeAsync(newEnrollments, ct);
                await uow.Payments.AddRangeAsync(newPayments, ct);
                await uow.SaveChangesAsync(ct);

                foreach (var payment in newPayments)
                {
                    await audit.LogAsync(
                        "Payment.LegacyImported", nameof(Payment), payment.Id,
                        oldValues: null,
                        newValues: new
                        {
                            payment.InstallmentId,
                            payment.Currency,
                            payment.AmountPaid,
                            payment.ExchangeRate,
                            payment.AmountInSyp,
                            payment.PaidAt
                        },
                        reason: "Legacy import", ct);
                }
                await uow.SaveChangesAsync(ct);
            }, ct);
            imported = newPayments.Count;
        }

        return new ImportOutcome(sheet.Rows.Count, newPayments.Count, imported, errors.Errors, errors.Count);
    }
}
