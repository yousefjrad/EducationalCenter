# PDF generation (parked)

Receipts, certificates and PDF report exports are written (QuestPDF) but switched off, because the
QuestPDF package is large and the download kept timing out. Everything else works, including Excel exports.
Until PDF is switched on, PDF endpoints answer HTTP 501 "not installed in this version yet".

## What is parked
Not compiled (see `<Compile Remove>` in `EducationalCenter.Infrastructure.csproj`):
- `Documents/PdfSetup.cs`
- `Documents/QuestPdfReceiptGenerator.cs`
- `Documents/QuestPdfCertificateGenerator.cs`
- `Documents/QuestPdfReportRenderer.cs`

## How to switch it on
1. Make the package available. If `dotnet restore` times out, download it by hand:
   ```powershell
   mkdir C:\nuget-local
   curl.exe -L -C - --retry 20 --retry-delay 3 -o C:\nuget-local\questpdf.2025.12.4.nupkg https://api.nuget.org/v3-flatcontainer/questpdf/2025.12.4/questpdf.2025.12.4.nupkg
   dotnet nuget add source C:\nuget-local --name local-packages
   ```
2. In `EducationalCenter.Infrastructure.csproj`: add `<PackageReference Include="QuestPDF" Version="2025.12.4" />`
   and delete the two `<Compile Remove=... />` lines.
3. In `Documents/ReportExporter.cs`: make `ToPdf` return `QuestPdfReportRenderer.Render(table);`.
4. In `DependencyInjection.cs`: register `QuestPdfReceiptGenerator` and `QuestPdfCertificateGenerator`
   instead of the two `Unavailable...` classes.
5. `dotnet build`.
