using EducationalCenter.Application.Common.Models;

namespace EducationalCenter.Application.Features.Imports;

internal static class ImportTemplates
{
    private static string L(string lang, string ar, string en) => lang == "ar" ? ar : en;

    private static ImportTemplateColumn C(string lang, string name, bool required, string ar, string en) =>
        new(name, required, L(lang, ar, en));

    public static ImportTemplate Students(string lang) => new(
        L(lang, "استيراد الطلاب", "Import students"),
        lang,
        new[]
        {
            C(lang, "FullName", true, "الاسم الكامل للطالب", "The student's full name"),
            C(lang, "PhoneNumber", true, "رقم الهاتف (6 إلى 20 رقماً، قد يبدأ بـ +)", "Phone number (6 to 20 digits, may start with +)")
        },
        new[] { new[] { "Ahmad Ali", "0991234567" } },
        new[]
        {
            L(lang, "اكتب بياناتك في ورقة Data فقط.", "Enter your data on the Data sheet only."),
            L(lang, "الطالب المكرر (الاسم والهاتف معاً) يُرفض.", "A duplicate student (same name and phone) is rejected."),
            L(lang, "إن وُجد خطأ واحد لا يُحفظ شيء من الملف.", "If there is a single error, nothing from the file is saved.")
        });

    public static ImportTemplate Courses(string lang) => new(
        L(lang, "استيراد المواد", "Import courses"),
        lang,
        new[]
        {
            C(lang, "Code", true, "رمز المادة، فريد (حروف وأرقام و - و _)", "Unique course code (letters, digits, - and _)"),
            C(lang, "Name", true, "اسم المادة", "Course name"),
            C(lang, "Description", false, "وصف اختياري", "Optional description"),
            C(lang, "Level", false, "المستوى (اختياري)", "Level (optional)"),
            C(lang, "DefaultDurationHours", true, "عدد الساعات (رقم صحيح)", "Duration in hours (whole number)"),
            C(lang, "DefaultPrice", true, "السعر الافتراضي بالليرة السورية", "Default price in SYP")
        },
        new[] { new[] { "ENG-101", "English Level 1", "", "Beginner", "40", "400000" } },
        new[]
        {
            L(lang, "اكتب بياناتك في ورقة Data فقط.", "Enter your data on the Data sheet only."),
            L(lang, "الرمز يُحفظ بأحرف كبيرة ويجب ألا يكون مستخدماً.", "The code is saved in upper case and must not already exist.")
        });

    public static ImportTemplate Sections(string lang) => new(
        L(lang, "استيراد الشعب", "Import sections"),
        lang,
        new[]
        {
            C(lang, "CourseCode", true, "رمز مادة موجودة مسبقاً", "Code of an existing course"),
            C(lang, "SectionName", true, "اسم الشعبة، فريد داخل المادة", "Section name, unique inside the course"),
            C(lang, "TrainerName", true, "الاسم الكامل لمدرب فعّال موجود مسبقاً", "Full name of an existing active trainer"),
            C(lang, "RoomName", true, "اسم قاعة فعّالة موجودة مسبقاً", "Name of an existing active room"),
            C(lang, "StartDate", true, "تاريخ البداية yyyy-MM-dd", "Start date yyyy-MM-dd"),
            C(lang, "EndDate", true, "تاريخ النهاية yyyy-MM-dd", "End date yyyy-MM-dd"),
            C(lang, "Price", false, "السعر، وإن تُرك فارغاً يؤخذ سعر المادة", "Price; if empty the course price is used"),
            C(lang, "Capacity", true, "السعة (لا تتجاوز سعة القاعة)", "Capacity (not above the room capacity)"),
            C(lang, "MinStudents", false, "الحد الأدنى للطلاب، والافتراضي 0", "Minimum students, default 0"),
            C(lang, "Schedule", true, "الجدول الأسبوعي، مثال: Sat 10:00-12:00; Mon 10:00-12:00", "Weekly schedule, e.g. Sat 10:00-12:00; Mon 10:00-12:00")
        },
        new[] { new[] { "ENG-101", "Morning A", "Sara Khaled", "Room 1", "2026-11-01", "2026-12-20", "", "15", "5", "Sat 10:00-12:00; Mon 10:00-12:00" } },
        new[]
        {
            L(lang, "الشعب تُنشأ كمسودة، ولا تُولَّد جلساتها تلقائياً.", "Sections are created as drafts; their sessions are not generated automatically."),
            L(lang, "أيام الأسبوع بالإنجليزية: Sun Mon Tue Wed Thu Fri Sat.", "Weekdays: Sun Mon Tue Wed Thu Fri Sat."),
            L(lang, "استورد المواد والمدربين والقاعات أولاً.", "Import courses, trainers and rooms first.")
        });

    public static ImportTemplate LegacyPayments(string lang) => new(
        L(lang, "استيراد الدفعات القديمة", "Import legacy payments"),
        lang,
        new[]
        {
            C(lang, "StudentName", true, "اسم طالب موجود مسبقاً", "Name of an existing student"),
            C(lang, "StudentPhone", true, "هاتف الطالب نفسه", "The same student's phone"),
            C(lang, "CourseCode", true, "رمز مادة موجودة", "Code of an existing course"),
            C(lang, "SectionName", true, "اسم شعبة موجودة في المادة", "Name of an existing section of the course"),
            C(lang, "PaidAt", true, "تاريخ الدفع القديم yyyy-MM-dd", "Original payment date yyyy-MM-dd"),
            C(lang, "Currency", true, "SYP أو USD", "SYP or USD"),
            C(lang, "AmountPaid", true, "المبلغ المدفوع (الليرة أعداد صحيحة)", "Amount paid (SYP whole numbers)"),
            C(lang, "ExchangeRate", false, "سعر الصرف لكل دولار، مطلوب للدولار فقط", "SYP per USD, required for USD only")
        },
        new[] { new[] { "Ahmad Ali", "0991234567", "ENG-101", "Morning A", "2026-09-10", "SYP", "200000", "" } },
        new[]
        {
            L(lang, "إن لم يكن للطالب تسجيل في الشعبة يُنشأ تسجيل له وخطة بدفعة واحدة بسعر الشعبة.", "If the student has no enrollment in the section, one is created with a single-installment plan at the section price."),
            L(lang, "تُطبَّق كل دفعة على أول دفعة غير مسدَّدة، ولا يجوز أن تتجاوز المتبقي منها.", "Each payment goes to the first unpaid installment and cannot exceed what remains of it."),
            L(lang, "لا تُصدر إيصالات للدفعات القديمة.", "No receipts are issued for legacy payments."),
            L(lang, "لا تستورد الملف نفسه مرتين، فالدفعات ستُسجَّل مرتين.", "Do not import the same file twice: the payments would be recorded twice.")
        });
}
