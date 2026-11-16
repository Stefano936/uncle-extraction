# UNCLE EXTRACTION - pruebas de humo de red con DOS PROCESOS en la misma PC (localhost, 127.0.0.1).
# No es una prueba LAN ni de internet: registrar esos resultados por separado.
# Uso: powershell -ExecutionPolicy Bypass -File tools\windows\net_smoke.ps1 [-Scenario <lista separada por comas>|all] [-Scene Bar_LastRound|PhysicsLab] [-Runs 1]
#   Escenarios: coop, repeat, lose, netsim, race, hostleave, clientdrop, loaddrop, closedrop, party3, party4, latejoin
#   party3/party4: un anfitrion y 2/3 clientes en procesos separados (sala, inicio con todos listos, ronda completa).
#   latejoin: un cliente que llega con la ronda iniciada es rechazado con motivo (sin incorporacion en curso).
#   netsim usa la build de desarrollo: el simulador de red de NGO solo existe en editor y builds de desarrollo.
# Codigo de salida: 0 = TODOS los escenarios pedidos dieron PASS; 1 = hubo algun FAIL; 2 = ejecucion incompleta
#   (algun BLOCKED o NOT_RUN, o no se ejecuto nada). Un 0 nunca encubre escenarios sin correr.
param([string]$Scenario = 'all', [string]$Scene = 'Bar_LastRound', [int]$Runs = 1)
$ErrorActionPreference = 'Continue'
$Repo   = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$Exe    = Join-Path $Repo 'Builds\UncleExtraction\UncleExtraction.exe'
$ExeDev = Join-Path $Repo 'Builds\UncleExtraction_Dev\UncleExtraction.exe'
$Log    = Join-Path $Repo '.local-setup\logs\net'
New-Item -ItemType Directory -Force -Path $Log | Out-Null
if (-not (Test-Path $Exe)) { Write-Host "Falta la build: $Exe" -ForegroundColor Red; exit 2 }
$Win = @('-screen-fullscreen','0','-screen-width','800','-screen-height','450')
$All = @('coop','repeat','lose','netsim','race','hostleave','clientdrop','loaddrop','closedrop','party3','party4','latejoin')
$Wanted = if ($Scenario -eq 'all') { $All } else { $Scenario -split ',' | ForEach-Object { $_.Trim() } }
$results = @()
# Procesos del juego que quedaron vivos de una corrida anterior ocupan el puerto 7777: cerrarlos (solo los de Builds\)
Get-Process UncleExtraction -ErrorAction SilentlyContinue | Where-Object { $_.Path -like (Join-Path $Repo 'Builds\*') } | ForEach-Object { Write-Host "Cerrando proceso previo $($_.Id)"; $_ | Stop-Process -Force }
Start-Sleep 1

