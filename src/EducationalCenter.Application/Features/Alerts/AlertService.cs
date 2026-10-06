using EducationalCenter.Application.Common.Interfaces;
using EducationalCenter.Domain.Constants;

namespace EducationalCenter.Application.Features.Alerts;

public sealed class AlertService(IUnitOfWork uow, IClock clock, ICurrentUser currentUser) : IAlertService
{
    private const int MaxItems = 20;

    public async Task<AlertsDto> GetAlertsAsync(CancellationToken ct = default)
    {
        var userId = currentUser.RequireUserId();
        var granted = (await uow.Users.GetPermissionNamesAsync(userId, ct)).ToHashSet();

        var now = clock.UtcNow;

        OverdueInstallmentsAlert? overdue = null;
        if (granted.Contains(Permissions.Payments.View))
            overdue = await BuildOverdueAsync(DateOnly.FromDateTime(now), ct);

        PendingEnrollmentsAlert? pending = null;
        if (granted.Contains(Permissions.Enrollments.View))
            pending = await BuildPendingAsync(now, ct);

        WaitingListAlert? waiting = null;
        if (granted.Contains(Permissions.WaitingList.View))
        {
            var rows = await uow.Reports.GetPromotableWaitingAsync(now, ct);
            waiting = new WaitingListAlert(rows.Count, rows.Take(MaxItems).ToList());
        }

        var total = (overdue?.Count ?? 0) + (pending?.Count ?? 0) + (waiting?.Count ?? 0);
        return new AlertsDto(overdue, pending, waiting, total);
    }

    private async Task<OverdueInstallmentsAlert> BuildOverdueAsync(DateOnly today, CancellationToken ct)
    {
        var rows = (await uow.Reports.GetOverdueInstallmentsAsync(today, null, ct))
            .Select(r => r with { DaysOverdue = Math.Max(0, today.DayNumber - r.DueDate.DayNumber) })
            .ToList();

        return new OverdueInstallmentsAlert(
            rows.Count,
            rows.Sum(r => r.RemainingInSyp),
            rows.Take(MaxItems).ToList());
    }

    private async Task<PendingEnrollmentsAlert> BuildPendingAsync(DateTime now, CancellationToken ct)
    {
        var enrollments = await uow.Enrollments.GetPendingAsync(ct);

        var items = enrollments
            .Select(e => new PendingEnrollmentAlertItem(
                e.Id,
                e.StudentId,
                e.Student.FullName,
                e.Student.PhoneNumber,
                e.SectionId,
                e.Section.Name,
                e.HoldExpiresAt,
                e.HoldExpiresAt <= now))
            .ToList();

        return new PendingEnrollmentsAlert(
            items.Count,
            items.Count(i => i.IsExpired),
            items.Take(MaxItems).ToList());
    }
}
