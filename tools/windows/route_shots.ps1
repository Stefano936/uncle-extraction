# Capturas del entorno de la ruta (sesion 21, UE-16): mismas camaras para comparar antes/despues en la MISMA build
# (-ueOldRoute = entorno anterior). Puerta por dentro y por fuera, carrito, vereda, rampa, carga, costado y frente de la
# camioneta y cuadra de enfrente. Solo detiene los procesos que lanza (Guard.psm1).
# Uso: powershell -ExecutionPolicy Bypass -File tools\windows\route_shots.ps1 -Build Builds\X -Out carpeta [-Old] [-Quality high|low]
param([Parameter(Mandatory)][string]$Build, [Parameter(Mandatory)][string]$Out, [switch]$Old, [string]$Quality = 'high', [string[]]$Only = @())
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'Guard.psm1') -Force
$Repo = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$B = if ([IO.Path]::IsPathRooted($Build)) { $Build } else { Join-Path $Repo $Build }
$O = if ([IO.Path]::IsPathRooted($Out)) { $Out } else { Join-Path $Repo $Out }
New-Item -ItemType Directory -Force $O | Out-Null
$exe = Join-Path $B 'UncleExtraction.exe'
$base = @('-screen-fullscreen', '0', '-screen-width', '1920', '-screen-height', '1080', '-ueQuality', $Quality, '-ueScene', 'Bar_LastRound',
          '-ueAutopilot', 'drag', '-ueAutopilotDelay', '90')
if ($Old) { $base += '-ueOldRoute' }
$Only = @($Only | ForEach-Object { $_ -split ',' } | Where-Object { $_ })
$shots = [ordered]@{
  'r1_puerta_adentro'   = '0.0,0,1.6,180,6'
  'r2_puerta_afuera'    = '0.3,0,-4.2,0,4'
  'r3_carrito'          = '2.5,0,-0.4,0,32'
  'r4_vereda'           = '1.5,0,-3.6,215,3'
  'r5_rampa'            = '0.0,0,-5.0,180,10'
  'r6_carga'            = '0.0,0,-8.7,180,14'
  'r7_costado'          = '4.2,0,-8.0,226,5'
  'r8_frente'           = '3.5,0,-16.5,298,4'
  'r9_cuadra_enfrente'  = '4.5,0,-8.0,180,-8'
}
function Shot([string]$name, [int]$wait, [string[]]$extra) {
  $a = $base + @('-logFile', "`"$O\$name.log`"") + $extra
  $p = Start-Tracked $exe $a; Start-Sleep $wait; Stop-AllTracked; Start-Sleep 1
}
try {
  Enter-BatchLock 'route_shots'
  Shot 'warmup' 35 @()
  foreach ($k in $shots.Keys) {
    if ($Only.Count -and $k -notin $Only) { continue }
    Shot $k 16 @('-uePose', "$($shots[$k])@2", '-ueShot', "`"$O\$k.png@4`"")
    if (-not (Test-Path "$O\$k.png")) { Shot $k 20 @('-uePose', "$($shots[$k])@2", '-ueShot', "`"$O\$k.png@4`"") }
  }
  (Get-Content (Join-Path $B 'VERSION.txt')) + "calidad=$Quality 1920x1080 entorno=$(if ($Old) { 'anterior (-ueOldRoute)' } else { 'S21' })" | Set-Content (Join-Path $O 'VERSION_capturada.txt')
} finally { Stop-AllTracked; Exit-BatchLock }
Get-ChildItem $O -Filter *.png | ForEach-Object { $_.Name }
