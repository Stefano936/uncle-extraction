# Clip de juego real (sesion 21): el bot de QA juega una ronda y se guardan cuadros con paso fijo (-ueFrames), que se
# convierten a WebP animado (sin ffmpeg). Mismas condiciones para comparar builds: escena, modo del bot, resolucion,
# calidad, inicio, duracion y fps. Solo detiene los procesos que lanza.
# Uso: powershell -ExecutionPolicy Bypass -File tools\windows\clip.ps1 -Build Builds\X -Out .local-setup\clips\x [-Mode drag|cart]
#      [-Start 4] [-Seconds 20] [-Fps 10] [-Width 960] [-Height 540] [-Quality high] [-Extra '-ueFaceUncle','2']
param([Parameter(Mandatory)][string]$Build, [Parameter(Mandatory)][string]$Out, [string]$Mode = 'drag', [float]$Start = 4,
      [float]$Seconds = 20, [int]$Fps = 10, [int]$Width = 960, [int]$Height = 540, [string]$Quality = 'high', [string[]]$Extra = @())
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'Guard.psm1') -Force
$Repo = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$B = if ([IO.Path]::IsPathRooted($Build)) { $Build } else { Join-Path $Repo $Build }
$O = if ([IO.Path]::IsPathRooted($Out)) { $Out } else { Join-Path $Repo $Out }
$frames = Join-Path $O 'frames'; if (Test-Path $frames) { Remove-Item $frames -Recurse -Force }; New-Item -ItemType Directory -Force $frames | Out-Null
$exe = Join-Path $B 'UncleExtraction.exe'
$inv = [Globalization.CultureInfo]::InvariantCulture
$spec = "$frames@$($Start.ToString($inv))@$($Seconds.ToString($inv))@$Fps"
$a = @('-screen-fullscreen', '0', '-screen-width', "$Width", '-screen-height', "$Height", '-ueQuality', $Quality, '-ueScene', 'Bar_LastRound',
       '-ueAutopilot', $Mode, '-ueFrames', "`"$spec`"", '-logFile', "`"$O\clip.log`"") + $Extra
try {
  Enter-BatchLock 'clip'
  $p = Start-Tracked $exe $a
  $deadline = (Get-Date).AddSeconds($Start + $Seconds * 8 + 60)
  while ((Get-Date) -lt $deadline -and -not $p.HasExited) {
    Start-Sleep 2
    if (Select-String -Path "$O\clip.log" -Pattern '\[Shot\] \d+ cuadros' -Quiet -ErrorAction SilentlyContinue) { break }
  }
} finally { Stop-AllTracked; Exit-BatchLock }
(Get-Content (Join-Path $B 'VERSION.txt') -TotalCount 1) + " | modo=$Mode $Width x $Height $Quality inicio=$Start s duracion=$Seconds s fps=$Fps" | Set-Content (Join-Path $O 'clip_info.txt')
python (Join-Path $PSScriptRoot '..\art\frames_to_webp.py') $frames (Join-Path $O 'clip.webp') $Fps
