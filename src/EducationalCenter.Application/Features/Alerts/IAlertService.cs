namespace EducationalCenter.Application.Features.Alerts;

public interface IAlertService
{
    /// <summary>
    /// Overdue installments (needs Payments.View), seat holds awaiting confirmation (Enrollments.View) and
    /// waiting-list promotions now possible (WaitingList.View). Each part appears only if the user holds
    /// that permission, so any signed-in user may call this.
    /// </summary>
    Task<AlertsDto> GetAlertsAsync(CancellationToken ct = default);
}
