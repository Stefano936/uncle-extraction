# Demo para compartir (sesion 17): empaqueta la build de JUGADORES (BuildTools -uePlayer: solo Frontend y Bar_LastRound,
# sin sondas, autopiloto, capturas ni atajos de diagnostico), calcula su SHA-256, la descomprime en OTRA carpeta y hace
# una prueba de humo sobre la copia descomprimida:
#   1) arranque normal: llega al menu sin excepciones;
#   2) arranque con argumentos de QA (-ueScene PhysicsLab -ueAutopilot drag -ueProbe toggle -ueShot): deben ignorarse
#      (sin lineas de autopiloto/sonda, sin captura, sin el laboratorio).
# El ZIP anterior no se toca (cada demo tiene su nombre).
# Uso: powershell -ExecutionPolicy Bypass -File tools\windows\make_demo_zip.ps1 -Build Builds\UncleExtraction_Player -Zip Builds\UncleExtraction_Demo_s17.zip
param([string]$Build = 'Builds\UncleExtraction_Player', [string]$Zip = 'Builds\UncleExtraction_Demo_s17.zip', [string]$Guide = 'docs\VOLVER_A_PROBAR_S17.md')
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'Guard.psm1') -Force
$Repo = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$B = Join-Path $Repo $Build; $Z = Join-Path $Repo $Zip
$ver = Join-Path $B 'VERSION.txt'
if (-not (Test-Path (Join-Path $B 'UncleExtraction.exe')) -or -not (Test-Path $ver)) { Write-Host "Falta la build de jugadores en $B"; exit 2 }
$vt = Get-Content $ver -Raw
if ($vt -notmatch 'Tipo: jugadores') { Write-Host "La build de $B no es de jugadores:`n$vt"; exit 2 }
if ($vt -match 'cambios sin commitear') { Write-Host "La build se genero con cambios sin commitear"; exit 2 }
if (Test-Path $Z) { Write-Host "Ya existe $Z (no se sobrescribe una demo entregada)"; exit 2 }
$results = @(); $exit = 0
try {
  Enter-BatchLock 'make_demo_zip'
  if (Test-Path (Join-Path $Repo $Guide)) { Copy-Item (Join-Path $Repo $Guide) (Join-Path $B 'VOLVER_A_PROBAR.txt') -Force }
  Get-ChildItem $B -Filter '*_BurstDebugInformation_DoNotShip' -Directory -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force
  Compress-Archive -Path (Join-Path $B '*') -DestinationPath $Z -CompressionLevel Optimal
  $sha = (Get-FileHash $Z -Algorithm SHA256).Hash.ToLowerInvariant()
  "$sha  $(Split-Path -Leaf $Z)" | Set-Content "$Z.sha256" -Encoding ascii
  $size = [math]::Round((Get-Item $Z).Length / 1MB, 1)
  Write-Host "ZIP: $Z ($size MB)`nSHA-256: $sha"
  # descomprimir en otro lugar y probar la copia
  $dst = Join-Path $env:TEMP ("ue_demo_check_" + (Get-Date -Format 'yyyyMMdd_HHmmss'))
  Expand-Archive $Z -DestinationPath $dst
  $exe = Join-Path $dst 'UncleExtraction.exe'
  $win = @('-screen-fullscreen', '0', '-screen-width', '800', '-screen-height', '450')
  # 1) arranque normal
  $log1 = Join-Path $dst 'smoke_menu.log'
  $p = Start-Tracked $exe ($win + @('-logFile', "`"$log1`"")); Start-Sleep 20; Stop-AllTracked; Start-Sleep 1
  $l1 = Get-Content $log1 -Raw -ErrorAction SilentlyContinue
  $menu = $l1 -match '\[Menu\]'; $exc = ([regex]::Matches("$l1", 'Exception')).Count
  $results += "arranque normal: menu=$menu excepciones=$exc"
  if (-not $menu -or $exc -gt 0) { $exit = 1 }
  # 2) argumentos de QA ignorados
  $log2 = Join-Path $dst 'smoke_qa_args.log'; $shot = Join-Path $dst 'no_deberia_existir.png'
  $p = Start-Tracked $exe ($win + @('-ueScene', 'PhysicsLab', '-ueAutopilot', 'drag', '-ueProbe', 'toggle', '-ueShot', "`"$shot`"@3", '-logFile', "`"$log2`"")); Start-Sleep 20; Stop-AllTracked; Start-Sleep 1
  $l2 = Get-Content $log2 -Raw -ErrorAction SilentlyContinue
  $qa = ([regex]::Matches("$l2", '\[Autopilot\]|\[Probe\]|\[Shot\]')).Count; $lab = $l2 -match 'PhysicsLab\b.*(cargada|loaded)'; $shotMade = Test-Path $shot
  $results += "argumentos de QA: lineas_qa=$qa captura=$shotMade laboratorio=$lab"
  if ($qa -gt 0 -or $shotMade -or $lab) { $exit = 1 }
  $results += "copia descomprimida: $dst"
} catch { Write-Host "ERROR: $_"; $exit = 1 }
finally { Stop-AllTracked; Exit-BatchLock }
$results | ForEach-Object { Write-Host $_ }
Write-Host $(if ($exit -eq 0) { 'make_demo_zip PASS' } else { 'make_demo_zip FAIL' })
exit $exit
