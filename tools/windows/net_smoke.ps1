# UNCLE EXTRACTION - pruebas de humo de red con DOS PROCESOS en la misma PC (localhost, 127.0.0.1).
# No es una prueba LAN ni de internet: registrar esos resultados por separado.
# Uso: powershell -ExecutionPolicy Bypass -File tools\windows\net_smoke.ps1 [-Scenario <lista separada por comas>|all] [-Scene Bar_LastRound|PhysicsLab] [-Runs 1]
#   Escenarios: coop, repeat, lose, netsim, race, hostleave, clientdrop, loaddrop, closedrop, party3, party4, latejoin,
#   guesttoggle(_sim), marks, grace, graceend (sesion 14: sondas con mouse/teclado virtuales y desconexion con gracia)
#   party3/party4: un anfitrion y 2/3 clientes en procesos separados (sala, inicio con todos listos, ronda completa).
#   latejoin: un cliente que llega con la ronda iniciada es rechazado con motivo (sin incorporacion en curso).
#   netsim usa la build de desarrollo: el simulador de red de NGO solo existe en editor y builds de desarrollo.
# Codigo de salida: 0 = TODOS los escenarios pedidos dieron PASS; 1 = hubo algun FAIL; 2 = ejecucion incompleta
#   (algun BLOCKED o NOT_RUN, o no se ejecuto nada). Un 0 nunca encubre escenarios sin correr.
param([string]$Scenario = 'all', [string]$Scene = 'Bar_LastRound', [int]$Runs = 1, [switch]$NoPreflight)
$ErrorActionPreference = 'Continue'
$Repo   = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$Exe    = Join-Path $Repo 'Builds\UncleExtraction\UncleExtraction.exe'
$ExeDev = Join-Path $Repo 'Builds\UncleExtraction_Dev\UncleExtraction.exe'
$RefExe = Join-Path $Repo 'Builds\UncleExtraction_Ref_s16\UncleExtraction.exe' # compatibilidad: demo anterior (sesion 17)
$Log    = Join-Path $Repo '.local-setup\logs\net'
New-Item -ItemType Directory -Force -Path $Log | Out-Null
if (-not (Test-Path $Exe)) { Write-Host "Falta la build: $Exe" -ForegroundColor Red; exit 2 }
$Win = @('-screen-fullscreen','0','-screen-width','800','-screen-height','450')
$All = @('coop','repeat','lose','netsim','race','hostleave','clientdrop','loaddrop','closedrop','party3','party4','latejoin','guesttoggle','guesttoggle_sim','marks','grace','graceend','cycle','compat','race_natural')
# QA ampliada (mas larga; -Scenario qa): dificultades y grupos, sesion larga, perdida brusca del anfitrion, rendimiento.
# 'relay' va aparte: necesita el proyecto vinculado a Unity Cloud y prueba Relay en UNA PC.
$Qa = @('hostkill','solo3','matrix','long','perf1','perf4','perf1080','perf1080low')
$Wanted = if ($Scenario -eq 'all') { $All } elseif ($Scenario -eq 'qa') { $Qa } else { $Scenario -split ',' | ForEach-Object { $_.Trim() } }
$results = @()
# Guardia (sesion 12): sintaxis y VERSION.txt al dia, una sola tanda a la vez, y al cancelar se cierran SOLO sus procesos
Import-Module (Join-Path $PSScriptRoot 'Guard.psm1') -Force
if (-not $NoPreflight) { try { Assert-Preflight -Builds @('UncleExtraction','UncleExtraction_Dev') } catch { Write-Host $_ -ForegroundColor Red; exit 2 } }
try { Enter-BatchLock 'net_smoke' } catch { Write-Host $_ -ForegroundColor Red; exit 2 }
# Sesion 17: NO se cierran procesos ajenos (un juego abierto a mano, otra copia). Esta tanda solo detiene los que lanzo
# (Start-Tracked + finally). Si el puerto 7777 esta ocupado por otro, se usa el siguiente UDP libre y se informa.
$others = @(Get-Process UncleExtraction -ErrorAction SilentlyContinue)
if ($others.Count) { Write-Host "Aviso: hay $($others.Count) proceso(s) UncleExtraction ajenos a esta tanda; no se tocan" }
$Port = 7777
$busy = @(Get-NetUDPEndpoint -ErrorAction SilentlyContinue | ForEach-Object { [int]$_.LocalPort })
while ($busy -contains $Port) { $Port++ }
if ($Port -ne 7777) { Write-Host "Puerto 7777 ocupado: la tanda usa $Port" }
Start-Sleep 1

