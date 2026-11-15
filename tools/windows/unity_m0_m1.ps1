# M0 + M1: crea/abre UnityProject en modo batch, instala paquetes base, construye PhysicsLab,
# ejecuta pruebas EditMode y PlayMode y genera una build Windows (Mono). Requiere licencia de Unity activa.
$ErrorActionPreference = 'Continue'
$Repo    = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$Project = Join-Path $Repo 'UnityProject'
$LogDir  = Join-Path $Repo '.local-setup\logs\unity'
New-Item -ItemType Directory -Force -Path $LogDir | Out-Null
Start-Transcript -Path (Join-Path $LogDir ('unity_m0_m1_' + (Get-Date -Format 'yyyyMMdd_HHmmss') + '.log')) -Force | Out-Null

$exe = Get-ChildItem -Path "$env:ProgramFiles\Unity\Hub\Editor\6000.3.*\Editor\Unity.exe" -ErrorAction SilentlyContinue | Sort-Object FullName -Descending | Select-Object -First 1
if (-not $exe) { Write-Host 'No encontre Unity 6000.3 en la ruta por defecto del Hub. Ejecuta antes setup_entorno.cmd.' -ForegroundColor Red; Stop-Transcript | Out-Null; Read-Host 'Enter'; exit 1 }
$U = $exe.FullName
Write-Host "Editor: $U"

function Unity-Step($name, [string[]]$extra, [int]$timeoutMin = 40) {
  $log = Join-Path $LogDir "$name.log"
  $uargs = @('-batchmode', '-nographics', '-projectPath', "`"$Project`"", '-logFile', "`"$log`"") + $extra
  Write-Host ""; Write-Host "==== $name ====" -ForegroundColor Cyan
  Write-Host ("Unity.exe " + ($uargs -join ' '))
  $p = Start-Process -FilePath $U -ArgumentList $uargs -PassThru -NoNewWindow
  if (-not $p.WaitForExit($timeoutMin * 60 * 1000)) { $p.Kill(); Write-Host "TIMEOUT en $name" -ForegroundColor Red; return 99 }
  $code = $p.ExitCode
  $errs = Select-String -Path $log -Pattern 'error CS\d+|Scripts have compiler errors|\[Bootstrap\]|\[LabBuilder\]|\[Build\]|No valid Unity Editor license|License' -ErrorAction SilentlyContinue | Select-Object -First 40
  $errs | ForEach-Object { Write-Host $_.Line }
  Write-Host "$name exit=$code" -ForegroundColor ($(if ($code -eq 0) { 'Green' } else { 'Red' }))
  return $code
}

$r1 = Unity-Step '01_bootstrap_paquetes' @('-executeMethod', 'UncleExtraction.EditorTools.ProjectBootstrap.Run')
$r2 = Unity-Step '02_construir_physicslab' @('-quit', '-executeMethod', 'UncleExtraction.EditorTools.PhysicsLabBuilder.BuildFromCommandLine')
$r3 = Unity-Step '03_tests_editmode' @('-runTests', '-testPlatform', 'EditMode', '-testResults', "`"$LogDir\03_tests_editmode.xml`"")
$r4 = Unity-Step '04_tests_playmode' @('-runTests', '-testPlatform', 'PlayMode', '-testResults', "`"$LogDir\04_tests_playmode.xml`"")
$r5 = Unity-Step '05_build_windows_mono' @('-quit', '-executeMethod', 'UncleExtraction.EditorTools.BuildTools.BuildWindows', '-ueBackend', 'Mono', '-ueOut', "`"$Repo\Builds\PhysicsLab`"")

foreach ($x in '03_tests_editmode', '04_tests_playmode') {
  $f = Join-Path $LogDir "$x.xml"
  if (Test-Path $f) { [xml]$t = Get-Content $f; $r = $t.'test-run'; Write-Host "$x : total=$($r.total) passed=$($r.passed) failed=$($r.failed) skipped=$($r.skipped)" }
  else { Write-Host "$x : sin XML de resultados" -ForegroundColor Red }
}
$ver = Get-Content (Join-Path $Project 'ProjectSettings\ProjectVersion.txt') -ErrorAction SilentlyContinue
Write-Host "ProjectVersion: $ver"
Write-Host "Resumen: bootstrap=$r1 escena=$r2 editmode=$r3 playmode=$r4 build=$r5"
Write-Host "Build: $Repo\Builds\PhysicsLab\UncleExtraction_PhysicsLab.exe"
Write-Host 'Avisale a Claude en el chat: va a leer los registros en .local-setup\logs\unity.' -ForegroundColor Green
Stop-Transcript | Out-Null
Read-Host 'Enter para cerrar'
