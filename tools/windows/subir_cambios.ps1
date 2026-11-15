# Sube main a GitHub sin force push. Se niega si main no contiene a origin/main.
$ErrorActionPreference = 'Continue'
$Repo   = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$LogDir = Join-Path $Repo '.local-setup\logs'; New-Item -ItemType Directory -Force -Path $LogDir | Out-Null
Start-Transcript -Path (Join-Path $LogDir ('subir_cambios_' + (Get-Date -Format 'yyyyMMdd_HHmmss') + '.log')) -Force | Out-Null
Set-Location $Repo
git fetch origin --prune 2>&1 | ForEach-Object { "$_" } | Out-Host
git status --short --branch 2>&1 | ForEach-Object { "$_" } | Out-Host
$behind = (git rev-list --count main..origin/main 2>$null)
if ($behind -and [int]$behind -gt 0) { Write-Host "main no contiene $behind commit(s) de origin/main. No se sube; avisale a Claude." -ForegroundColor Red }
elseif (git status --porcelain) { Write-Host 'Hay cambios sin commit. No se sube; avisale a Claude.' -ForegroundColor Red }
else {
  git push -u origin main 2>&1 | ForEach-Object { "$_" } | Out-Host
  git log --oneline --decorate -n 10 2>&1 | ForEach-Object { "$_" } | Out-Host
}
Stop-Transcript | Out-Null
Read-Host 'Enter para cerrar'
