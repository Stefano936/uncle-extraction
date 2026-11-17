# Rendimiento comparable (sesion 12): corre el banco de vistas fijas (Bench.cs) en una o varias builds con las mismas
# condiciones y arma una tabla. Uso:
#   powershell -ExecutionPolicy Bypass -File tools\windows\perf_bench.ps1 -Builds UncleExtraction,UncleExtraction_Bench_2fc2d5d
#     [-Modes normal,novsync] [-Quality high|low] [-Width 1920 -Height 1080] [-Device -1] [-Repeat 2] [-Tag nombre]
#   -Device N: GPU por indice (-force-device-index de Unity; sin tocar la configuracion de Windows). -1 = la que elija Windows.
#   novsync: sin sincronizacion vertical ni limite de FPS; SOLO diagnostico (costo real sin el tope del monitor).
param([string[]]$Builds = @('UncleExtraction'), [string[]]$Modes = @('normal','novsync'), [string]$Quality = 'high',
      [int]$Width = 1920, [int]$Height = 1080, [int]$Device = -1, [int]$Repeat = 2, [string]$Tag = '', [float]$PerView = 6)
$ErrorActionPreference = 'Stop'
$Repo = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$Out = Join-Path $Repo ".local-setup\logs\perf"; New-Item -ItemType Directory -Force $Out | Out-Null
Import-Module (Join-Path $PSScriptRoot 'Guard.psm1') -Force
Assert-Preflight   # sintaxis de los scripts (las builds de comparacion pueden ser de otro commit: se registra su VERSION.txt)
Enter-BatchLock 'perf_bench'
$rows = @()
try {
  # procesos abiertos al empezar (condicion registrada; no se cierran procesos ajenos)
  $busy = Get-Process | Where-Object { $_.CPU -gt 0 } | Sort-Object CPU -Descending | Select-Object -First 8 | ForEach-Object { $_.ProcessName }
  $power = (Get-CimInstance Win32_Battery).BatteryStatus  # 2 = conectada a la red
  $plan = ((powercfg /getactivescheme) -replace '.*\(([^)]*)\).*', '$1').Trim()
  foreach ($r in 1..$Repeat) { foreach ($b in $Builds) { foreach ($m in $Modes) {
    $exe = Join-Path $Repo "Builds\$b\UncleExtraction.exe"
    $ver = ((Get-Content (Join-Path $Repo "Builds\$b\VERSION.txt") -Raw) -split "`n")[0].Trim()
    $log = Join-Path $Out ("bench_{0}_{1}_{2}_{3}x{4}_{5}{6}_r{7}.log" -f $b, $m, $Quality, $Width, $Height, $(if ($Device -ge 0) { "dev$Device" } else { 'auto' }), $Tag, $r)
    if (Test-Path $log) { Remove-Item $log }
    $a = @('-screen-fullscreen','0','-popupwindow','-screen-width',"$Width",'-screen-height',"$Height",'-ueBench',"$PerView",'-ueQuality',$Quality,'-logFile',"`"$log`"")
    if ($m -eq 'novsync') { $a += '-ueNoVsync' }
    if ($Device -ge 0) { $a += @('-force-device-index',"$Device") }
    $p = Start-Tracked $exe $a
    if (-not $p.WaitForExit(180000)) { $p.Kill(); $res = $null } else { $res = Select-String -Path $log -Pattern '\[Bench\] resumen' | Select-Object -Last 1 }
    $L = "$res"
    function F($k) { [regex]::Match($L, "$k=([^\s»]+)").Groups[1].Value }
    $row = [pscustomobject]@{
      Build = "$b ($ver)"; Modo = $m; Calidad = $Quality; Resolucion = "${Width}x${Height}"; Rep = $r
      GPU = [regex]::Match($L, 'gpu=«([^»]*)»').Groups[1].Value; Alimentacion = (F 'alimentacion'); Plan = $plan
      Vsync = (F 'vsync'); Limite = (F 'limite'); Pantalla = (F 'pantalla')
      FPS = (F 'fps'); p50 = [regex]::Match($L, 'cuadro_ms p50=(\S+)').Groups[1].Value; p95 = [regex]::Match($L, 'cuadro_ms p50=\S+ p95=(\S+)').Groups[1].Value
      p99 = [regex]::Match($L, ' p99=(\S+)').Groups[1].Value; CPU_ms = [regex]::Match($L, 'cpu_ms media=(\S+)').Groups[1].Value; GPU_ms = [regex]::Match($L, 'gpu_ms media=(\S+)').Groups[1].Value
      Escala = (F 'escala'); MSAA = (F 'msaa'); LucesSombra = (F 'luces_con_sombra'); Post = (F 'posproceso')
    }
    if (-not $res) { $row.FPS = 'NOT_RUN' }
    $rows += $row
    Write-Host ("{0,-38} {1,-8} {2,-5} {3,-9} r{4} GPU={5} fps={6} p95={7} p99={8} cpu={9} gpu={10} vsync={11}" -f $row.Build, $m, $Quality, $row.Resolucion, $r, $row.GPU, $row.FPS, $row.p95, $row.p99, $row.CPU_ms, $row.GPU_ms, $row.Vsync)
  } } }
} finally { Stop-AllTracked; Exit-BatchLock }
$csv = Join-Path $Out ("bench_{0}{1}.csv" -f (Get-Date -Format 'yyyyMMdd_HHmm'), $(if ($Tag) { "_$Tag" } else { '' }))
$rows | Export-Csv -NoTypeInformation -Encoding UTF8 $csv
Write-Host "Procesos con mas CPU al empezar: $($busy -join ', ')"
Write-Host "Resultados: $csv"
