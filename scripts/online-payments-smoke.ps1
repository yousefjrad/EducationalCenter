param([string]$Base = 'http://localhost:5080')
$ErrorActionPreference = 'Stop'
$email = Read-Host 'بريد المدير'
$pass  = Read-Host 'كلمة مرور المدير'
$script:fail = 0

function Call($method, $path, $body, $token) {
    $p = @{ UseBasicParsing = $true; Method = $method; Uri = "$Base/api/v1/$path"; ContentType = 'application/json'; Headers = @{} }
    if ($token) { $p.Headers.Authorization = "Bearer $token" }
    if ($body)  { $p.Body = ($body | ConvertTo-Json) }
    try { $r = Invoke-WebRequest @p; $status = [int]$r.StatusCode; $raw = $r.Content }
    catch {
        $status = [int]$_.Exception.Response.StatusCode; $raw = ''
        try { $raw = (New-Object IO.StreamReader($_.Exception.Response.GetResponseStream())).ReadToEnd() } catch {}
    }
    $json = $null; if ($raw) { try { $json = $raw | ConvertFrom-Json } catch {} }
    [pscustomobject]@{ Status = $status; Json = $json }
}
function Check($name, $ok) {
    if ($ok) { Write-Host "PASS  $name" -ForegroundColor Green } else { Write-Host "FAIL  $name" -ForegroundColor Red; $script:fail++ }
}

$rnd = Get-Random -Minimum 100000 -Maximum 999999
$pw = 'Student@12345'

$admin = Call 'POST' 'auth/login' @{ email = $email; password = $pass } $null
Check 'تسجيل دخول المدير' ($admin.Status -eq 200)
$tok = $admin.Json.accessToken

$nameA = "Pay A $rnd"
$regA = Call 'POST' 'public/register' @{ fullName = $nameA; phoneNumber = "09$rnd"; email = "paya$rnd@test.local"; password = $pw } $null
$regB = Call 'POST' 'public/register' @{ fullName = "Pay B $rnd"; phoneNumber = "08$rnd"; email = "payb$rnd@test.local"; password = $pw } $null
Check 'تسجيل الطالبين A وB' ($regA.Status -eq 201 -and $regB.Status -eq 201)
$ta = $regA.Json.accessToken; $tb = $regB.Json.accessToken

$pub = Call 'GET' 'public/sections' $null $null
$open = @($pub.Json | Where-Object { $_.seatsLeft -gt 0 })
if ($open.Count -eq 0) {
    Write-Host 'SKIP  لا توجد شعبة مفتوحة فيها مقاعد (افتح شعبة للتسجيل وأعد التشغيل)' -ForegroundColor Yellow
    return
}

$found = Call 'GET' "students?search=$([uri]::EscapeDataString($nameA))" $null $tok
$sid = @($found.Json.items)[0].id
$enr = Call 'POST' 'enrollments' @{ studentId = $sid; sectionId = $open[0].id } $tok
Check 'المدير يسجّل A في شعبة (201)' ($enr.Status -eq 201)
$eid = $enr.Json.id

$plan = Call 'POST' 'payment-plans' @{ enrollmentId = $eid; installmentsCount = 2; firstDueDate = '2030-01-01' } $tok
Check 'خطة دفع بقسطين (201)' ($plan.Status -eq 201)
$inst = @($plan.Json.installments)[0]

$start = Call 'POST' 'online-payments' @{ installmentId = $inst.id } $ta
Check 'A يبدأ دفع القسط الأول (201 والحالة Pending)' ($start.Status -eq 201 -and $start.Json.status -eq 'Pending' -and $start.Json.amountInSyp -eq $inst.amount)
$ref = $start.Json.reference

$over = Call 'POST' 'online-payments' @{ installmentId = $inst.id; amount = ($inst.amount + 1) } $ta
Check 'مبلغ أكبر من المتبقي مرفوض (409)' ($over.Status -eq 409)

$other = Call 'POST' "online-payments/$ref/simulate-success" $null $tb
Check 'B لا يستطيع تأكيد دفع A (404)' ($other.Status -eq 404)

$ok = Call 'POST' "online-payments/$ref/simulate-success" $null $ta
Check 'A يؤكد الدفع (200 والحالة Succeeded وإيصال)' ($ok.Status -eq 200 -and $ok.Json.status -eq 'Succeeded' -and $ok.Json.receiptId -gt 0)

$again = Call 'POST' "online-payments/$ref/simulate-success" $null $ta
Check 'تأكيد نفس الطلب مرة ثانية مرفوض (409)' ($again.Status -eq 409)

$pays = Call 'GET' "me/enrollments/$eid/payments" $null $ta
$first = @($pays.Json.installments)[0]
Check 'القسط الأول صار مدفوعاً بالكامل' ($pays.Status -eq 200 -and $first.remainingInSyp -eq 0 -and @($first.payments).Count -eq 1)

$paid = Call 'POST' 'online-payments' @{ installmentId = $inst.id } $ta
Check 'دفع قسط مدفوع بالكامل مرفوض (409)' ($paid.Status -eq 409)

$bTry = Call 'POST' 'online-payments' @{ installmentId = $inst.id } $tb
Check 'B لا يدفع قسط A (404)' ($bTry.Status -eq 404)

$mine = Call 'GET' 'online-payments' $null $ta
Check 'قائمة دفعات A تحوي طلباً ناجحاً' ($mine.Status -eq 200 -and @($mine.Json).Count -eq 1 -and @($mine.Json)[0].status -eq 'Succeeded')

if ($script:fail -eq 0) { Write-Host 'كلها PASS' -ForegroundColor Green } else { Write-Host "$script:fail فشل" -ForegroundColor Red }