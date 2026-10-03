using EducationalCenter.Application.Features.ClassSessions;
using EducationalCenter.Application.Features.Courses;
using EducationalCenter.Application.Features.Enrollments;
using EducationalCenter.Application.Features.Rooms;
using EducationalCenter.Application.Features.Sections;
using EducationalCenter.Application.Features.Students;
using EducationalCenter.Application.Features.Trainers;
using EducationalCenter.Application.Features.WaitingList;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace EducationalCenter.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Registers every AbstractValidator<T> in this assembly.
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddScoped<ICourseService, CourseService>();
        services.AddScoped<IRoomService, RoomService>();
        services.AddScoped<ITrainerService, TrainerService>();
        services.AddScoped<IStudentService, StudentService>();
        services.AddScoped<ISectionService, SectionService>();
        services.AddScoped<IClassSessionService, ClassSessionService>();
        services.AddScoped<IEnrollmentService, EnrollmentService>();
        services.AddScoped<IWaitingListService, WaitingListService>();
        // Later features are registered here, following the same pattern.

        return services;
    }
}
