using QuestPDF.Infrastructure;

namespace EducationalCenter.Infrastructure.Documents;

internal static class PdfSetup
{
    private static readonly object Gate = new();
    private static bool _initialized;

    /// <summary>
    /// Installed on every Windows machine and has Arabic and Latin glyphs.
    /// On Linux, install it (or change this name) before generating PDFs.
    /// </summary>
    public const string FontFamily = "Arial";

    public static void EnsureInitialized()
    {
        if (_initialized)
            return;

        lock (Gate)
        {
            if (_initialized)
                return;

            // QuestPDF's Community license is free for individuals and organisations under 1M USD yearly revenue.
            QuestPDF.Settings.License = LicenseType.Community;
            _initialized = true;
        }
    }
}
