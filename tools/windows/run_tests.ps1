# Pruebas de Unity en modo batch con control de efectos (sesion 17).
# - Toma el bloqueo comun (una tanda a la vez) y lo libera solo si es suyo.
# - Saca una instantanea de los archivos versionados de UnityProject (Assets, ProjectSettings, Packages) antes y despues:
#   si una prueba los modifica, la tanda FALLA y lista los archivos (no se restauran solos: hay que encontrar el origen).
# Uso: powershell -ExecutionPolicy Bypass -File tools\windows\run_tests.ps1 [-Platform EditMode,PlayMode] [-Filter <regex>]
param([string[]]$Platform = @('EditMode', 'PlayMode'), [string]$Filter = '', [string]$Tag = '')
$ErrorActionPreference = 'Continue'
$Platform = @($Platform | ForEach-Object { $_ -split ',' } | Where-Object { $_ })  # -File pasa 'EditMode,PlayMode' como un solo texto
Import-Module (Join-Path $PSScriptRoot 'Guard.psm1') -Force
$Repo = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$Project = Join-Path $Repo 'UnityProject'
$LogDir = Join-Path $Repo '.local-setup\logs\unity'
New-Item -ItemType Directory -Force $LogDir | Out-Null
$U = Get-ChildItem "$env:ProgramFiles\Unity\Hub\Editor\6000.3.*\Editor\Unity.exe" -ErrorAction SilentlyContinue | Sort-Object FullName -Descending | Select-Object -First 1
if (-not $U) { Write-Host 'Falta Unity 6000.3'; exit 2 }
if (-not $Tag) { $Tag = Get-Date -Format 'yyyyMMdd_HHmmss' }
$exit = 0
try {
  Enter-BatchLock "run_tests $($Platform -join ',')"
  $before = Get-AssetSnapshot
  foreach ($pl in $Platform) {
    $xml = Join-Path $LogDir "tests_${pl}_$Tag.xml"; $log = Join-Path $LogDir "tests_${pl}_$Tag.log"
    $a = @('-batchmode', '-nographics', '-projectPath', "`"$Project`"", '-logFile', "`"$log`"", '-runTests', '-testPlatform', $pl, '-testResults', "`"$xml`"")
    if ($Filter) { $a += @('-testFilter', "`"$Filter`"") }
    $p = Start-Tracked $U.FullName $a
    if (-not $p.WaitForExit(90 * 60 * 1000)) { Stop-AllTracked; Write-Host "$pl TIMEOUT"; $exit = 1; continue }
    # Sesion 21 (UE-18): con errores de compilacion Unity puede correr las pruebas con los ensamblados ANTERIORES y escribir
    # un XML en verde de codigo que no es el actual. Eso es FALLA, no PASS.
    $cs = @(Select-String -Path $log -Pattern 'error CS\d+' -ErrorAction SilentlyContinue | ForEach-Object { $_.Line.Trim() } | Select-Object -Unique)
    if ($cs.Count) {
      $exit = 1; Write-Host "$pl ERRORES DE COMPILACION ($($cs.Count)): los resultados no corresponden al codigo actual"
      $cs | Select-Object -First 5 | ForEach-Object { Write-Host "  $_" }
      continue
    }
    if (Test-Path $xml) {
      [xml]$t = Get-Content $xml; $r = $t.'test-run'
      $line = "$pl total=$($r.total) passed=$($r.passed) failed=$($r.failed) skipped=$($r.skipped) xml=$xml"
      Write-Host $line
      if ([int]$r.failed -gt 0) {
        $exit = 1
        $t.SelectNodes("//test-case[@result='Failed']") | ForEach-Object { Write-Host "  FAIL $($_.fullname): $(($_.failure.message.'#cdata-section' -split "`n")[0])" }
      }
    } else { Write-Host "$pl sin XML de resultados (ver $log)"; $exit = 1 }
  }
  $changed = Compare-AssetSnapshot $before
  if ($changed.Count) { $exit = 1; Write-Host "ASSETS MODIFICADOS por la tanda ($($changed.Count)):"; $changed | ForEach-Object { Write-Host "  $_" } }
  else { Write-Host 'Assets versionados: sin cambios' }
} catch { Write-Host "ERROR: $_"; $exit = 1 }
finally { Stop-AllTracked; Exit-BatchLock }
exit $exit
