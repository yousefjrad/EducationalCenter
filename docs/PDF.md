# PDF generation

Receipts, certificates and PDF report exports are generated with QuestPDF (Community license) and are switched on.

## Where things are
- `Documents/PdfSetup.cs`: license and font (`Arial`, installed on every Windows machine; install it or change the name on Linux).
- `Documents/PdfTheme.cs`: the shared colours (navy and gold) and table cell styles.
- `Documents/PdfText.cs`: Arabic labels written as `\u` escapes so file encoding can never garble them.
- `Documents/QuestPdfReceiptGenerator.cs`: A5 receipt (header band, receipt number, amount and balance boxes, stamp and signature box, void banner).
- `Documents/QuestPdfCertificateGenerator.cs`: A4 landscape certificate (double border, optional logo, seal, signature line).
- `Documents/QuestPdfReportRenderer.cs`: A4 landscape report (title band, repeating table header, zebra rows, summary box, page numbers).

## Endpoints
- `GET /api/v1/receipts/{id}/pdf?language=ar|en`
- `GET /api/v1/certificates/{id}/pdf?language=ar|en`
- `GET /api/v1/reports/financial/export` and `/operational/export` with the PDF format.
- Student portal: `GET /api/v1/me/receipts/{id}/pdf` and `/me/certificates/{id}/pdf` (own documents only).

## Package download
If `dotnet restore` times out on a slow connection, download QuestPDF by hand:
```powershell
mkdir C:\nuget-local
curl.exe -L -C - --retry 20 --retry-delay 3 -o C:\nuget-local\questpdf.2025.12.4.nupkg https://api.nuget.org/v3-flatcontainer/questpdf/2025.12.4/questpdf.2025.12.4.nupkg
dotnet nuget add source C:\nuget-local --name nuget-local
```

## Checking the look
Run the API, then `scripts\pdf-check.ps1`: it signs in, downloads the first receipt and certificate it finds (Arabic and English) to `%TEMP%\pdf-check` and opens them.