# Proteccion comun de las tandas de verificacion (sesion 12).
# - Assert-Preflight: sintaxis de los scripts de tools/windows y VERSION.txt de cada build con el commit esperado.
# - Enter-BatchLock / Exit-BatchLock: una sola tanda a la vez (.local-setup/batch.lock con el PID duenio).
# - Start-Tracked / Stop-AllTracked: cada script detiene SUS procesos al terminar o al cancelarse (finally), sin tocar
#   procesos ajenos ni de otras tandas.
$script:Tracked = New-Object System.Collections.ArrayList
$script:Repo = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$script:LockFile = Join-Path $script:Repo '.local-setup\batch.lock'

function Assert-Preflight([string[]]$Builds = @(), [string]$ExpectCommit = '') {
  $bad = @()
  foreach ($f in Get-ChildItem (Join-Path $script:Repo 'tools\windows') -Filter *.ps*1) {
    $e = $null; [void][System.Management.Automation.Language.Parser]::ParseFile($f.FullName, [ref]$null, [ref]$e)
    if ($e.Count -gt 0) { $bad += "$($f.Name): $($e[0].Message) (linea $($e[0].Extent.StartLineNumber))" }
  }
  # Sin commit esperado: la build vale si su commit no difiere del actual en UnityProject/ (los commits de documentacion
  # posteriores no la invalidan) y si el arbol actual no tiene cambios sin commitear en UnityProject/.
  foreach ($b in $Builds) {
    $v = Join-Path $script:Repo "Builds\$b\VERSION.txt"
    if (-not (Test-Path $v)) { $bad += "$b sin VERSION.txt"; continue }
    $t = Get-Content $v -Raw; $c = [regex]::Match($t, 'Commit de origen: ([0-9a-f]+)').Groups[1].Value
    if ($t -match 'cambios sin commitear') { $bad += "$b se genero con cambios sin commitear"; continue }
    if ($ExpectCommit) { if ($c -ne $ExpectCommit) { $bad += "$b es $c, se esperaba $ExpectCommit" }; continue }
    git -C $script:Repo diff --quiet $c HEAD -- UnityProject 2>$null
    if ($LASTEXITCODE -ne 0) { $bad += "$b es $c y UnityProject cambio despues" }
  }
  git -C $script:Repo diff --quiet HEAD -- UnityProject 2>$null
  if ($Builds.Count -and $LASTEXITCODE -ne 0) { $bad += "hay cambios sin commitear en UnityProject (la build no los tendria)" }
  if ($bad.Count) { throw "Preflight: " + ($bad -join ' | ') }
  Write-Host "Preflight OK (scripts sin errores; builds $($Builds -join ', ') al dia con el codigo)"
}

function Enter-BatchLock([string]$Name) {
  if (Test-Path $script:LockFile) {
    $l = Get-Content $script:LockFile -Raw; $owner = [int](($l -split ' ')[0])
    if (Get-Process -Id $owner -ErrorAction SilentlyContinue) { throw "Ya corre otra tanda: $l" }
  }
  New-Item -ItemType Directory -Force (Split-Path $script:LockFile) | Out-Null
  "$PID $Name $(Get-Date -Format s)" | Set-Content $script:LockFile
}
function Exit-BatchLock { if (Test-Path $script:LockFile) { $owner = [int]((Get-Content $script:LockFile -Raw) -split ' ')[0]; if ($owner -eq $PID) { Remove-Item $script:LockFile -Force } } }

function Start-Tracked([string]$Exe, [string[]]$ArgList) {
  $p = Start-Process $Exe -ArgumentList $ArgList -PassThru
  [void]$script:Tracked.Add($p); return $p
}
function Register-Tracked($p) { if ($p) { [void]$script:Tracked.Add($p) } }
function Stop-AllTracked {
  foreach ($p in $script:Tracked) {
    try { if (-not $p.HasExited) {
      Get-CimInstance Win32_Process -Filter "ParentProcessId=$($p.Id)" -ErrorAction SilentlyContinue | ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
      $p.Kill() } } catch {}
  }
  $script:Tracked.Clear()
}
Export-ModuleMember -Function Assert-Preflight, Enter-BatchLock, Exit-BatchLock, Start-Tracked, Register-Tracked, Stop-AllTracked
