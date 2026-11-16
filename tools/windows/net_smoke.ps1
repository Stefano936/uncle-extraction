# UNCLE EXTRACTION - pruebas de humo de red con DOS PROCESOS en la misma PC (localhost, 127.0.0.1).
# No es una prueba LAN ni de internet: registrar esos resultados por separado.
# Uso: powershell -ExecutionPolicy Bypass -File tools\windows\net_smoke.ps1 [-Scenario coop|race|repeat|netsim|hostleave|clientdrop|all] [-Runs 1]
# netsim usa la build de desarrollo (Builds\PhysicsLab_Dev): el simulador de red de NGO solo existe en editor y builds de desarrollo.
param([string]$Scenario = 'all', [int]$Runs = 1)
$ErrorActionPreference = 'Continue'
$Repo   = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$Exe    = Join-Path $Repo 'Builds\PhysicsLab\UncleExtraction_PhysicsLab.exe'
$ExeDev = Join-Path $Repo 'Builds\PhysicsLab_Dev\UncleExtraction_PhysicsLab.exe'
$Log    = Join-Path $Repo '.local-setup\logs\net'
New-Item -ItemType Directory -Force -Path $Log | Out-Null
if (-not (Test-Path $Exe)) { Write-Host "Falta la build: $Exe" -ForegroundColor Red; exit 2 }
$Win = @('-screen-fullscreen','0','-screen-width','800','-screen-height','450')
$results = @()

