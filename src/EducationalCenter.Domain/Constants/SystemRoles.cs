namespace EducationalCenter.Domain.Constants;

public static class SystemRoles
{
    public const string Admin = "Admin";
    public const string Receptionist = "Receptionist";
    public const string Student = "Student";
    public const string BranchManager = "BranchManager";

    /// <summary>Admin gets everything.</summary>
    public static IReadOnlyList<string> AdminPermissions => Permissions.All;

    /// <summary>
    /// Receptionist: students, enrollment, sessions, attendance, grades, issuing certificates,
    /// payment plans, recording payments, receipts, operational report, import.
    /// Admin only: cancelling payments, expenses, payroll, financial reports, users, roles, settings.
    /// </summary>
    public static IReadOnlyList<string> ReceptionistPermissions { get; } =
    [
        Permissions.Courses.View,
        Permissions.Rooms.View,
        Permissions.Trainers.View,
        Permissions.Sections.View,

        Permissions.Students.View, Permissions.Students.Create, Permissions.Students.Update, Permissions.Students.Delete,

        Permissions.ClassSessions.View, Permissions.ClassSessions.Create, Permissions.ClassSessions.Update, Permissions.ClassSessions.Delete,

        Permissions.Enrollments.View, Permissions.Enrollments.Create, Permissions.Enrollments.Confirm,
        Permissions.Enrollments.Cancel, Permissions.Enrollments.Transfer,

        Permissions.WaitingList.View, Permissions.WaitingList.Manage,

        Permissions.Attendance.View, Permissions.Attendance.Record,
        Permissions.Grades.View, Permissions.Grades.Record,
        Permissions.Certificates.View, Permissions.Certificates.Issue,

        Permissions.PaymentPlans.View, Permissions.PaymentPlans.Create,
        Permissions.Payments.View, Permissions.Payments.Create,
        Permissions.Receipts.View,

        Permissions.Reports.Operational,
        Permissions.Import.Run,
    ];

    /// <summary>
    /// Branch manager: the receptionist's daily work plus expenses, rooms, sections, budget requests and viewing their own branch.
    /// No pricing, no users, no cross-branch reports, no budget approval, no student transfer between branches.
    /// </summary>
    public static IReadOnlyList<string> BranchManagerPermissions { get; } =
    [
        .. ReceptionistPermissions,
        Permissions.Expenses.View, Permissions.Expenses.Create, Permissions.Expenses.Update,
        Permissions.Rooms.Create, Permissions.Rooms.Update,
        Permissions.Sections.Create, Permissions.Sections.Update,
        Permissions.Budgets.View, Permissions.Budgets.Request,
        Permissions.Courses.Create, Permissions.Courses.Update,
        Permissions.Trainers.Create, Permissions.Trainers.Update,
        Permissions.Branches.View,
    ];
}
