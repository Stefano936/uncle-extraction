# Auditoria automatica de la interfaz en la build: varias resoluciones/aspectos y texto al 100 % y 150 %.
# Uso: powershell -ExecutionPolicy Bypass -File tools\windows\ui_audit.ps1 [-Out .local-setup\shots\ui_audit]
# Cada variante abre la build en ventana sin bordes, recorre menu, error, sala, HUD, pausa, opciones y resultados
# (UiAudit.cs), captura cada pantalla y registra textos fuera de pantalla, recortados o encimados.
# Aspectos mayores que el monitor (2560x1080, 3440x1440) se prueban con una ventana del MISMO aspecto: el CanvasScaler
# (1920x1080, match 0,5) escala uniformemente, asi que la disposicion en unidades de canvas es identica.
param([string]$Out = '', [string]$Lang = '', [switch]$Quick)
$ErrorActionPreference = 'Stop'
$Repo = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$Exe = Join-Path $Repo 'Builds\UncleExtraction\UncleExtraction.exe'
if (-not $Out) { $Out = Join-Path $Repo '.local-setup\shots\ui_audit' }
New-Item -ItemType Directory -Force $Out | Out-Null; $Out = (Resolve-Path $Out).Path
$ver = if (Test-Path (Join-Path (Split-Path $Exe) 'VERSION.txt')) { ((Get-Content (Join-Path (Split-Path $Exe) 'VERSION.txt') -Raw) -replace '\s+', ' ').Trim() } else { 'sin VERSION.txt' }
$variants = @(
  @{ w = 1280; h = 720;  name = '720p 16:9' },
  @{ w = 1920; h = 1080; name = '1080p 16:9' },
  @{ w = 1920; h = 810;  name = 'ultrawide 21:9 (equivale a 2560x1080; 3440x1440 es 21,5:9)' },
  @{ w = 1728; h = 1080; name = '16:10' },
  @{ w = 1440; h = 1080; name = '4:3' }
)
if ($Quick) { $variants = @($variants[0], $variants[2]) }
$langArgs = if ($Lang) { @('-ueLang', $Lang) } else { @() }
$rows = @()
foreach ($v in $variants) { foreach ($ts in '1', '1.25', '1.5') {
  $log = Join-Path $Out ("audit_{0}x{1}_t{2}.log" -f $v.w, $v.h, $ts)
  $p = Start-Process $Exe -ArgumentList (@('-screen-fullscreen','0','-popupwindow','-screen-width',"$($v.w)",'-screen-height',"$($v.h)",'-ueUiAudit',"`"$Out`"",'-ueTextScale',$ts) + $langArgs + @('-logFile',"`"$log`"")) -PassThru
  if (-not $p.WaitForExit(120000)) { $p.Kill(); $state = 'NOT_RUN'; $detail = 'no termino en 120 s' }
  else {
    $start = Select-String -Path $log -Pattern '\[UiAudit\] inicio' | Select-Object -Last 1
    $sum = Select-String -Path $log -Pattern '\[UiAudit\] resumen' | Select-Object -Last 1
    $real = [regex]::Match("$start", 'pantalla=(\d+x\d+)').Groups[1].Value
    if (-not $sum) { $state = 'NOT_RUN'; $detail = "sin resumen (pantalla real $real)" }
    else {
      $n = [int]([regex]::Match("$sum", 'problemas=(\d+)').Groups[1].Value)
      $state = if ($n -eq 0) { 'PASS' } else { 'FAIL' }
      $detail = "pantalla real $real; " + [regex]::Match("$sum", ':: (.*)$').Groups[1].Value
    }
  }
  $rows += [pscustomobject]@{ Variante = "$($v.name) texto x$ts"; Resultado = $state; Build = $ver; Detalle = $detail }
  Write-Host ("{0,-70} {1,-8} {2}" -f "$($v.name) texto x$ts", $state, $detail)
} }
$issues = Get-ChildItem $Out -Filter 'audit_*.log' | ForEach-Object { Select-String -Path $_.FullName -Pattern '\[UiAudit\] \S+ (FUERA|RECORTADO|ENCIMADO|TAPADO|PANELES)' | ForEach-Object { "$($_.Filename): $($_.Line)" } }
$issues | Set-Content -Encoding UTF8 (Join-Path $Out 'problemas.txt')
$rows | Export-Csv -NoTypeInformation -Encoding UTF8 (Join-Path $Out 'resultados.csv')
Write-Host "Problemas detallados: $(Join-Path $Out 'problemas.txt') ($(@($issues).Count) lineas)"
if (@($rows | Where-Object Resultado -ne 'PASS').Count -gt 0) { exit 1 } else { exit 0 }
