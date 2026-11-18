param(
    [Parameter(Mandatory)][string]$Exe,
    [Parameter(Mandatory)][string]$Out
)
$ErrorActionPreference = 'Stop'
$Exe = (Resolve-Path -LiteralPath $Exe).Path
New-Item -ItemType Directory -Force -Path $Out | Out-Null
$Out = (Resolve-Path -LiteralPath $Out).Path
$views = [ordered]@{
    general = '0.65,0,4.6,43,10@2'
    barra = '3.45,0,7.35,24,25@2'
    salida = '0.9,0,-0.6,215,8@2'
}

foreach ($view in $views.GetEnumerator()) {
    $shot = Join-Path $Out ($view.Key + '.png')
    $log = Join-Path $Out ($view.Key + '.log')
    if (Test-Path -LiteralPath $shot) { throw "Ya existe $shot" }
    $arguments = @('-screen-fullscreen','0','-screen-width','1600','-screen-height','900',
        '-ueScene','Bar_LastRound','-ueQuality','high','-ueLang','es_UY','-ueCleanCapture',
        '-ueNoHud','-ueAutopilot','drag','-ueAutopilotDelay','120','-uePose',$view.Value,'-ueShot',('"{0}@3.5"' -f $shot),'-logFile',('"{0}"' -f $log))
    $p = Start-Process -FilePath $Exe -ArgumentList $arguments -PassThru
    try {
        for ($i = 0; $i -lt 320 -and !$p.HasExited; $i++) {
            if (Test-Path -LiteralPath $shot) { break }
            Start-Sleep -Milliseconds 250
        }
        if (!(Test-Path -LiteralPath $shot)) { throw "Falta $shot (ver $log)" }
        Start-Sleep -Milliseconds 500
        if (!(Select-String -LiteralPath $log -SimpleMatch "[Shot] pose $($view.Value)" -Quiet)) {
            throw "La cámara no confirmó la pose (ver $log)"
        }
        Get-Item -LiteralPath $shot
    }
    finally {
        if (!$p.HasExited) {
            $null = $p.CloseMainWindow()
            if (!$p.WaitForExit(4000)) { Stop-Process -Id $p.Id }
        }
    }
}
