# Pruebas de Unity en modo batch con control de efectos (sesion 17).
# - Toma el bloqueo comun (una tanda a la vez) y lo libera solo si es suyo.
# - Saca una instantanea de los archivos versionados de UnityProject (Assets, ProjectSettings, Packages) antes y despues:
#   si una prueba los modifica, la tanda FALLA y lista los archivos (no se restauran solos: hay que encontrar el origen).
# Uso: powershell -ExecutionPolicy Bypass -File tools\windows\run_tests.ps1 [-Platform EditMode,PlayMode] [-Filter <regex>]
param([string[]]$Platform = @('EditMode', 'PlayMode'), [string]$Filter = '', [string]$Tag = '', [string]$UnityExe = '')
$ErrorActionPreference = 'Continue'
$Platform = @($Platform | ForEach-Object { $_ -split ',' } | Where-Object { $_ })  # -File pasa 'EditMode,PlayMode' como un solo texto
Import-Module (Join-Path $PSScriptRoot 'Guard.psm1') -Force
$Repo = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$Project = Join-Path $Repo 'UnityProject'
$LogDir = Join-Path $Repo '.local-setup\logs\unity'
New-Item -ItemType Directory -Force $LogDir | Out-Null
$editor = [regex]::Match((Get-Content (Join-Path $Project 'ProjectSettings\ProjectVersion.txt') -Raw), '(?m)^m_EditorVersion: (\S+)').Groups[1].Value
if (-not $editor) { Write-Host 'BLOCKED: ProjectVersion.txt no fija el editor'; exit 2 }
if (-not $UnityExe) { $UnityExe = Join-Path $env:ProgramFiles "Unity\Hub\Editor\$editor\Editor\Unity.exe" }
$U = Get-Item -LiteralPath $UnityExe -ErrorAction SilentlyContinue
if (-not $U -or $U.PSIsContainer -or $U.VersionInfo.ProductVersion -notmatch "^$([regex]::Escape($editor))(\b|_|\+)") {
  Write-Host "BLOCKED: se requiere Unity $editor en $UnityExe"
  exit 2
}
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
