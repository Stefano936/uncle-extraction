# Demo para compartir (sesion 17): empaqueta la build de JUGADORES (BuildTools -uePlayer: solo Frontend y Bar_LastRound,
# sin sondas, autopiloto, capturas ni atajos de diagnostico), calcula su SHA-256, la descomprime en OTRA carpeta y hace
# una prueba de humo sobre la copia descomprimida:
#   1) arranque normal: llega al menu sin excepciones;
#   2) arranque con herramientas de QA pedidas por argumento: debe quedarse en el menu sin activar ninguna.
# El ZIP anterior no se toca (cada demo tiene su nombre).
# Uso: powershell -ExecutionPolicy Bypass -File tools\windows\make_demo_zip.ps1 -Build Builds\UncleExtraction_Player -Zip Builds\UncleExtraction_Demo_s17.zip
param([string]$Build = 'Builds\UncleExtraction_Player', [string]$Zip = 'Builds\UncleExtraction_Demo_s17.zip', [string]$Guide = 'docs\VOLVER_A_PROBAR_S17.md', [string]$ExtractTo = '', [switch]$VerifyExisting)
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'Guard.psm1') -Force
$Repo = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$B = Join-Path $Repo $Build
$Z = if ([IO.Path]::IsPathRooted($Zip)) { $Zip } else { Join-Path $Repo $Zip }
function Test-PlayerBuild([string]$Folder) {
  $version = Join-Path $Folder 'VERSION.txt'
  if (!(Test-Path -LiteralPath (Join-Path $Folder 'UncleExtraction.exe') -PathType Leaf) -or !(Test-Path -LiteralPath $version -PathType Leaf)) {
    throw "Falta ejecutable o VERSION.txt en $Folder"
  }
  $identity = Get-Content -LiteralPath $version -Raw
  if ($identity -notmatch 'Tipo: jugadores' -or $identity -match 'cambios sin commitear') {
    throw "VERSION.txt no identifica una build limpia de jugadores en $Folder"
  }
}
if ($VerifyExisting) {
  if (!(Test-Path -LiteralPath $Z -PathType Leaf) -or !(Test-Path -LiteralPath "$Z.sha256" -PathType Leaf)) {
    Write-Host 'BLOCKED: se necesitan el ZIP y su archivo .sha256'; exit 2
  }
  $sidecar = Get-Content -LiteralPath "$Z.sha256" -Raw
  $hashLine = [regex]::Match($sidecar, '\A([a-fA-F0-9]{64})\s+([^\r\n]+)\s*\z')
  if (!$hashLine.Success -or $hashLine.Groups[2].Value -cne (Split-Path -Leaf $Z) -or
      $hashLine.Groups[1].Value -ne (Get-FileHash -LiteralPath $Z -Algorithm SHA256).Hash) {
    Write-Host "FAIL: el ZIP no coincide con $Z.sha256"; exit 1
  }
} else {
  try { Test-PlayerBuild $B } catch { Write-Host "BLOCKED: $_"; exit 2 }
  if (Test-Path -LiteralPath $Z) { Write-Host "Ya existe $Z (no se sobrescribe una demo entregada)"; exit 2 }
}
$dst = if ($ExtractTo) { $ExtractTo } else { Join-Path $env:TEMP ('ue_demo_check_' + (Get-Date -Format 'yyyyMMdd_HHmmss')) }
if (Test-Path -LiteralPath $dst) { Write-Host "La carpeta de verificacion debe ser nueva: $dst"; exit 2 }
$results = @(); $exit = 0
try {
  Enter-BatchLock 'make_demo_zip'
  if (!$VerifyExisting) {
    if (Test-Path (Join-Path $Repo $Guide)) { Copy-Item (Join-Path $Repo $Guide) (Join-Path $B 'VOLVER_A_PROBAR.txt') -Force }
    $files = @(Get-ChildItem -LiteralPath $B | Where-Object Name -notlike '*_BurstDebugInformation_DoNotShip')
    Compress-Archive -LiteralPath $files.FullName -DestinationPath $Z -CompressionLevel Optimal
  }
  $sha = (Get-FileHash -LiteralPath $Z -Algorithm SHA256).Hash.ToLowerInvariant()
  if (!$VerifyExisting) { "$sha  $(Split-Path -Leaf $Z)" | Set-Content -LiteralPath "$Z.sha256" -Encoding ascii }
  $size = [math]::Round((Get-Item -LiteralPath $Z).Length / 1MB, 1)
  Write-Host "ZIP: $Z ($size MB)`nSHA-256: $sha"
  # descomprimir en otro lugar y probar la copia
  Expand-Archive -LiteralPath $Z -DestinationPath $dst
  Test-PlayerBuild $dst
  $exe = Join-Path $dst 'UncleExtraction.exe'
  $win = @('-screen-fullscreen', '0', '-screen-width', '800', '-screen-height', '450')
  # 1) arranque normal
  $log1 = Join-Path $dst 'smoke_menu.log'
  # el primer arranque de una copia nueva tarda (sombreadores, antivirus): hasta 60 s para llegar al menu
  $p = Start-Tracked $exe ($win + @('-logFile', "`"$log1`"")); $t0 = Get-Date
  while (((Get-Date) - $t0).TotalSeconds -lt 60 -and -not ((Get-Content $log1 -Raw -ErrorAction SilentlyContinue) -match '\[Menu\]')) { Start-Sleep 1 }
  $results += "segundos hasta el menu: $([math]::Round(((Get-Date) - $t0).TotalSeconds))"; Start-Sleep 3; Stop-AllTracked; Start-Sleep 1
  $l1 = Get-Content $log1 -Raw -ErrorAction SilentlyContinue
  $menu = $l1 -match '\[Menu\]'; $exc = ([regex]::Matches("$l1", 'Exception')).Count
  $results += "arranque normal: menu=$menu excepciones=$exc"
  if (-not $menu -or $exc -gt 0) { $exit = 1 }
  # 2) argumentos de QA ignorados
  $log2 = Join-Path $dst 'smoke_qa_args.log'; $shot = Join-Path $dst 'no_deberia_existir.png'
  $uiAudit = Join-Path $dst 'no_ui_audit'; $camTour = Join-Path $dst 'no_cam_tour'
  $argsQA = @('-ueScene', 'PhysicsLab', '-ueAutopilot', 'drag', '-ueProbe', 'toggle',
    '-ueShotMenu', ('"{0}@3"' -f $shot), '-ueUiAudit', ('"{0}"' -f $uiAudit), '-ueCamTour', ('"{0}"' -f $camTour),
    '-ueBench', '2', '-uePerf', '1', '-logFile', ('"{0}"' -f $log2))
  $p = Start-Tracked $exe ($win + $argsQA); Start-Sleep 20; $staysOpen = -not $p.HasExited; Stop-AllTracked; Start-Sleep 1
  $l2 = Get-Content $log2 -Raw -ErrorAction SilentlyContinue
  # el laboratorio no esta en la build ni se intenta cargar; el juego se queda en el menu
  $qa = ([regex]::Matches("$l2", '\[(Autopilot|Probe|Shot|UiAudit|CamTour|Bench|Perf)\]')).Count
  $lab = $l2 -match 'PhysicsLab'; $menu2 = $l2 -match '\[Menu\]'; $exc2 = $l2 -match 'Exception'
  $artifacts = (Test-Path -LiteralPath $shot) -or (Test-Path -LiteralPath $uiAudit) -or (Test-Path -LiteralPath $camTour)
  $results += "argumentos de QA: lineas_qa=$qa artefactos=$artifacts laboratorio_mencionado=$lab menu=$menu2 sigue_abierto=$staysOpen excepciones=$exc2"
  if ($qa -gt 0 -or $artifacts -or $lab -or -not $menu2 -or -not $staysOpen -or $exc2) { $exit = 1 }
  $results += "copia descomprimida: $dst"
} catch { Write-Host "ERROR: $_"; $exit = 1 }
finally { Stop-AllTracked; Exit-BatchLock }
$results | ForEach-Object { Write-Host $_ }
Write-Host $(if ($exit -eq 0) { 'make_demo_zip PASS' } else { 'make_demo_zip FAIL' })
exit $exit
