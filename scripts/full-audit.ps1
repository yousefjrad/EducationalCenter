$base = 'http://localhost:5080'
try { Invoke-WebRequest "$base/health" -UseBasicParsing | Out-Null } catch { Write-Host 'API is not running on 5080' -ForegroundColor Red; return }

$adminEmail = Read-Host 'Admin email'
$adminPass  = Read-Host 'Admin password'

function FindTok($o) {
  if ($o -eq $null) { return $null }
  foreach ($p in $o.PSObject.Properties) {
    if ($p.Name -match '^(accessToken|token)$') { return $p.Value }
    if ($p.Value -is [System.Management.Automation.PSCustomObject]) { $t = FindTok $p.Value; if ($t) { return $t } }
  }
  return $null
}

function Call($m, $p, $tok, $multi) {
  $h = @{}
  if ($tok) { $h.Authorization = "Bearer $tok" }
  try {
    if ($multi) {
      $b = '----b' + [guid]::NewGuid().ToString('N')
      $body = "--$b`r`nContent-Disposition: form-data; name=`"file`"; filename=`"x.xlsx`"`r`nContent-Type: application/octet-stream`r`n`r`nx`r`n--$b--`r`n"
      $r = Invoke-WebRequest -Uri ($base + $p) -Method $m -Headers $h -Body $body -ContentType "multipart/form-data; boundary=$b" -UseBasicParsing
    } elseif ($m -eq 'GET') {
      $r = Invoke-WebRequest -Uri ($base + $p) -Method $m -Headers $h -UseBasicParsing
    } else {
      $r = Invoke-WebRequest -Uri ($base + $p) -Method $m -Headers $h -Body '{}' -ContentType 'application/json' -UseBasicParsing
    }
    return [int]$r.StatusCode
  } catch {
    if ($_.Exception.Response) { return [int]$_.Exception.Response.StatusCode }
    return -1
  }
}

# tokens
$adminTok = $null; $studTok = $null
try {
  $r = Invoke-RestMethod -Method Post -Uri "$base/api/v1/auth/login" -ContentType 'application/json' -Body (@{ email = $adminEmail; password = $adminPass } | ConvertTo-Json)
  $adminTok = FindTok $r
} catch { Write-Host "Admin login failed: $($_.Exception.Message)" -ForegroundColor Red }
try {
  $rnd = Get-Random -Minimum 10000000 -Maximum 99999999
  $r = Invoke-RestMethod -Method Post -Uri "$base/api/v1/public/register" -ContentType 'application/json' -Body (@{ fullName = "Audit Student $rnd"; phoneNumber = "09$rnd"; email = "audit$rnd@test.local"; password = 'Test@12345Aa' } | ConvertTo-Json)
  $studTok = FindTok $r
} catch { Write-Host "Student register failed: $($_.Exception.Message)" -ForegroundColor Red }
if (-not $adminTok -or -not $studTok) { Write-Host 'Missing token, stopping.' -ForegroundColor Red; return }

$sw = Invoke-RestMethod "$base/swagger/v1/swagger.json"
$ops = @()
foreach ($pp in $sw.paths.PSObject.Properties) {
  foreach ($mm in $pp.Value.PSObject.Properties) {
    if ($mm.Name -in 'get','post','put','patch','delete') { $ops += [pscustomobject]@{ M = $mm.Name.ToUpper(); P = $pp.Name } }
  }
}
Write-Host "Operations: $($ops.Count)"

$pass = 0; $fail = 0; $warn = 0
function Ok($t) { $script:pass++ }
function Bad($t) { $script:fail++; Write-Host "FAIL  $t" -ForegroundColor Red }
function Wn($t) { $script:warn++; Write-Host "WARN  $t" -ForegroundColor Yellow }

foreach ($o in $ops) {
  $path = [regex]::Replace($o.P, '\{[^}]+\}', '2147483647')
  $hasParam = $o.P -match '\{'
  $multi = ($o.P -match '/import') -and ($o.M -ne 'GET')
  $label = "$($o.M) $path"
  if ($path -match '/health') { continue }

  # anonymous
  $s = Call $o.M $path $null $multi
  $pub = $path -match '/auth/(login|refresh|logout)|/public/'
  if ($pub) { if ($s -ge 500 -or $s -eq -1) { Bad "anon $label -> $s" } else { Ok } }
  elseif ($s -eq 401) { Ok } else { Bad "anon $label -> $s (expected 401)" }

  # student
  $free = $path -match '/auth/|/public/|/me/|/online-payments|/alerts'
  if (-not $free) {
    $s = Call $o.M $path $studTok $multi
    if ($s -eq 403) { Ok } else { Bad "student $label -> $s (expected 403)" }
  }

  # admin: GET without path params only
  if ($o.M -eq 'GET' -and -not $hasParam) {
    $s = Call 'GET' $path $adminTok $false
    if ($s -eq 200) { Ok }
    elseif ($s -eq 404 -and $path -match '/me/|/online-payments') { Ok }
    elseif ($s -eq 400 -and $path -match '/reports/') { Ok }
    elseif ($s -ge 500 -or $s -eq 401 -or $s -eq 403 -or $s -eq -1) { Bad "admin $label -> $s" }
    else { Wn "admin $label -> $s" }
  }
}

Write-Host ''
Write-Host "PASS=$pass  FAIL=$fail  WARN=$warn" -ForegroundColor Cyan