using System.Reflection;

namespace EducationalCenter.Domain.Constants;

public static class Permissions
{
    public static class Courses { public const string View = "Courses.View"; public const string Create = "Courses.Create"; public const string Update = "Courses.Update"; public const string Delete = "Courses.Delete"; }
    public static class Rooms { public const string View = "Rooms.View"; public const string Create = "Rooms.Create"; public const string Update = "Rooms.Update"; public const string Delete = "Rooms.Delete"; }
    public static class Trainers { public const string View = "Trainers.View"; public const string Create = "Trainers.Create"; public const string Update = "Trainers.Update"; public const string Delete = "Trainers.Delete"; }
    public static class Students { public const string View = "Students.View"; public const string Create = "Students.Create"; public const string Update = "Students.Update"; public const string Delete = "Students.Delete"; }
    public static class Sections { public const string View = "Sections.View"; public const string Create = "Sections.Create"; public const string Update = "Sections.Update"; public const string Delete = "Sections.Delete"; }
    public static class ClassSessions { public const string View = "ClassSessions.View"; public const string Create = "ClassSessions.Create"; public const string Update = "ClassSessions.Update"; public const string Delete = "ClassSessions.Delete"; }
    public static class Enrollments { public const string View = "Enrollments.View"; public const string Create = "Enrollments.Create"; public const string Confirm = "Enrollments.Confirm"; public const string Cancel = "Enrollments.Cancel"; public const string Transfer = "Enrollments.Transfer"; }
    public static class WaitingList { public const string View = "WaitingList.View"; public const string Manage = "WaitingList.Manage"; }
    public static class Attendance { public const string View = "Attendance.View"; public const string Record = "Attendance.Record"; }
    public static class Grades { public const string View = "Grades.View"; public const string Record = "Grades.Record"; }
    public static class Certificates { public const string View = "Certificates.View"; public const string Issue = "Certificates.Issue"; public const string Override = "Certificates.Override"; }
    public static class CertificateTemplates { public const string View = "CertificateTemplates.View"; public const string Manage = "CertificateTemplates.Manage"; }
    public static class PaymentPlans { public const string View = "PaymentPlans.View"; public const string Create = "PaymentPlans.Create"; public const string Cancel = "PaymentPlans.Cancel"; }
    public static class Payments { public const string View = "Payments.View"; public const string Create = "Payments.Create"; public const string Cancel = "Payments.Cancel"; }
    public static class Receipts { public const string View = "Receipts.View"; }
    public static class Expenses { public const string View = "Expenses.View"; public const string Create = "Expenses.Create"; public const string Update = "Expenses.Update"; }
    public static class TrainerPayroll { public const string View = "TrainerPayroll.View"; public const string Calculate = "TrainerPayroll.Calculate"; public const string Pay = "TrainerPayroll.Pay"; }
    public static class Reports { public const string Operational = "Reports.Operational"; public const string Financial = "Reports.Financial"; }
    public static class Import { public const string Run = "Import.Run"; }
    public static class Users { public const string View = "Users.View"; public const string Create = "Users.Create"; public const string Update = "Users.Update"; public const string Deactivate = "Users.Deactivate"; }
    public static class Roles { public const string View = "Roles.View"; public const string Manage = "Roles.Manage"; }
    public static class Settings { public const string View = "Settings.View"; public const string Update = "Settings.Update"; }
    public static class AuditLog { public const string View = "AuditLog.View"; }

    /// <summary>Every permission name defined above (used for seeding and for the Admin role).</summary>
    public static IReadOnlyList<string> All { get; } = typeof(Permissions)
        .GetNestedTypes(BindingFlags.Public)
        .SelectMany(t => t.GetFields(BindingFlags.Public | BindingFlags.Static))
        .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
        .Select(f => (string)f.GetRawConstantValue()!)
        .ToList();
}
