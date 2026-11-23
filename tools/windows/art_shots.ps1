# Capturas de arte comparables (sesion 20, UE-14/UE-16): mismas posiciones de camara, FOV, resolucion y calidad para cada
# build. Encuadres de UE-16 (vista general del bar, aproximacion al tio, carrito cruzando la puerta, rampa a la
# camioneta), primeros planos del tio (frente, tres cuartos, perfil, a distancia de agarre) y menu. HUD real salvo en
# los primeros planos. Solo detiene los procesos que lanza (Guard.psm1); nunca un juego abierto por otra persona.
# Uso: powershell -ExecutionPolicy Bypass -File tools\windows\art_shots.ps1 -Build Builds\X -Out .local-setup\shots\s20\x [-Quality high|low]
# La build tiene que ser de QA o desarrollo (la de jugadores ignora las capturas).
param([Parameter(Mandatory)][string]$Build, [Parameter(Mandatory)][string]$Out, [string]$Quality = 'high', [string[]]$Only = @())
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'Guard.psm1') -Force
$Repo = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$B = if ([IO.Path]::IsPathRooted($Build)) { $Build } else { Join-Path $Repo $Build }
$O = if ([IO.Path]::IsPathRooted($Out)) { $Out } else { Join-Path $Repo $Out }
New-Item -ItemType Directory -Force $O | Out-Null
$exe = Join-Path $B 'UncleExtraction.exe'
$W = @('-screen-fullscreen', '0', '-screen-width', '1920', '-screen-height', '1080', '-ueQuality', $Quality)
$Only = @($Only | ForEach-Object { $_ -split ',' } | Where-Object { $_ })
function Shot([string]$name, [int]$wait, [string[]]$extra, [switch]$menu) {
  if ($Only.Count -and $name -notin $Only) { return }
  $a = $W + @('-logFile', "`"$O\$name.log`"") + $extra
  if (-not $menu) { $a = @('-ueScene', 'Bar_LastRound', '-ueAutopilot', 'drag', '-ueAutopilotDelay', '90') + $a }
  $p = Start-Tracked $exe $a; Start-Sleep $wait; Stop-AllTracked; Start-Sleep 1
}
$shots = @(
  @{ n = 'a1_bar_general';      x = @('-uePose', '-4.5,0,0.5,50,4@2', '-ueShot', "`"$O\a1_bar_general.png@4`"") },
  @{ n = 'a2_aproximacion';     x = @('-uePose', '4.2,0,4.4,5,12@2', '-ueShot', "`"$O\a2_aproximacion.png@4`"") },
  @{ n = 'a3_puerta_carrito';   x = @('-uePose', '0.0,0,1.6,180,6@2', '-ueShot', "`"$O\a3_puerta_carrito.png@4`"") },
  @{ n = 'a4_rampa_camioneta';  x = @('-uePose', '0.0,0,-5.0,180,10@2', '-ueShot', "`"$O\a4_rampa_camioneta.png@4`"") },
  @{ n = 'b1_tio_frente';       x = @('-ueNoHud', '-ueFaceUncle', '2', '-ueShot', "`"$O\b1_tio_frente.png@3.6`"") },
  @{ n = 'b2_tio_tres_cuartos'; x = @('-ueNoHud', '-ueFaceUncle', '2', '-ueFaceAngle', '-30', '-ueShot', "`"$O\b2_tio_tres_cuartos.png@3.6`"") },
  @{ n = 'b4_tio_cara';         x = @('-ueNoHud', '-ueFaceUncle', '2', '-ueFaceAngle', '-60', '-ueShot', "`"$O\b4_tio_cara.png@3.6`"") },
  @{ n = 'b5_tio_anticipa';     x = @('-ueNoHud', '-ueFaceUncle', '2', '-ueFaceAngle', '-60', '-ueForceAttack', 'slap@3.2', '-ueShot', "`"$O\b5_tio_anticipa.png@3.5`"") },
  @{ n = 'b3_tio_perfil';       x = @('-ueNoHud', '-ueFaceUncle', '2', '-ueFaceAngle', '90', '-ueShot', "`"$O\b3_tio_perfil.png@3.6`"") }
)
try {
  Enter-BatchLock 'art_shots'
  Shot 'warmup' 35 @()   # el primer arranque compila sombreadores
  foreach ($s in $shots) { Shot $s.n 16 $s.x }
  Shot 'm1_menu' 9 @('-ueShotMenu', "`"$O\m1_menu.png@4`"") -menu
  (Get-Content (Join-Path $B 'VERSION.txt')) + "calidad=$Quality 1920x1080" | Set-Content (Join-Path $O 'VERSION_capturada.txt')
} finally { Stop-AllTracked; Exit-BatchLock }
Get-ChildItem $O -Filter *.png | ForEach-Object { $_.Name }
