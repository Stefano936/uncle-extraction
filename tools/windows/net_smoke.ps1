# UNCLE EXTRACTION - pruebas de humo de red (M2) con DOS PROCESOS en la misma PC (localhost, 127.0.0.1).
# No es una prueba LAN ni de internet: registrar esos resultados por separado.
# Uso: powershell -ExecutionPolicy Bypass -File tools\windows\net_smoke.ps1 [-Scenario coop|race|hostleave|clientdrop|all] [-Runs 1]
param([string]$Scenario = 'all', [int]$Runs = 1)
$ErrorActionPreference = 'Continue'
$Repo = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$Exe  = Join-Path $Repo 'Builds\PhysicsLab\UncleExtraction_PhysicsLab.exe'
$Log  = Join-Path $Repo '.local-setup\logs\net'
New-Item -ItemType Directory -Force -Path $Log | Out-Null
if (-not (Test-Path $Exe)) { Write-Host "Falta la build: $Exe" -ForegroundColor Red; exit 2 }
$Win = @('-screen-fullscreen','0','-screen-width','800','-screen-height','450')
$results = @()

function Start-Game($name, [string[]]$extra) {
  $f = Join-Path $Log "$name.log"; if (Test-Path $f) { Remove-Item $f }
  $p = Start-Process $Exe -ArgumentList ($Win + $extra + @('-logFile', "`"$f`"")) -PassThru
  return @{ p = $p; log = $f }
}
function Stop-Game($g) { if (-not $g.p.HasExited) { $g.p.CloseMainWindow() | Out-Null; if (-not $g.p.WaitForExit(8000)) { $g.p.Kill() } } }
function Grep($g, $pat) { if (Test-Path $g.log) { Select-String -Path $g.log -Pattern $pat | ForEach-Object { $_.Line } } }
function Record($name, $pass, $detail) { $script:results += [pscustomobject]@{ Escenario = $name; Resultado = $(if ($pass) { 'PASS' } else { 'FAIL' }); Detalle = $detail }; Write-Host ("{0,-22} {1}  {2}" -f $name, $(if ($pass) { 'PASS' } else { 'FAIL' }), $detail) }

for ($r = 1; $r -le $Runs; $r++) {
  if ($Scenario -in 'coop','all') {
    # Dos jugadores reales (anfitrion + cliente), cada uno con su autopiloto, cooperando hasta la extraccion.
    $h = Start-Game "coop_host_$r" @('-ueHost','-ueListen','127.0.0.1','-uePort','7777','-ueAutopilot','drag','-ueAutopilotDelay','6','-ueQuitOnEnd')
    Start-Sleep 3
    $c = Start-Game "coop_client_$r" @('-ueClient','127.0.0.1','-uePort','7777','-ueAutopilot','drag','-ueAutopilotDelay','3','-ueQuitOnEnd')
    $okC = $c.p.WaitForExit(300000); $okH = $h.p.WaitForExit(20000); Stop-Game $c; Stop-Game $h
    $hs = (Grep $h '\[Round\] SUCCESS in') | Select-Object -First 1
    $cs = (Grep $c 'Autopilot\] build: ronda Success') | Select-Object -First 1
    $rtt = (Grep $h 'NetDiag\] anfitrion: jugadores=2 c1=(\d+)ms') | ForEach-Object { [int]([regex]::Match($_, 'c1=(\d+)ms').Groups[1].Value) }
    $rttTxt = if ($rtt) { "rtt anfitrion->cliente {0}-{1} ms" -f ($rtt | Measure-Object -Minimum).Minimum, ($rtt | Measure-Object -Maximum).Maximum } else { 'rtt -' }
    $he = [regex]::Match("$hs", 'in ([\d,\.]+)s').Groups[1].Value; $ce = [regex]::Match("$cs", 'en ([\d,\.]+)s').Groups[1].Value
    $match = $hs -and $cs -and ([math]::Abs([double]($he -replace ',','.') - [double]($ce -replace ',','.')) -le 0.5)
    Record "coop #$r" ($match) "anfitrion: '$he s' cliente: '$ce s'; $rttTxt"
  }
  if ($Scenario -in 'race','all') {
    # Carrera por el mismo punto: anfitrion y cliente aprietan sobre el cinturon en el mismo instante de reloj.
    $at = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds() + 16000
    $h = Start-Game "race_host_$r" @('-ueHost','-ueListen','127.0.0.1','-uePort','7777','-ueAutopilot','race','-ueRaceAt',"$at")
    Start-Sleep 3
    $c = Start-Game "race_client_$r" @('-ueClient','127.0.0.1','-uePort','7777','-ueAutopilot','race','-ueRaceAt',"$at",'-ueAutopilotDelay','2')
    Start-Sleep 22; Stop-Game $c; Stop-Game $h
    # Orden autoritativo: nunca dos agarres vivos sobre 'belt' (capacidad 1). Un segundo create solo despues de liberar el primero.
    $seq = @(Grep $h '\[Grab\] (create .*-> belt$|release .*-> belt reason)')
    $live = 0; $maxLive = 0; foreach ($l in $seq) { if ($l -match 'create') { $live++ } else { $live-- }; if ($live -gt $maxLive) { $maxLive = $live } }
    $creates = @($seq | Where-Object { $_ -match 'create' }).Count
    $busy = (Grep $h '\[Grab\] reject .*socket=belt: SocketBusy').Count
    $rh = (Grep $h 'resultado carrera') | Select-Object -First 1; $rc = (Grep $c 'resultado carrera') | Select-Object -First 1
    $winners = @($rh, $rc) | Where-Object { $_ -match "mano izq = 'belt'" }
    Record "race #$r" (($maxLive -eq 1) -and ($busy -ge 1) -and ($winners.Count -eq 1)) "max_simultaneos=$maxLive agarres_belt=$creates rechazos=$busy ganadores=$($winners.Count) | anfitrion: $([regex]::Match("$rh",'mano izq.*').Value) | cliente: $([regex]::Match("$rc",'mano izq.*').Value)"
  }
  if ($Scenario -in 'hostleave','all') {
    # El anfitrion se va a mitad de ronda con el cliente agarrando: el cliente NO debe ganar ni simular solo; vuelve a jugar solo.
    $h = Start-Game "hostleave_host_$r" @('-ueHost','-ueListen','127.0.0.1','-uePort','7777')
    Start-Sleep 3
    $c = Start-Game "hostleave_client_$r" @('-ueClient','127.0.0.1','-uePort','7777','-ueAutopilot','drag','-ueAutopilotDelay','3')
    Start-Sleep 14; Stop-Game $h; Start-Sleep 8; Stop-Game $c
    $disc = Grep $c 'Disconnect Event|Volviendo a jugar solo'
    $soloBegin = Grep $c '\[Net\] begin Solo ok=True'
    $falseWin = Grep $c '\[Round\] SUCCESS in [0-4],'
    Record "hostleave #$r" (($disc.Count -gt 0) -and ($soloBegin.Count -gt 0) -and ($falseWin.Count -eq 0)) "desconexion=$($disc.Count -gt 0) vuelve_a_solo=$($soloBegin.Count -gt 0) victoria_falsa=$($falseWin.Count -gt 0)"
  }
  if ($Scenario -in 'clientdrop','all') {
    # El cliente se cierra mientras agarra al tio: el anfitrion libera sus agarres (cero huerfanos).
    $h = Start-Game "clientdrop_host_$r" @('-ueHost','-ueListen','127.0.0.1','-uePort','7777')
    Start-Sleep 3
    $c = Start-Game "clientdrop_client_$r" @('-ueClient','127.0.0.1','-uePort','7777','-ueAutopilot','drag','-ueAutopilotDelay','3')
    Start-Sleep 16; $c.p.Kill(); Start-Sleep 9; Stop-Game $h
    $held = (Grep $h 'NetDiag\] anfitrion: jugadores=2.*agarres=([1-9])').Count
    $after = Grep $h 'NetDiag\] anfitrion: jugadores=1 agarres=(\d+)' | Select-Object -Last 1
    $left = Grep $h 'Se fue el jugador 2'
    Record "clientdrop #$r" (($held -gt 0) -and ($left.Count -gt 0) -and ("$after" -match 'agarres=0')) "agarraba=$($held -gt 0) salida_detectada=$($left.Count -gt 0) despues='$after'"
  }
}
$results | Format-Table -AutoSize
$results | Export-Csv -NoTypeInformation -Encoding UTF8 (Join-Path $Log 'net_smoke_results.csv')
if ($results | Where-Object Resultado -eq 'FAIL') { exit 1 } else { exit 0 }