function Start-Game($name, [string[]]$extra, [string]$exe = $Exe) {
  $f = Join-Path $Log "$name.log"; if (Test-Path $f) { Remove-Item $f }
  $p = Start-Process $exe -ArgumentList ($Win + $extra + @('-logFile', "`"$f`"")) -PassThru
  return @{ p = $p; log = $f; name = $name }
}
function Stop-Game($g) { if (-not $g.p.HasExited) { $g.p.CloseMainWindow() | Out-Null; if (-not $g.p.WaitForExit(8000)) { $g.p.Kill() } } }
function Grep($g, $pat) { if (Test-Path $g.log) { Select-String -Path $g.log -Pattern $pat | ForEach-Object { $_.Line } } }
function Crashed($g) { (Grep $g 'Crash!!!').Count -gt 0 }
function Record($name, $state, $detail) { $script:results += [pscustomobject]@{ Escenario = $name; Resultado = $state; Detalle = $detail }; Write-Host ("{0,-16} {1,-8} {2}" -f $name, $state, $detail) }
function Final($g) { (Grep $g '\[NetDiag\] final:') | Select-Object -First 1 }  # el primer final: lo que vio cada proceso al terminar la ronda en red
function Field($line, $k) { [regex]::Match("$line", "$k=(\S+)").Groups[1].Value }

# Cooperativo hasta la extraccion real (camioneta, cierre). Devuelve $null si un proceso crasheo al arrancar.
function Coop($tag, [string[]]$hostExtra, [string[]]$clientExtra, [string]$exe = $Exe, [int]$timeout = 330) {
  $h = Start-Game "${tag}_host" (@('-ueHost','-ueListen','127.0.0.1','-uePort','7777','-ueAutopilot','drag','-ueAutopilotDelay','6','-ueQuitOnEnd') + $hostExtra) $exe
  Start-Sleep 4  # arranque escalonado: dos inicializaciones D3D simultaneas ya provocaron un crash del driver
  $c = Start-Game "${tag}_client" (@('-ueClient','127.0.0.1','-uePort','7777','-ueAutopilot','drag','-ueAutopilotDelay','3','-ueQuitOnEnd') + $clientExtra) $exe
  $null = $c.p.WaitForExit($timeout * 1000); $null = $h.p.WaitForExit(25000); Stop-Game $c; Stop-Game $h
  if ((Crashed $h) -or (Crashed $c)) { return $null }
  return @{ h = $h; c = $c }
}
function CompareFinal($r) {
  $fh = Final $r.h; $fc = Final $r.c
  $same = $fh -and $fc -and ((Field $fh 'ronda') -eq (Field $fc 'ronda')) -and ((Field $fh 'asegurado') -eq (Field $fc 'asegurado')) -and ((Field $fh 'tio') -eq (Field $fc 'tio')) -and (((Field $fh 'puertas') -split '/')[0] -eq ((Field $fc 'puertas') -split '/')[0])
  $win = ((Field $fh 'ronda') -eq 'Success') -and ((Field $fh 'asegurado') -eq 'True')
  return @{ ok = ($same -and $win); txt = "anfitrion[$(Field $fh 'ronda') t=$(Field $fh 't') puertas=$(Field $fh 'puertas') tio=$(Field $fh 'tio')] cliente[$(Field $fc 'ronda') t=$(Field $fc 't') puertas=$(Field $fc 'puertas') tio=$(Field $fc 'tio')]" }
}
function RunCoop($label, $tag, [string[]]$hostExtra, [string[]]$clientExtra, [string]$exe = $Exe) {
  $r = Coop $tag $hostExtra $clientExtra $exe
  if ($null -eq $r) { Write-Host "$label : un proceso crasheo al arrancar (log conservado en $Log\${tag}_*.log). Reintento una vez." -ForegroundColor Yellow; $r = Coop "${tag}_retry" $hostExtra $clientExtra $exe }
  if ($null -eq $r) { Record $label 'BLOCKED' "crash al arrancar dos veces; ver $Log\${tag}*_*.log"; return $null }
  return $r
}
function RttRange($g, $pat) {
  $v = (Grep $g $pat) | ForEach-Object { [int]([regex]::Match($_, $pat).Groups[1].Value) }
  if ($v) { return "{0}-{1} ms" -f ($v | Measure-Object -Minimum).Minimum, ($v | Measure-Object -Maximum).Maximum } else { return '-' }
}

for ($run = 1; $run -le $Runs; $run++) {
  if ($Scenario -in 'coop','all') {
    $r = RunCoop "coop #$run" "coop_$run" @() @()
    if ($r) { $cmp = CompareFinal $r; Record "coop #$run" $(if ($cmp.ok) { 'PASS' } else { 'FAIL' }) "$($cmp.txt); rtt anfitrion $(RttRange $r.h 'c1=(\d+)ms')" }
  }
  if ($Scenario -in 'repeat','all') {
    # Ganar, reiniciar (decide el anfitrion) y volver a ganar; los dos procesos deben ver los mismos reinicios.
    $r = RunCoop "repeat #$run" "repeat_$run" @('-ueRepeat','1') @('-ueRepeat','1')
    if ($r) { $cmp = CompareFinal $r; $wins = (Grep $r.h '\[Round\] SUCCESS').Count
      $fh = (Grep $r.h '\[NetDiag\] final:') | Select-Object -Last 1; $fc = (Grep $r.c '\[NetDiag\] final:') | Select-Object -Last 1
      Record "repeat #$run" $(if ($cmp.ok -and $wins -ge 2 -and (Field $fh 'reinicios') -eq '1' -and (Field $fc 'reinicios') -eq '1') { 'PASS' } else { 'FAIL' }) "victorias=$wins reinicios anfitrion=$(Field $fh 'reinicios') cliente=$(Field $fc 'reinicios'); $($cmp.txt)" }
  }
  if ($Scenario -in 'netsim','all') {
    if (-not (Test-Path $ExeDev)) { Record "netsim #$run" 'NOT_RUN' "falta la build de desarrollo $ExeDev" }
    else {
      # Red degradada simulada en el CLIENTE sobre localhost: 120 ms +-20 ms, 3% de perdida (no es LAN ni internet).
      $r = RunCoop "netsim #$run" "netsim_$run" @() @('-ueNetSim','120','20','3') $ExeDev
      if ($r) { $cmp = CompareFinal $r; $sim = (Grep $r.c 'simulador de red') | Select-Object -First 1
        $simTxt = [regex]::Match("$sim", 'simulador de red: [^\(]*').Value; $rttC = RttRange $r.c 'rtt=(\d+)ms'
        $ok = $cmp.ok -and ("$sim" -match 'activo=True')
        Record "netsim #$run" $(if ($ok) { 'PASS' } else { 'FAIL' }) ($cmp.txt + "; rtt cliente " + $rttC + "; " + $simTxt) }
    }
  }
  if ($Scenario -in 'race','all') {
    $at = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds() + 16000
    $h = Start-Game "race_host_$run" @('-ueHost','-ueListen','127.0.0.1','-uePort','7777','-ueAutopilot','race','-ueRaceAt',"$at")
    Start-Sleep 4
    $c = Start-Game "race_client_$run" @('-ueClient','127.0.0.1','-uePort','7777','-ueAutopilot','race','-ueRaceAt',"$at",'-ueAutopilotDelay','2')
    Start-Sleep 21; Stop-Game $c; Stop-Game $h
    $seq = @(Grep $h '\[Grab\] (create .*-> belt$|release .*-> belt reason)')
    $live = 0; $maxLive = 0; foreach ($l in $seq) { if ($l -match 'create') { $live++ } else { $live-- }; if ($live -gt $maxLive) { $maxLive = $live } }
    $busy = (Grep $h '\[Grab\] reject .*socket=belt: SocketBusy').Count
    $rh = (Grep $h 'resultado carrera') | Select-Object -First 1; $rc = (Grep $c 'resultado carrera') | Select-Object -First 1
    $winners = @($rh, $rc) | Where-Object { $_ -match "mano izq = 'belt'" }
    $state = if ((Crashed $h) -or (Crashed $c)) { 'BLOCKED' } elseif (($maxLive -eq 1) -and ($busy -ge 1) -and ($winners.Count -eq 1)) { 'PASS' } else { 'FAIL' }
    Record "race #$run" $state "max_simultaneos=$maxLive rechazos=$busy ganadores=$($winners.Count)"
  }
  if ($Scenario -in 'hostleave','all') {
    # El anfitrion se va con el cliente agarrando: el cliente no simula ni gana por su cuenta y vuelve al menu.
    $h = Start-Game "hostleave_host_$run" @('-ueHost','-ueListen','127.0.0.1','-uePort','7777')
    Start-Sleep 4
    $c = Start-Game "hostleave_client_$run" @('-ueClient','127.0.0.1','-uePort','7777','-ueAutopilot','drag','-ueAutopilotDelay','3')
    Start-Sleep 14; Stop-Game $h; Start-Sleep 9; Stop-Game $c
    $disc = Grep $c 'Disconnect Event|Volviendo al menu'
    $falseWin = Grep $c '\[Round\] SUCCESS'
    Record "hostleave #$run" $(if (($disc.Count -gt 0) -and ($falseWin.Count -eq 0)) { 'PASS' } else { 'FAIL' }) "desconexion=$($disc.Count -gt 0) victoria_falsa=$($falseWin.Count -gt 0)"
  }
  if ($Scenario -in 'clientdrop','all') {
    # El cliente se cierra mientras agarra al tio: el anfitrion libera sus agarres (cero huerfanos).
    $h = Start-Game "clientdrop_host_$run" @('-ueHost','-ueListen','127.0.0.1','-uePort','7777')
    Start-Sleep 4
    $c = Start-Game "clientdrop_client_$run" @('-ueClient','127.0.0.1','-uePort','7777','-ueAutopilot','drag','-ueAutopilotDelay','3')
    Start-Sleep 16; $c.p.Kill(); Start-Sleep 9; Stop-Game $h
    $held = (Grep $h 'NetDiag\] anfitrion: jugadores=2.*agarres=([1-9])').Count
    $after = Grep $h 'NetDiag\] anfitrion: jugadores=1 agarres=(\d+)' | Select-Object -Last 1
    $left = Grep $h 'Se fue el jugador 2'
    Record "clientdrop #$run" $(if (($held -gt 0) -and ($left.Count -gt 0) -and ("$after" -match 'agarres=0')) { 'PASS' } else { 'FAIL' }) "agarraba=$($held -gt 0) salida_detectada=$($left.Count -gt 0) despues=$([regex]::Match("$after",'agarres=\d+').Value)"
  }
}
$results | Format-Table -AutoSize | Out-String -Width 240
$results | Export-Csv -NoTypeInformation -Encoding UTF8 (Join-Path $Log 'net_smoke_results.csv')
if ($results | Where-Object Resultado -eq 'FAIL') { exit 1 } else { exit 0 }
