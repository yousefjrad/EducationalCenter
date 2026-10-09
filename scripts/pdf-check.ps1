param([string]$Base = 'http://localhost:5080')
$ErrorActionPreference = 'Stop'

if (-not (Test-Path 'src\EducationalCenter.Infrastructure\Documents\PdfTheme.cs')) {
    Write-Host 'تنبيه: ملف PdfTheme.cs غير موجود، أي أن التصميم الجديد لم يُطبّق بعد.' -ForegroundColor Yellow
}

try { Invoke-RestMethod "$Base/health" | Out-Null }
catch { Write-Host "الـ API لا يعمل على $Base. شغّله أولاً." -ForegroundColor Red; return }

$email = Read-Host 'البريد'
$pass  = Read-Host 'كلمة المرور'
$login = Invoke-RestMethod -Method Post -Uri "$Base/api/v1/auth/login" -ContentType 'application/json' `
    -Body (@{ email = $email; password = $pass } | ConvertTo-Json)
$token = $login.accessToken
if (-not $token) { $token = $login.data.accessToken }
if (-not $token) { Write-Host 'لم أجد accessToken في الرد:' ; $login | ConvertTo-Json -Depth 4; return }
$h = @{ Authorization = "Bearer $token" }

$out = Join-Path $env:TEMP 'pdf-check'
New-Item -ItemType Directory -Force $out | Out-Null

function Get-FirstPdf($kind) {
    foreach ($id in 1..30) {
        foreach ($lang in 'ar','en') {
            $file = Join-Path $out "$kind-$id-$lang.pdf"
            try {
                Invoke-WebRequest -UseBasicParsing -Headers $h -OutFile $file -Uri "$Base/api/v1/$kind/$id/pdf?language=$lang" | Out-Null
                Write-Host "OK  $file" -ForegroundColor Green
                if ($lang -eq 'en') { return }
            } catch {
                if (Test-Path $file) { Remove-Item $file -Force }
                if ($lang -eq 'ar') { break }
            }
        }
        if (Test-Path (Join-Path $out "$kind-$id-ar.pdf")) { return }
    }
    Write-Host "لا يوجد $kind بالأرقام 1 إلى 30 (أنشئ واحداً أولاً)" -ForegroundColor Yellow
}

Get-FirstPdf 'receipts'
Get-FirstPdf 'certificates'

Write-Host "الملفات في: $out"
Get-ChildItem $out -Filter *.pdf | ForEach-Object { Invoke-Item $_.FullName }