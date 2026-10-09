using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace EducationalCenter.Infrastructure.Documents;

internal static class PdfTheme
{
    public static readonly Color Navy = Color.FromHex("#1F3A5F");
    public static readonly Color Gold = Color.FromHex("#B8923A");
    public static readonly Color Ink = Color.FromHex("#1F2933");
    public static readonly Color Muted = Color.FromHex("#6B7280");
    public static readonly Color Line = Color.FromHex("#D9DEE7");
    public static readonly Color Soft = Color.FromHex("#F3F5F9");
    public static readonly Color White = Color.FromHex("#FFFFFF");
    public static readonly Color Danger = Color.FromHex("#B42318");

    public static IContainer BodyCell(IContainer cell, bool alt) =>
        cell.Background(alt ? Soft : White)
            .BorderBottom(0.5f).BorderColor(Line)
            .PaddingVertical(6).PaddingHorizontal(8);

    public static IContainer HeadCell(IContainer cell) =>
        cell.Background(Navy).PaddingVertical(6).PaddingHorizontal(8);
}