function Start-Game($name, [string[]]$extra, [string]$exe = $Exe) {
  $f = Join-Path $Log "$name.log"; if (Test-Path $f) { Remove-Item $f }
  $p = Start-Process $exe -ArgumentList ($Win + @('-ueScene', $Scene) + $extra + @('-logFile', "`"$f`"")) -PassThru
  Register-Tracked $p
  return @{ p = $p; log = $f; name = $name }
}
function Stop-Game($g) { if (-not $g.p.HasExited) { $g.p.CloseMainWindow() | Out-Null; if (-not $g.p.WaitForExit(8000)) { $g.p.Kill() } } }
function Grep($g, $pat) { if (Test-Path $g.log) { Select-String -Path $g.log -Pattern $pat | ForEach-Object { $_.Line } } }
function Crashed($g) { (Grep $g 'Crash!!!').Count -gt 0 }
# Version de la build realmente usada (VERSION.txt lo escribe BuildTools con el commit de origen)
function BuildVer($exe) { $v = Join-Path (Split-Path -Parent $exe) 'VERSION.txt'; if (Test-Path $v) { ((Get-Content $v -Raw) -replace '\s+', ' ').Trim() } else { 'sin VERSION.txt' } }
$BuildTag = BuildVer $Exe; $BuildTagDev = BuildVer $ExeDev; $Stamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
Write-Host "build: $BuildTag | dev: $BuildTagDev"
function Record($name, $state, $detail) { $script:results += [pscustomobject]@{ Fecha = $Stamp; Escenario = $name; Escena = $Scene; Resultado = $state; Build = $(if ($name -like 'netsim*') { $BuildTagDev } else { $BuildTag }); Detalle = $detail }; Write-Host ("{0,-16} {1,-8} {2}" -f $name, $state, $detail) }
function Finals($g) { @(Grep $g '\[NetDiag\] final:') }
function Field($line, $k) { [regex]::Match("$line", "$k=(\S+)").Groups[1].Value }
function Last($g, $pat) { (Grep $g $pat) | Select-Object -Last 1 }
function RttRange($g, $pat) {
  $v = (Grep $g $pat) | ForEach-Object { [int]([regex]::Match($_, $pat).Groups[1].Value) }
  if ($v) { return "{0}-{1} ms" -f ($v | Measure-Object -Minimum).Minimum, ($v | Measure-Object -Maximum).Maximum } else { return '-' }
}
function HostArgs([string[]]$x, [int]$expect = 2) { @('-ueHost','-ueListen','127.0.0.1','-uePort',"$Port",'-ueExpectPlayers',"$expect") + $x }
function ClientArgs([string[]]$x) { @('-ueClient','127.0.0.1','-uePort',"$Port") + $x }

# Dos procesos: anfitrion y cliente. Devuelve $null si alguno crasheo al arrancar (registro conservado).
function Pair($tag, [string[]]$h, [string[]]$c, [int]$timeout, [string]$exe = $Exe, [switch]$waitHost) {
  $hostG = Start-Game "${tag}_host" (HostArgs $h) $exe
  Start-Sleep 4   # arranque escalonado: dos inicializaciones D3D simultaneas provocaron un crash del driver NVIDIA
  $cliG = Start-Game "${tag}_client" (ClientArgs $c) $exe
  if ($waitHost) { $null = $hostG.p.WaitForExit($timeout * 1000); $null = $cliG.p.WaitForExit(15000) }
  else { $null = $cliG.p.WaitForExit($timeout * 1000); $null = $hostG.p.WaitForExit(25000) }
  Stop-Game $cliG; Stop-Game $hostG
  if ((Crashed $hostG) -or (Crashed $cliG)) { return $null }
  if ((Grep $hostG 'begin Host ok=False').Count -gt 0) { Record "$tag" 'BLOCKED' "el anfitrion no pudo escuchar en $Port (puerto ocupado)"; return 'blocked' }
  return @{ h = $hostG; c = $cliG }
}
# Sesion 17: evidencia de disputa por el cinturon en el registro del anfitrion, atada a un intento concreto.
function RaceEvidence($g) {
  $seq = @(Grep $g '\[Grab\] (create #\d+ .+ -> belt$|release #\d+ .+ -> belt reason|reject .+ socket=\S+: SocketBusy)')
  $live = 0; $maxLive = 0; $holder = $null; $tied = $false; $busy = 0; $other = 0; $players = @{}
  foreach ($l in $seq) {
    if ($l -match 'create #(\d+) (.+)/[LR] -> belt$') { $live++; $holder = $Matches[2]; $players[$holder] = 1 }
    elseif ($l -match 'release #\d+ (.+)/[LR] -> belt reason') { $live--; $holder = $null }
    elseif ($l -match 'reject (.+) hand=\d socket=(\S+): SocketBusy') {
      $who = $Matches[1]; $players[$who] = 1
      if ($Matches[2] -ne 'belt') { $other++ } else { $busy++; if ($holder -and $who -ne $holder) { $tied = $true } }
    }
    if ($live -gt $maxLive) { $maxLive = $live }
  }
  $why = @(); if ($players.Count -lt 2) { $why += "falta un participante en el anfitrion ($($players.Keys -join ','))" }
  if (-not $tied) { $why += 'no hubo SocketBusy de otro jugador mientras el primero tenia el cinturon' }
  if ($maxLive -gt 1) { $why += "dos agarres a la vez ($maxLive)" }
  if ($other -gt 0 -and $busy -eq 0) { $why += 'el rechazo fue en otro punto' }
  [pscustomobject]@{ ok = ($why.Count -eq 0); why = ($why -join '; '); maxLive = $maxLive; busy = $busy
    text = "max_simultaneos=$maxLive rechazos_cinturon=$busy rechazos_otro_punto=$other participantes=$($players.Count) disputa_atada=$tied" }
}
function PairRetry($label, $tag, [string[]]$h, [string[]]$c, [int]$timeout, [string]$exe = $Exe, [switch]$waitHost) {
  $r = Pair $tag $h $c $timeout $exe -waitHost:$waitHost
  if ($r -eq 'blocked') { return $null }
  if ($null -eq $r) { Write-Host "$label : crash al arrancar (registro en $Log\${tag}_*.log); reintento una vez" -ForegroundColor Yellow; $r = Pair "${tag}_retry" $h $c $timeout $exe -waitHost:$waitHost }
  if ($null -eq $r) { Record $label 'BLOCKED' "crash al arrancar dos veces; ver $Log\${tag}*_*.log" }
  return $r
}
# El estado final lo registra cada proceso al converger (LabAutopilot): resultado, tio asegurado y puertas coherentes entre
# si dentro del MISMO intento (reinicios), con tiempo maximo. "convergencia=TIMEOUT(...) faltan=..." = no convergio.
function Converged($l) { $c = Field $l 'convergencia'; $c -ne '' -and $c -notmatch 'TIMEOUT|reinicio' }
# N procesos (anfitrion + N-1 clientes), arranque escalonado; devuelve los juegos o $null si alguno crasheo.
function Party($tag, [int]$n, [int]$timeout, [string[]]$hx = @(), [string[]]$ax = @()) {
  $games = @(); $games += Start-Game "${tag}_host" (HostArgs (@('-ueAutopilot','drag','-ueQuitOnEnd') + $hx + $ax) $n)
  for ($k = 1; $k -lt $n; $k++) { Start-Sleep 4; $games += Start-Game "${tag}_client$k" (ClientArgs (@('-ueAutopilot','drag','-ueAutopilotDelay','3','-ueQuitOnEnd') + $ax)) }
  $deadline = (Get-Date).AddSeconds($timeout)
  foreach ($g in $games) { $left = [int][Math]::Max(1, ($deadline - (Get-Date)).TotalMilliseconds); $null = $g.p.WaitForExit($left) }
  foreach ($g in $games) { Stop-Game $g }
  if (@($games | Where-Object { Crashed $_ }).Count -gt 0) { return $null }
  return $games
}

# Un proceso solo (anfitrion en loopback, como «Jugar solo»).
function Solo($tag, [string[]]$x, [int]$timeout) {
  $g = Start-Game "${tag}_solo" (@('-ueAutopilot','drag','-ueQuitOnEnd') + $x)
  $null = $g.p.WaitForExit($timeout * 1000); Stop-Game $g
  if (Crashed $g) { return $null }; return $g
}
function PerfLine($g) { $l = Last $g '\[Perf\] resumen'; if (-not $l) { return 'sin [Perf] resumen' }
  "fps=$(Field $l 'fps_medio') p95=$(Field $l 'p95')ms p99=$(Field $l 'p99')ms lentos=$(Field $l 'cuadros_>33ms') cpu=$(Field $l 'media')ms gpu=$(([regex]::Match("$l", 'gpu_ms media=(\S+)')).Groups[1].Value)ms mem_unity=$(Field $l 'mem_unity_inicio')->$(Field $l 'final') reservada_pico=$(Field $l 'reservada_pico')" }
function SameFinal($a, $b) {
  $a -and $b -and (Converged $a) -and (Converged $b) -and ((Field $a 'reinicios') -eq (Field $b 'reinicios')) -and ((Field $a 'ronda') -eq (Field $b 'ronda')) -and ((Field $a 'asegurado') -eq (Field $b 'asegurado')) -and ((Field $a 'tio') -eq (Field $b 'tio')) -and (((Field $a 'puertas') -split '/')[0] -eq ((Field $b 'puertas') -split '/')[0])
}
function Brief($l) { "$(Field $l 'ronda') t=$(Field $l 't') intento=$(Field $l 'reinicios') puertas=$(Field $l 'puertas') tio=$(Field $l 'tio') asegurado=$(Field $l 'asegurado') escena=$(Field $l 'escena') conv=$(Field $l 'convergencia')$(if ((Field $l 'faltan') -ne '') { ' faltan=' + (Field $l 'faltan') })" }

try {
for ($run = 1; $run -le $Runs; $run++) {
  foreach ($sc in $Wanted) {
    $label = "$sc #$run"; $tag = "${sc}_${Scene}_$run"
    switch ($sc) {
      'relay' {
        # Internet por Relay en UNA PC: prueba el proveedor y el codigo, no dos equipos ni dos redes (eso es manual).
        $hostG = Start-Game "${tag}_host" @('-ueHost','-ueRelay','-ueProfile','uehost','-ueExpectPlayers','2','-ueAutopilot','drag','-ueAutopilotDelay','6','-ueQuitOnEnd')
        $code = ''; $fail = ''; $deadline = (Get-Date).AddSeconds(60)
        while ((Get-Date) -lt $deadline -and $code -eq '' -and $fail -eq '' -and -not $hostG.p.HasExited) {
          Start-Sleep 1
          $l = Last $hostG 'transporte=relay codigo='; if ($l) { $code = Field $l 'codigo' }
          $f = Last $hostG 'internet no disponible: '; if ($f) { $fail = ([regex]::Match($f, 'internet no disponible: (\S+)')).Groups[1].Value }
        }
        if ($code -eq '') {
          Stop-Game $hostG
          $why = if ($fail -eq 'error.online_unlinked') { 'proyecto sin vincular a Unity Cloud (Application.cloudProjectId vacio)' } elseif ($fail) { "servicio: $fail" } else { 'sin codigo en 60 s' }
          Record $label 'BLOCKED' "Relay no disponible: $why"; break
        }
        $cliG = Start-Game "${tag}_client" @('-ueJoinCode',$code,'-ueProfile','ueclient','-ueAutopilot','drag','-ueAutopilotDelay','3','-ueQuitOnEnd')
        $null = $cliG.p.WaitForExit(330000); $null = $hostG.p.WaitForExit(25000); Stop-Game $cliG; Stop-Game $hostG
        $fh = @(Finals $hostG)[0]; $fc = @(Finals $cliG)[0]
        $viaRelay = (Grep $cliG 'transporte=relay').Count -gt 0
        $ok = $viaRelay -and (SameFinal $fh $fc) -and (Field $fh 'ronda') -eq 'Success'
        Record $label $(if ($ok) { 'PASS' } else { 'FAIL' }) "codigo=$code relay_cliente=$viaRelay anfitrion[$(Brief $fh)] cliente[$(Brief $fc)] rtt $(RttRange $hostG 'c1=(\d+)ms') (una PC: no prueba dos equipos ni dos redes)"
      }
      'hostkill' {
        # Perdida BRUSCA del anfitrion (proceso terminado, sin aviso) con el cliente agarrando: el cliente detecta la
        # caida, muestra el motivo y vuelve al menu. Mide cuanto tarda en enterarse (tiempo de desconexion del transporte).
        $hostG = Start-Game "${tag}_host" (HostArgs @()); Start-Sleep 4
        $cliG = Start-Game "${tag}_client" (ClientArgs @('-ueAutopilot','drag','-ueAutopilotDelay','3'))
        Start-Sleep 18
        $held = (Grep $hostG 'NetDiag\] anfitrion: jugadores=2.*agarres=[1-9]').Count -gt 0
        $killedAt = Get-Date; $hostG.p.Kill()
        $seen = $null; while (((Get-Date) - $killedAt).TotalSeconds -lt 60) { if ((Grep $cliG 'error.host_left|Disconnect Event').Count -gt 0) { $seen = ((Get-Date) - $killedAt).TotalSeconds; break }; Start-Sleep -Milliseconds 250 }
        Start-Sleep 6; Stop-Game $cliG
        if (Crashed $cliG) { Record $label 'FAIL' "el cliente crasheo al perder el anfitrion; ver $($cliG.log)"; break }
        $menu = (Grep $cliG '\[Menu\] en el menu').Count -ge 2; $exc = (Grep $cliG 'Exception').Count; $fake = (Grep $cliG '\[Round\] SUCCESS').Count -gt 0
        $state = if (-not $held) { 'NOT_RUN' } elseif ($seen -and $menu -and $exc -eq 0 -and -not $fake) { 'PASS' } else { 'FAIL' }
        Record $label $state "agarraba=$held detecta_en=$(if ($seen) { '{0:0.0}s' -f $seen } else { 'no (60 s)' }) vuelve_al_menu=$menu excepciones=$exc victoria_falsa=$fake"
      }
      'solo3' {
        foreach ($d in 'Relaxed','Normal','Hard') {
          $g = Solo "${tag}_$d" @('-ueDifficulty',$d) 330
          if ($null -eq $g) { Record "$label $d" 'BLOCKED' "crash al arrancar"; continue }
          $f = @(Finals $g)[0]
          Record "$label $d" $(if ((Converged $f) -and (Field $f 'ronda') -eq 'Success' -and "$f" -match "dificultad=$d") { 'PASS' } elseif (-not $f) { 'NOT_RUN' } else { 'FAIL' }) "1 jugador [$(Brief $f)] dificultad=$(Field $f 'dificultad')"
        }
      }
      'matrix' {
        # Normal con 2-4 ya lo cubren coop/party3/party4; aca Relajado y Dificil con 2, 3 y 4 procesos.
        foreach ($d in 'Relaxed','Hard') { foreach ($n in 2,3,4) {
          $g = Party "${tag}_${d}_$n" $n 480 @('-ueDifficulty',$d)
          if ($null -eq $g) { Record "$label $d x$n" 'BLOCKED' "crash al arrancar algun proceso"; continue }
          $finals = @($g | ForEach-Object { @(Finals $_)[0] }); $started = Last $g[0] 'sala: inicia la ronda con'
          $allSame = $true; foreach ($f in $finals) { if (-not (SameFinal $finals[0] $f)) { $allSame = $false } }
          $ok = $allSame -and (Field $finals[0] 'ronda') -eq 'Success' -and ("$started" -match "con $n jugadores") -and "$($finals[0])" -match "dificultad=$d"
          Record "$label $d x$n" $(if ($ok) { 'PASS' } elseif (-not $started) { 'NOT_RUN' } else { 'FAIL' }) ("procesos=$n " + (($finals | ForEach-Object { "[" + (Brief $_) + " dif=" + (Field $_ 'dificultad') + "]" }) -join ' '))
        } }
      }
      'long' {
        # Sesion larga: 1 + 7 reintentos seguidos en solitario, con medicion de memoria por ventana.
        $g = Solo $tag @('-ueRepeat','7','-uePerf','15') 1500
        if ($null -eq $g) { Record $label 'BLOCKED' 'crash al arrancar'; break }
        $fs = @(Finals $g); $wins = @($fs | Where-Object { (Field $_ 'ronda') -eq 'Success' }).Count; $exc = (Grep $g 'Exception').Count
        $mem = @(Grep $g '\[Perf\] ventana' | ForEach-Object { [int]([regex]::Match($_, 'mem_unity=(\d+)MB').Groups[1].Value) })
        $growth = if ($mem.Count -ge 4) { $mem[-1] - $mem[2] } else { 0 }
        $ok = $fs.Count -ge 8 -and $wins -eq $fs.Count -and $exc -eq 0 -and $growth -lt 64
        Record $label $(if ($ok) { 'PASS' } else { 'FAIL' }) "rondas=$($fs.Count) victorias=$wins excepciones=$exc mem_unity_ventanas=$($mem -join ',') crecimiento=$growth MB $(PerfLine $g)"
      }
      'perf1' {
        $g = Solo $tag @('-uePerf','10') 330
        if ($null -eq $g) { Record $label 'BLOCKED' 'crash al arrancar'; break }
        $f = @(Finals $g)[0]
        Record $label $(if ((Field $f 'ronda') -eq 'Success') { 'PASS' } else { 'FAIL' }) "1 proceso [$(Brief $f)] $(PerfLine $g) (esta PC)"
      }
      'perf4' {
        $g = Party $tag 4 480 @() @('-uePerf','10')
        if ($null -eq $g) { Record $label 'BLOCKED' 'crash al arrancar algun proceso'; break }
        $f = @(Finals $g[0])[0]
        $detail = "4 procesos en UNA PC (compiten por CPU/GPU; no equivale a 4 equipos) [$(Brief $f)] " + ((0..3 | ForEach-Object { "p$($_): " + (PerfLine $g[$_]) }) -join ' | ')
        Record $label $(if ((Field $f 'ronda') -eq 'Success') { 'PASS' } else { 'FAIL' }) $detail
      }
      { $_ -in 'perf1080','perf1080low' } {
        # Un proceso a 1920x1080 sin bordes (pantalla completa del monitor de esta PC), con sincronizacion vertical.
        # perf1080low: calidad grafica Baja (-ueQuality low). Se registra la GPU que uso Windows (equipos con dos GPU).
        $q = if ($sc -eq 'perf1080low') { @('-ueQuality','low') } else { @('-ueQuality','high') }
        $f0 = Join-Path $Log "${tag}_solo.log"; if (Test-Path $f0) { Remove-Item $f0 }
        $p = Start-Process $Exe -ArgumentList (@('-screen-fullscreen','0','-popupwindow','-screen-width','1920','-screen-height','1080','-ueScene',$Scene,'-ueAutopilot','drag','-ueQuitOnEnd','-uePerf','10') + $q + @('-logFile',"`"$f0`"")) -PassThru; Register-Tracked $p
        $null = $p.WaitForExit(330000); $g = @{ p = $p; log = $f0; name = "${tag}_solo" }; Stop-Game $g
        if (Crashed $g) { Record $label 'BLOCKED' 'crash al arrancar'; break }
        $f = @(Finals $g)[0]; $eq = Last $g '\[Perf\] equipo'
        Record $label $(if ((Field $f 'ronda') -eq 'Success') { 'PASS' } else { 'FAIL' }) "1920x1080 [$(Brief $f)] $(PerfLine $g) $([regex]::Match("$eq", 'gpu=«[^»]*»').Value) $([regex]::Match("$eq", 'pantalla=\S+ vsync=\d').Value) calidad=$($q[1]) (esta PC)"
      }
      'coop' {
        $r = PairRetry $label $tag @('-ueAutopilot','drag','-ueAutopilotDelay','6','-ueQuitOnEnd') @('-ueAutopilot','drag','-ueAutopilotDelay','3','-ueQuitOnEnd') 330
        if ($r) { $fh = @(Finals $r.h)[0]; $fc = @(Finals $r.c)[0]
          $ok = (SameFinal $fh $fc) -and (Field $fh 'ronda') -eq 'Success' -and (Field $fh 'escena') -eq $Scene
          Record $label $(if ($ok) { 'PASS' } else { 'FAIL' }) "anfitrion[$(Brief $fh)] cliente[$(Brief $fc)] rtt $(RttRange $r.h 'c1=(\d+)ms')" }
      }
      'repeat' {
        $r = PairRetry $label $tag @('-ueAutopilot','drag','-ueAutopilotDelay','6','-ueQuitOnEnd','-ueRepeat','1') @('-ueAutopilot','drag','-ueAutopilotDelay','3','-ueQuitOnEnd','-ueRepeat','1') 420
        if ($r) { $fh = @(Finals $r.h); $fc = @(Finals $r.c); $wins = (Grep $r.h '\[Round\] SUCCESS').Count
          # iniciar desde la sala ya cuenta un reinicio: el segundo intento es el primero + 1, en ambos procesos
          $i0 = [int](Field $fh[0] 'reinicios'); $ok = $wins -ge 2 -and [int](Field $fh[-1] 'reinicios') -eq ($i0 + 1) -and [int](Field $fc[-1] 'reinicios') -eq ($i0 + 1) -and (SameFinal $fh[0] $fc[0])
          Record $label $(if ($ok) { 'PASS' } else { 'FAIL' }) "victorias=$wins reinicios anfitrion=$(Field $fh[-1] 'reinicios') cliente=$(Field $fc[-1] 'reinicios') intento1 anfitrion[$(Brief $fh[0])] cliente[$(Brief $fc[0])]" }
      }
      'lose' {
        # Derrota por tiempo (limite de prueba de 10 s desde el inicio en la sala), reintento decidido por el anfitrion y regreso del cliente al menu.
        $r = PairRetry $label $tag @('-ueAutopilot','drag','-ueAutopilotDelay','6','-ueRoundTime','10','-ueRepeat','1','-ueQuitOnEnd') @('-ueAutopilot','drag','-ueAutopilotDelay','3','-ueRepeat','1','-ueMenuAtEnd') 150 -waitHost
        if ($r) { $fh = @(Finals $r.h); $fc = @(Finals $r.c)
          $lostBoth = (Field $fh[0] 'ronda') -eq 'Timeout' -and (Field $fc[0] 'ronda') -eq 'Timeout' -and (Converged $fh[0]) -and (Converged $fc[0]) -and (Field $fh[0] 'reinicios') -eq (Field $fc[0] 'reinicios')
          $retried = (Grep $r.h '\[Round\] restart #1').Count -gt 0
          $menu = (Grep $r.c '\[Menu\] en el menu').Count -ge 2   # el menu al arrancar + el regreso
          Record $label $(if ($lostBoth -and $retried -and $menu) { 'PASS' } else { 'FAIL' }) "derrota_ambos=$lostBoth reintento=$retried cliente_vuelve_al_menu=$menu anfitrion[$(Brief $fh[0])] cliente[$(Brief $fc[0])]" }
      }
      'netsim' {
        if (-not (Test-Path $ExeDev)) { Record $label 'NOT_RUN' "falta la build de desarrollo $ExeDev"; break }
        $r = PairRetry $label $tag @('-ueAutopilot','drag','-ueAutopilotDelay','6','-ueQuitOnEnd') @('-ueAutopilot','drag','-ueAutopilotDelay','3','-ueQuitOnEnd','-ueNetSim','120','20','3') 330 $ExeDev
        if ($r) { $fh = @(Finals $r.h)[0]; $fc = @(Finals $r.c)[0]; $sim = Last $r.c 'simulador de red'
          $ok = (SameFinal $fh $fc) -and (Field $fh 'ronda') -eq 'Success' -and ("$sim" -match 'activo=True')
          Record $label $(if ($ok) { 'PASS' } else { 'FAIL' }) ("anfitrion[" + (Brief $fh) + "] cliente[" + (Brief $fc) + "] rtt cliente " + (RttRange $r.c 'rtt=(\d+)ms') + " (simulado 120 ms +-20, 3% perdida)") }
      }
      'race' {
        # sesion 17: evidencia del ANFITRION atada a UN intento: un jugador crea el agarre del cinturon y, mientras lo
        # tiene (entre su create y su release), OTRO jugador distinto recibe SocketBusy en el mismo cinturon. Falla si falta
        # un participante, si el rechazo fue en otro punto, si hubo dos a la vez o si el candidato local no era el cinturon.
        $at = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds() + 17000
        $r = PairRetry $label $tag @('-ueAutopilot','race','-ueRaceAt',"$at") @('-ueAutopilot','race','-ueRaceAt',"$at",'-ueAutopilotDelay','2') 24 -waitHost
        if ($r -and -not (Test-Path $r.h.log)) { Record $label 'BLOCKED' "el anfitrion no dejo registro (salida=$($r.h.p.ExitCode) exe=$($r.h.p.StartInfo.FileName))"; break }
        if ($r) { $e = RaceEvidence $r.h
          $aimed = @((Last $r.h 'aprieta en'), (Last $r.c 'aprieta en')) | Where-Object { $_ -match 'candidato=belt' }
          $restored = (Grep $r.h 'carrera: IA del tio restaurada \(True\)').Count -gt 0
          $why = @(); if (-not $e.ok) { $why += $e.why }; if ($aimed.Count -ne 2) { $why += "candidato local distinto del cinturon en $(2 - $aimed.Count) proceso(s)" }; if (-not $restored) { $why += 'la IA del tio no se restauro' }
          Record $label $(if ($why.Count -eq 0) { 'PASS' } else { 'FAIL' }) "$($e.text) apuntaban=$($aimed.Count)/2 ia_restaurada=$restored$(if ($why) { ' :: ' + ($why -join '; ') })" }
      }
      'race_natural' {
        # sesion 17: juego normal (sin reubicar, IA del tio activa, latencia simulada en el invitado). Criterio duro: nunca dos
        # agarres a la vez en el cinturon. Metricas aparte: hubo disputa (SocketBusy), aproximaciones fallidas, ganador.
        if (-not (Test-Path $ExeDev)) { Record $label 'NOT_RUN' "falta la build de desarrollo $ExeDev"; break }
        $at = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds() + 19000
        $r = PairRetry $label $tag @('-ueAutopilot','race','-ueRaceNatural','-ueRaceAt',"$at") @('-ueAutopilot','race','-ueRaceNatural','-ueRaceAt',"$at",'-ueAutopilotDelay','2','-ueNetSim','80','15','1') 26 $ExeDev -waitHost
        if ($r) { $e = RaceEvidence $r.h
          $fails = (Grep $r.h 'aproximacion fallida').Count + (Grep $r.c 'aproximacion fallida').Count
          $ai = (Grep $r.h 'carrera: modo=natural ia_tio=True').Count -gt 0
          $natWinners = @((Last $r.h 'resultado carrera'), (Last $r.c 'resultado carrera')) | Where-Object { $_ -match "mano izq = 'belt'" }
          Record $label $(if ($e.maxLive -le 1 -and $ai) { 'PASS' } else { 'FAIL' }) "max_simultaneos=$($e.maxLive) disputa_atada=$($e.ok) rechazos=$($e.busy) ganadores=$($natWinners.Count) aproximaciones_fallidas=$fails ia_activa=$ai rtt=$(RttRange $r.c 'rtt=(\d+)ms')" }
      }
      'hostleave' {
        $hostG = Start-Game "${tag}_host" (HostArgs @()); Start-Sleep 4
        $cliG = Start-Game "${tag}_client" (ClientArgs @('-ueAutopilot','drag','-ueAutopilotDelay','3'))
        Start-Sleep 14; Stop-Game $hostG; Start-Sleep 9; Stop-Game $cliG
        if ((Crashed $hostG) -or (Crashed $cliG)) { Record $label 'BLOCKED' "crash al arrancar; ver $Log\${tag}_*.log"; break }
        $disc = (Grep $cliG 'Disconnect Event').Count -gt 0; $menu = (Grep $cliG '\[Menu\] en el menu').Count -ge 2; $fake = (Grep $cliG '\[Round\] SUCCESS').Count -gt 0
        Record $label $(if ($disc -and $menu -and -not $fake) { 'PASS' } else { 'FAIL' }) "desconexion=$disc vuelve_al_menu=$menu victoria_falsa=$fake"
      }
      { $_ -in 'guesttoggle','guesttoggle_sim' } {
        # Invitado con agarre ALTERNADO y mouse/teclado virtuales (mismo camino que la entrada real): un clic agarra, otro
        # suelta, Q con el boton sostenido, pausa con el boton sostenido, pedido sin objetivo que se desarma. El anfitrion
        # acomoda al invitado frente al tio (-ueProbeStage). _sim: build de desarrollo con 150 ms +-30 de latencia.
        $sim = $sc -eq 'guesttoggle_sim'; $exe = if ($sim) { $ExeDev } else { $Exe }
        if ($sim -and -not (Test-Path $ExeDev)) { Record $label 'NOT_RUN' "falta la build de desarrollo $ExeDev"; break }
        $simArgs = if ($sim) { @('-ueNetSim','150','30','0') } else { @() }
        $r = PairRetry $label $tag (@('-ueProbeStage') + $simArgs) (@('-ueProbe','toggle') + $simArgs) 90 $exe
        if ($r) { $fin = Last $r.c '\[Probe\] fin toggle'; $steps = @(Grep $r.c '\[Probe\] paso=')
          $okN = ([regex]::Match("$fin", 'ok=(\d+)/(\d+)')); $pend = [regex]::Match("$((Grep $r.c 'paso=alternar_un_clic_agarra') | Select-Object -First 1)", 'pendiente_ms=(\d+)').Groups[1].Value
          $pass = $okN.Success -and $okN.Groups[1].Value -eq $okN.Groups[2].Value -and [int]$okN.Groups[2].Value -ge 5
          $bad = ($steps | Where-Object { $_ -match 'ok=False' } | ForEach-Object { [regex]::Match($_, 'paso=(\S+)').Groups[1].Value }) -join ','
          Record $label $(if ($pass) { 'PASS' } elseif (-not $fin) { 'FAIL' } else { 'FAIL' }) "sonda $($okN.Value) pendiente_hasta_confirmar=${pend}ms$(if ($bad) { " fallaron=$bad" })$(if (-not $fin) { ' (la sonda no termino)' })" }
      }
      'marks' {
        # Marcador desde el invitado: lo reciben todos con nombre/color/objetivo del emisor; frecuencia y alcance los
        # valida el anfitrion; caduca; el reinicio de la ronda los limpia.
        $r = PairRetry $label $tag @('-ueProbeStage','-ueProbeRestartAt','18') @('-ueProbe','mark') 90
        if ($r) { $fin = Last $r.c '\[Probe\] fin mark'; $okN = [regex]::Match("$fin", 'ok=(\d+)/(\d+)')
          $hostSaw = @(Grep $r.h '\[Mark\] .* marca Uncle .* dueño=1'); $cliSaw = @(Grep $r.c '\[Mark\] .* marca Uncle .* dueño=1')
          $sameColor = $hostSaw.Count -gt 0 -and $cliSaw.Count -gt 0 -and ([regex]::Match($hostSaw[0], 'color=#\w+').Value -eq [regex]::Match($cliSaw[0], 'color=#\w+').Value)
          $rejFreq = (Grep $r.h '\[Mark\] rechazado .*demasiado seguido').Count; $rejRange = (Grep $r.h '\[Mark\] rechazado .*fuera de alcance').Count
          $pass = $okN.Success -and $okN.Groups[1].Value -eq $okN.Groups[2].Value -and $sameColor -and $rejFreq -ge 1 -and $rejRange -ge 1
          Record $label $(if ($pass) { 'PASS' } else { 'FAIL' }) "sonda $($okN.Value) anfitrion_recibe=$($hostSaw.Count) mismo_color=$sameColor rechazos frecuencia=$rejFreq alcance=$rejRange" }
      }
      { $_ -in 'grace','graceend' } {
        # Desconexion en plena ronda: el anfitrion sigue jugando (autopiloto), los agarres del que se fue se sueltan, el
        # tiempo extra llega en el momento y el ajuste del tio una sola vez a los 10 s, con el transcurrido conservado.
        # graceend: ronda de 40 s y la desconexion cuando quedan menos de 10 s: no se pierde por reloj antes de la ayuda.
        $end = $sc -eq 'graceend'
        # -ueNoClose: el anfitrion nunca cierra la camioneta, asi la ronda dura mas que la gracia y termina por reloj
        $hx = @('-ueAutopilot','drag','-ueAutopilotDelay','2','-ueQuitOnEnd','-ueNoClose','-ueRoundTime', $(if ($end) { '40' } else { '60' }))
        $hostG = Start-Game "${tag}_host" (HostArgs $hx); Start-Sleep 4
        # el invitado sale solo cuando SU reloj replicado de ronda llega a 33 s (graceend: quedan 7 s) o 14 s (grace)
        $cliG = Start-Game "${tag}_client" (ClientArgs @('-ueAutopilot','drag','-ueAutopilotDelay','3','-ueNoClose','-ueQuitAtRound', $(if ($end) { '33' } else { '14' })))
        $null = $cliG.p.WaitForExit(120000); if (-not $cliG.p.HasExited) { $cliG.p.Kill() }
        $null = $hostG.p.WaitForExit($(if ($end) { 140000 } else { 300000 })); Stop-Game $hostG
        if ((Crashed $hostG) -or (Crashed $cliG)) { Record $label 'BLOCKED' "crash; ver $Log\${tag}_*.log"; break }
        $drop = @(Grep $hostG '\[Round\] baja de jugador'); $adj = @(Grep $hostG '\[Round\] grupo 2 -> 1 tras la gracia')
        $left = (Grep $hostG 'Se fue el jugador').Count -gt 0
        function Num($s) { $v = 0.0; if ([double]::TryParse(($s -replace ',', '.'), [Globalization.NumberStyles]::Float, [Globalization.CultureInfo]::InvariantCulture, [ref]$v)) { $v } else { -1.0 } }
        $tDrop = Num ([regex]::Match("$($drop | Select-Object -First 1)", 'a los ([\d,\.]+)s').Groups[1].Value)
        $tAdj = Num ([regex]::Match("$($adj | Select-Object -First 1)", 'transcurrido ([\d,\.]+)s').Groups[1].Value)
        $gap = [math]::Round($tAdj - $tDrop, 1)
        $tout = (Get-Content $hostG.log | Select-String '\[Round\] (TIMEOUT|SUCCESS)' | Select-Object -First 1).LineNumber
        $adjLine = (Get-Content $hostG.log | Select-String '\[Round\] grupo 2 -> 1' | Select-Object -First 1).LineNumber
        $diagAfter = (Grep $hostG 'NetDiag\] anfitrion: jugadores=1').Count -gt 0
        $final = (Grep $hostG '\[Round\] (SUCCESS|TIMEOUT)') | Select-Object -First 1
        $orderOk = $adjLine -and (-not $tout -or $tout -gt $adjLine)
        $pass = $drop.Count -eq 1 -and $adj.Count -eq 1 -and $left -and $gap -ge 9 -and $gap -le 11.5 -and $diagAfter -and $orderOk
        Record $label $(if ($pass) { 'PASS' } elseif ($drop.Count -eq 0) { 'NOT_RUN' } else { 'FAIL' }) "baja=$($drop.Count) ajuste=$($adj.Count) gracia=${gap}s agarres_liberados=$left sigue_jugando=$diagAfter fin_despues_del_ajuste=$orderOk [$([regex]::Match("$($drop | Select-Object -First 1)", 'quedaban.*').Value)] final=$([regex]::Match("$final", '(SUCCESS|TIMEOUT).*').Value)"
      }
      'clientdrop' {
        $hostG = Start-Game "${tag}_host" (HostArgs @()); Start-Sleep 4
        $cliG = Start-Game "${tag}_client" (ClientArgs @('-ueAutopilot','drag','-ueAutopilotDelay','3'))
        Start-Sleep 16; $cliG.p.Kill(); Start-Sleep 9; Stop-Game $hostG
        if ((Crashed $hostG) -or (Crashed $cliG)) { Record $label 'BLOCKED' "crash al arrancar; ver $Log\${tag}_*.log"; break }
        $held = (Grep $hostG 'NetDiag\] anfitrion: jugadores=2.*agarres=[1-9]').Count -gt 0
        $after = Last $hostG 'NetDiag\] anfitrion: jugadores=1 agarres='
        Record $label $(if ($held -and "$after" -match 'agarres=0') { 'PASS' } elseif (-not $held) { 'NOT_RUN' } else { 'FAIL' }) "agarraba=$held despues=$([regex]::Match("$after",'agarres=\d+').Value)"
      }
      'loaddrop' {
        # El cliente sale del juego en el instante en que pide cargar al tio en el carrito (E): el anfitrion termina o
        # descarta la carga sin quedar "Cargando", sin agarres huerfanos y sin excepciones.
        $r = PairRetry $label $tag @() @('-ueAutopilot','cart','-ueAutopilotDelay','3','-ueDropAt','strap') 240
        if ($r) { $drop = (Grep $r.c 'DROP durante la carga').Count -gt 0
          Start-Sleep 0
          $after = Last $r.h 'NetDiag\] anfitrion: jugadores=1 '
          $exc = (Grep $r.h 'Exception').Count
          if (-not $drop) { Record $label 'NOT_RUN' "el cliente no llego a cargar al tio en $(240) s (sin desconexion en el momento pedido)" }
          else { $ok = "$after" -match 'agarres=0' -and "$after" -notmatch 'carrito=Cargando' -and $exc -eq 0
            Record $label $(if ($ok) { 'PASS' } else { 'FAIL' }) "anfitrion despues: $([regex]::Match("$after",'agarres=\d+').Value) $([regex]::Match("$after",'carrito=\S+').Value) excepciones=$exc" } }
      }
      'closedrop' {
        # El cliente sale a mitad del cierre de puertas (E mantenida 40 %); el anfitrion (que no cierra) debe cancelar el
        # cierre: puertas Open, sin asegurar, sin excepciones.
        $r = PairRetry $label $tag @('-ueAutopilot','drag','-ueAutopilotDelay','6','-ueNoClose') @('-ueAutopilot','drag','-ueAutopilotDelay','3','-ueDropAt','closing') 330
        if ($r) { $drop = (Grep $r.c 'DROP durante el cierre').Count -gt 0
          $after = Last $r.h 'NetDiag\] anfitrion: jugadores=1 '
          $exc = (Grep $r.h 'Exception').Count
          if (-not $drop) { Record $label 'NOT_RUN' "el cliente no llego a cerrar (sin desconexion en el momento pedido)" }
          else { $ok = "$after" -match 'puertas=Open' -and "$after" -match 'asegurado=False' -and $exc -eq 0
            Record $label $(if ($ok) { 'PASS' } else { 'FAIL' }) "anfitrion despues: $([regex]::Match("$after",'puertas=\S+').Value) $([regex]::Match("$after",'asegurado=\S+').Value) excepciones=$exc" } }
      }
      { $_ -in 'party3','party4' } {
        $n = if ($sc -eq 'party3') { 3 } else { 4 }
        $g = Party $tag $n 420
        if ($null -eq $g) { Record $label 'BLOCKED' "crash al arrancar algun proceso; ver $Log\${tag}_*.log"; break }
        $finals = @($g | ForEach-Object { @(Finals $_)[0] })
        $started = Last $g[0] 'sala: inicia la ronda con'
        $allSame = $true; foreach ($f in $finals) { if (-not (SameFinal $finals[0] $f)) { $allSame = $false } }
        $ok = $allSame -and (Field $finals[0] 'ronda') -eq 'Success' -and ("$started" -match "con $n jugadores")
        $detail = "procesos=$n inicio[$([regex]::Match("$started",'con \d+ jugadores').Value)] " + (($finals | ForEach-Object { "[" + (Brief $_) + "]" }) -join ' ')
        Record $label $(if ($ok) { 'PASS' } elseif (-not $started) { 'NOT_RUN' } else { 'FAIL' }) $detail
      }
      'cycle' {
        # sesion 17: tres rondas seguidas con el mismo grupo (Sin carrito, Sin carrito, Estandar) y la torre tirada en cada
        # una; el invitado comprueba carrito ausente/presente segun el modo y torre armada al empezar y caida una sola vez.
        $r = PairRetry $label $tag @('-ueProbeCycle','-ueChallenge','NoCart') @('-ueProbe','cycle') 90
        if ($r) { $fin = Last $r.c '\[Probe\] fin cycle'; $okN = [regex]::Match("$fin", 'ok=(\d+)/(\d+)')
          $bad = (@(Grep $r.c '\[Probe\] paso=') | Where-Object { $_ -match 'ok=False' } | ForEach-Object { [regex]::Match($_, 'paso=(\S+)').Groups[1].Value }) -join ','
          $stale = (Grep $r.c 'descartada').Count
          $pass = $okN.Success -and $okN.Groups[1].Value -eq $okN.Groups[2].Value -and [int]$okN.Groups[2].Value -ge 9
          Record $label $(if ($pass) { 'PASS' } else { 'FAIL' }) "sonda $($okN.Value) eventos_viejos_descartados=$stale$(if ($bad) { " fallaron=$bad" })$(if (-not $fin) { ' (la sonda no termino)' })" }
      }
      'compat' {
        # sesion 17: compatibilidad de version. Anfitrion nuevo + invitado de la demo anterior, y al reves: el que entra tiene
        # que ser rechazado con el motivo de version y volver al menu (puede reintentar).
        if (-not (Test-Path $RefExe)) { Record $label 'NOT_RUN' "falta la build de referencia $RefExe"; break }
        $res = @()
        foreach ($pair in @(@{ h = $Exe; c = $RefExe; n = 'nuevo<-viejo' }, @{ h = $RefExe; c = $Exe; n = 'viejo<-nuevo' }))
        {
          $hostG = Start-Game "${tag}_$($pair.n -replace '[<-]','')_host" (HostArgs @() 2) $pair.h; Start-Sleep 5
          $cliG = Start-Game "${tag}_$($pair.n -replace '[<-]','')_client" (ClientArgs @()) $pair.c; Start-Sleep 14
          Stop-Game $cliG; Stop-Game $hostG
          $rej = (Grep $hostG 'rechazo de conexion .*error.version').Count -gt 0
          $saw = (Grep $cliG 'error.version').Count -gt 0
          $menu = (Grep $cliG '\[Menu\] en el menu').Count -ge 2
          $res += "$($pair.n): rechazo=$rej motivo_visto=$saw vuelve_al_menu=$menu"
        }
        $ok = ($res | Where-Object { $_ -match 'rechazo=True motivo_visto=True vuelve_al_menu=True' }).Count -eq 2
        Record $label $(if ($ok) { 'PASS' } else { 'FAIL' }) ($res -join ' | ')
      }
      'latejoin' {
        # anfitrion solo (espera 1 jugador) con la ronda en curso; un cliente llega a los 14 s y debe ser rechazado con motivo
        $hostG = Start-Game "${tag}_host" (HostArgs @('-ueAutopilot','drag','-ueQuitOnEnd') 1)
        Start-Sleep 14
        $cliG = Start-Game "${tag}_client" (ClientArgs @())
        Start-Sleep 12
        Stop-Game $cliG; $null = $hostG.p.WaitForExit(120000); Stop-Game $hostG
        if ((Crashed $hostG) -or (Crashed $cliG)) { Record $label 'BLOCKED' "crash; ver $Log\${tag}_*.log"; break }
        $rej = Last $hostG 'rechazo de conexion .*error.already_started'
        $cliSaw = Last $cliG 'error.already_started'
        $started = Last $hostG 'sala: inicia la ronda'
        Record $label $(if ($rej -and $cliSaw -and $started) { 'PASS' } elseif (-not $started) { 'NOT_RUN' } else { 'FAIL' }) "ronda_iniciada=$([bool]$started) rechazo_anfitrion=$([bool]$rej) cliente_ve_motivo=$([bool]$cliSaw)"
      }
      default { Record $label 'NOT_RUN' "escenario desconocido" }
    }
  }
}
} finally { Stop-AllTracked; Exit-BatchLock }
$results | Format-Table -AutoSize | Out-String -Width 260
$results | Export-Csv -NoTypeInformation -Encoding UTF8 (Join-Path $Log "net_smoke_results_$Scene.csv")
# Historial acumulado: conserva los fallos iniciales y las repeticiones (una repeticion aprobada no borra un FAIL anterior)
$results | Export-Csv -NoTypeInformation -Encoding UTF8 -Append (Join-Path $Log "net_smoke_history.csv")
$fail = @($results | Where-Object Resultado -eq 'FAIL').Count
$incomplete = @($results | Where-Object { $_.Resultado -in 'BLOCKED','NOT_RUN' }).Count
if ($results.Count -eq 0) { Write-Host 'Sin resultados: ejecucion incompleta' -ForegroundColor Yellow; exit 2 }
if ($fail -gt 0) { Write-Host "FALLARON $fail escenario(s)" -ForegroundColor Red; exit 1 }
if ($incomplete -gt 0) { Write-Host "Ejecucion incompleta: $incomplete escenario(s) BLOCKED/NOT_RUN" -ForegroundColor Yellow; exit 2 }
Write-Host "Todos los escenarios pedidos: PASS" -ForegroundColor Green; exit 0