function Start-Game($name, [string[]]$extra, [string]$exe = $Exe) {
  $f = Join-Path $Log "$name.log"; if (Test-Path $f) { Remove-Item $f }
  $p = Start-Process $exe -ArgumentList ($Win + @('-ueScene', $Scene) + $extra + @('-logFile', "`"$f`"")) -PassThru
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
function HostArgs([string[]]$x, [int]$expect = 2) { @('-ueHost','-ueListen','127.0.0.1','-uePort','7777','-ueExpectPlayers',"$expect") + $x }
function ClientArgs([string[]]$x) { @('-ueClient','127.0.0.1','-uePort','7777') + $x }

# Dos procesos: anfitrion y cliente. Devuelve $null si alguno crasheo al arrancar (registro conservado).
function Pair($tag, [string[]]$h, [string[]]$c, [int]$timeout, [string]$exe = $Exe, [switch]$waitHost) {
  $hostG = Start-Game "${tag}_host" (HostArgs $h) $exe
  Start-Sleep 4   # arranque escalonado: dos inicializaciones D3D simultaneas provocaron un crash del driver NVIDIA
  $cliG = Start-Game "${tag}_client" (ClientArgs $c) $exe
  if ($waitHost) { $null = $hostG.p.WaitForExit($timeout * 1000); $null = $cliG.p.WaitForExit(15000) }
  else { $null = $cliG.p.WaitForExit($timeout * 1000); $null = $hostG.p.WaitForExit(25000) }
  Stop-Game $cliG; Stop-Game $hostG
  if ((Crashed $hostG) -or (Crashed $cliG)) { return $null }
  if ((Grep $hostG 'begin Host ok=False').Count -gt 0) { Record "$tag" 'BLOCKED' "el anfitrion no pudo escuchar en 7777 (puerto ocupado)"; return 'blocked' }
  return @{ h = $hostG; c = $cliG }
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
function Party($tag, [int]$n, [int]$timeout) {
  $games = @(); $games += Start-Game "${tag}_host" (HostArgs @('-ueAutopilot','drag','-ueQuitOnEnd') $n)
  for ($k = 1; $k -lt $n; $k++) { Start-Sleep 4; $games += Start-Game "${tag}_client$k" (ClientArgs @('-ueAutopilot','drag','-ueAutopilotDelay','3','-ueQuitOnEnd')) }
  $deadline = (Get-Date).AddSeconds($timeout)
  foreach ($g in $games) { $left = [int][Math]::Max(1, ($deadline - (Get-Date)).TotalMilliseconds); $null = $g.p.WaitForExit($left) }
  foreach ($g in $games) { Stop-Game $g }
  if (@($games | Where-Object { Crashed $_ }).Count -gt 0) { return $null }
  return $games
}

function SameFinal($a, $b) {
  $a -and $b -and (Converged $a) -and (Converged $b) -and ((Field $a 'reinicios') -eq (Field $b 'reinicios')) -and ((Field $a 'ronda') -eq (Field $b 'ronda')) -and ((Field $a 'asegurado') -eq (Field $b 'asegurado')) -and ((Field $a 'tio') -eq (Field $b 'tio')) -and (((Field $a 'puertas') -split '/')[0] -eq ((Field $b 'puertas') -split '/')[0])
}
function Brief($l) { "$(Field $l 'ronda') t=$(Field $l 't') intento=$(Field $l 'reinicios') puertas=$(Field $l 'puertas') tio=$(Field $l 'tio') asegurado=$(Field $l 'asegurado') escena=$(Field $l 'escena') conv=$(Field $l 'convergencia')$(if ((Field $l 'faltan') -ne '') { ' faltan=' + (Field $l 'faltan') })" }

for ($run = 1; $run -le $Runs; $run++) {
  foreach ($sc in $Wanted) {
    $label = "$sc #$run"; $tag = "${sc}_${Scene}_$run"
    switch ($sc) {
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
        $at = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds() + 17000
        $r = PairRetry $label $tag @('-ueAutopilot','race','-ueRaceAt',"$at") @('-ueAutopilot','race','-ueRaceAt',"$at",'-ueAutopilotDelay','2') 24 -waitHost
        if ($r) { $seq = @(Grep $r.h '\[Grab\] (create .*-> belt$|release .*-> belt reason)')
          $live = 0; $maxLive = 0; foreach ($l in $seq) { if ($l -match 'create') { $live++ } else { $live-- }; if ($live -gt $maxLive) { $maxLive = $live } }
          $busy = (Grep $r.h '\[Grab\] reject .*socket=belt: SocketBusy').Count
          $winners = @((Last $r.h 'resultado carrera'), (Last $r.c 'resultado carrera')) | Where-Object { $_ -match "mano izq = 'belt'" }
          Record $label $(if ($maxLive -eq 1 -and $busy -ge 1 -and $winners.Count -eq 1) { 'PASS' } else { 'FAIL' }) "max_simultaneos=$maxLive rechazos=$busy ganadores=$($winners.Count)" }
      }
      'hostleave' {
        $hostG = Start-Game "${tag}_host" (HostArgs @()); Start-Sleep 4
        $cliG = Start-Game "${tag}_client" (ClientArgs @('-ueAutopilot','drag','-ueAutopilotDelay','3'))
        Start-Sleep 14; Stop-Game $hostG; Start-Sleep 9; Stop-Game $cliG
        if ((Crashed $hostG) -or (Crashed $cliG)) { Record $label 'BLOCKED' "crash al arrancar; ver $Log\${tag}_*.log"; break }
        $disc = (Grep $cliG 'Disconnect Event').Count -gt 0; $menu = (Grep $cliG '\[Menu\] en el menu').Count -ge 2; $fake = (Grep $cliG '\[Round\] SUCCESS').Count -gt 0
        Record $label $(if ($disc -and $menu -and -not $fake) { 'PASS' } else { 'FAIL' }) "desconexion=$disc vuelve_al_menu=$menu victoria_falsa=$fake"
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
