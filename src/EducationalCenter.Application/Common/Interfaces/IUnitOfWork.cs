using EducationalCenter.Application.Common.Interfaces.Repositories;

namespace EducationalCenter.Application.Common.Interfaces;

public interface IUnitOfWork
{
    ICourseRepository Courses { get; }
    IRoomRepository Rooms { get; }
    ITrainerRepository Trainers { get; }
    IStudentRepository Students { get; }
    ISectionRepository Sections { get; }
    IClassSessionRepository ClassSessions { get; }
    IEnrollmentRepository Enrollments { get; }
    IWaitingListRepository WaitingList { get; }
    // More repositories are added here as later features need them.

    Task<int> SaveChangesAsync(CancellationToken ct = default);

    /// <summary>Runs the action in one DB transaction (payments, enrollment...). Rolls back on exception.</summary>
    Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken ct = default);

    Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action, CancellationToken ct = default);
}
