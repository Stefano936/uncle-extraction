# Autoprueba del bloqueo de tandas (sesion 17): dos arranques simultaneos -> exactamente uno toma el bloqueo; un
# cancelado (proceso matado) deja un bloqueo viejo que el siguiente recupera; quien no lo tomo no lo borra.
$ErrorActionPreference = 'Stop'
$mod = Join-Path $PSScriptRoot 'Guard.psm1'
$Repo = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$lock = Join-Path $Repo '.local-setup\batch.lock'
Import-Module $mod -Force
try { Enter-BatchLock 'selftest-pre' } catch { Write-Host "Hay una tanda en curso: $_"; exit 2 } finally { Exit-BatchLock }
$out = Join-Path $env:TEMP "ue_guard_selftest"; New-Item -ItemType Directory -Force $out | Out-Null; Remove-Item "$out\*" -Force -ErrorAction SilentlyContinue
$child = @"
Import-Module '$mod' -Force
`$f = Join-Path '$out' ("" + `$PID + '.txt')
try { Enter-BatchLock 'selftest'; 'OWNED' | Set-Content `$f; Start-Sleep 6 } catch { "DENIED `$_" | Set-Content `$f; Exit-BatchLock; exit 0 } finally { Exit-BatchLock }
"@
$cf = Join-Path $out 'child.ps1'; $child | Set-Content $cf
$res = @()
# 1) cuatro arranques simultaneos
$ps = 1..4 | ForEach-Object { Start-Process powershell -ArgumentList @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', "`"$cf`"") -PassThru -WindowStyle Hidden }
Start-Sleep 3
$held = Test-Path $lock
$ps | ForEach-Object { $_.WaitForExit(20000) | Out-Null }
$o = Get-ChildItem $out -Filter '*.txt' | ForEach-Object { Get-Content $_.FullName -Raw }
$owned = @($o | Where-Object { $_ -match '^OWNED' }).Count; $denied = @($o | Where-Object { $_ -match '^DENIED' }).Count
$res += "simultaneos: duenios=$owned rechazados=$denied bloqueo_mientras_corre=$held liberado_al_final=$(-not (Test-Path $lock))"
$ok1 = $owned -eq 1 -and $denied -eq 3 -and $held -and -not (Test-Path $lock)
# 2) cancelado: se mata al duenio a mitad; el bloqueo queda viejo y el siguiente lo recupera
Remove-Item "$out\*.txt" -Force -ErrorAction SilentlyContinue
$p = Start-Process powershell -ArgumentList @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', "`"$cf`"") -PassThru -WindowStyle Hidden
Start-Sleep 3; Stop-Process -Id $p.Id -Force; Start-Sleep 1
$stale = Test-Path $lock # el archivo informativo queda; el mutex queda abandonado
Import-Module $mod -Force
$recovered = $false; try { Enter-BatchLock 'selftest-recover'; $recovered = $true } catch {} finally { Exit-BatchLock }
$res += "cancelado: bloqueo_viejo=$stale recuperado=$recovered liberado=$(-not (Test-Path $lock))"
$ok2 = $stale -and $recovered -and -not (Test-Path $lock)
$res | ForEach-Object { Write-Host $_ }
if ($ok1 -and $ok2) { Write-Host 'guard_selftest PASS'; exit 0 } else { Write-Host 'guard_selftest FAIL'; exit 1 }
