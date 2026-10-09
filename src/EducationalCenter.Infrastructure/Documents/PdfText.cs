namespace EducationalCenter.Infrastructure.Documents;

/// <summary>Arabic labels written as escapes so file encoding can never garble them.</summary>
internal static class PdfText
{
    public const string CertificateTitle = "\u0634\u0647\u0627\u062f\u0629 \u0625\u062a\u0645\u0627\u0645";
    public const string CertificateNumber = "\u0631\u0642\u0645 \u0627\u0644\u0634\u0647\u0627\u062f\u0629";
    public const string Date = "\u0627\u0644\u062a\u0627\u0631\u064a\u062e";
    public const string ReceiptTitle = "\u0625\u064a\u0635\u0627\u0644 \u0627\u0633\u062a\u0644\u0627\u0645";
    public const string ReceiptNumber = "\u0631\u0642\u0645 \u0627\u0644\u0625\u064a\u0635\u0627\u0644";
    public const string Student = "\u0627\u0644\u0637\u0627\u0644\u0628";
    public const string Course = "\u0627\u0644\u0645\u0627\u062f\u0629";
    public const string Section = "\u0627\u0644\u0634\u0639\u0628\u0629";
    public const string InstallmentNumber = "\u0627\u0644\u062f\u0641\u0639\u0629 \u0631\u0642\u0645";
    public const string AmountPaid = "\u0627\u0644\u0645\u0628\u0644\u063a \u0627\u0644\u0645\u062f\u0641\u0648\u0639";
    public const string ExchangeRate = "\u0633\u0639\u0631 \u0627\u0644\u0635\u0631\u0641 (\u0644.\u0633 \u0644\u0643\u0644 \u062f\u0648\u0644\u0627\u0631)";
    public const string EquivalentInSyp = "\u0627\u0644\u0645\u0643\u0627\u0641\u0626 \u0628\u0627\u0644\u0644\u064a\u0631\u0629";
    public const string BalanceAfter = "\u0627\u0644\u0645\u062a\u0628\u0642\u064a \u0628\u0639\u062f \u0647\u0630\u0647 \u0627\u0644\u062f\u0641\u0639\u0629";
    public const string ReceivedBy = "\u0627\u0633\u062a\u0644\u0645\u0647\u0627";
    public const string Cancelled = "\u0645\u0644\u063a\u0649";
    public const string StampAndSignature = "\u0627\u0644\u062e\u062a\u0645 \u0648\u0627\u0644\u062a\u0648\u0642\u064a\u0639";
    public const string Approved = "\u0645\u0639\u062a\u0645\u062f";
    public const string NoData = "\u0644\u0627 \u062a\u0648\u062c\u062f \u0628\u064a\u0627\u0646\u0627\u062a";
    public const string Printed = "\u062a\u0627\u0631\u064a\u062e \u0627\u0644\u0637\u0628\u0627\u0639\u0629";
}