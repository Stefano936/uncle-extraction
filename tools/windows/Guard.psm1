# Proteccion comun de las tandas de verificacion (sesion 12).
# - Assert-Preflight: sintaxis de los scripts de tools/windows y VERSION.txt de cada build con el commit esperado.
# - Enter-BatchLock / Exit-BatchLock: una sola tanda a la vez (.local-setup/batch.lock con el PID duenio).
# - Start-Tracked / Stop-AllTracked: cada script detiene SUS procesos al terminar o al cancelarse (finally), sin tocar
#   procesos ajenos ni de otras tandas.
$script:Tracked = New-Object System.Collections.ArrayList
$script:Repo = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$script:LockFile = Join-Path $script:Repo '.local-setup\batch.lock'
$script:LockOwned = $false

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

# Bloqueo ATOMICO (sesion 17): un mutex con nombre del sistema. Tomarlo es atomico; si el duenio muere (tanda cancelada o
# matada) el sistema lo marca abandonado y el siguiente lo recupera sin carreras. El archivo batch.lock solo informa
# quien corre (PID, nombre, hora) y lo borra unicamente quien tiene el mutex.
function Enter-BatchLock([string]$Name) {
  New-Item -ItemType Directory -Force (Split-Path $script:LockFile) | Out-Null
  $m = New-Object System.Threading.Mutex($false, 'Global\UncleExtractionBatchLock')
  $got = $false
  try { $got = $m.WaitOne(0) } catch [System.Threading.AbandonedMutexException] { $got = $true; Write-Host 'Bloqueo de una tanda cancelada: recuperado' }
  if (-not $got) { $l = ''; try { $l = Get-Content $script:LockFile -Raw -ErrorAction Stop } catch {}; $m.Dispose(); throw "Ya corre otra tanda: $l" }
  $script:Mutex = $m; $script:LockOwned = $true
  "$PID $Name $(Get-Date -Format s)" | Set-Content $script:LockFile
}
# Solo lo libera quien lo tomo: un script que no consiguio el bloqueo no borra el de otro.
function Exit-BatchLock {
  if (-not $script:LockOwned) { return }
  Remove-Item $script:LockFile -Force -ErrorAction SilentlyContinue
  try { $script:Mutex.ReleaseMutex() } catch {}; $script:Mutex.Dispose(); $script:Mutex = $null; $script:LockOwned = $false
}

# Instantanea de los archivos VERSIONADOS del proyecto (sesion 17): una tanda de pruebas no puede modificarlos.
function Get-AssetSnapshot {
  $files = git -C $script:Repo ls-files -- UnityProject/Assets UnityProject/ProjectSettings UnityProject/Packages
  $h = @{}; foreach ($f in $files) { $p = Join-Path $script:Repo $f; if (Test-Path $p) { $h[$f] = (Get-FileHash $p -Algorithm SHA1).Hash } else { $h[$f] = 'missing' } }
  return $h
}
function Compare-AssetSnapshot($Before) {
  $after = Get-AssetSnapshot; $diff = @()
  foreach ($k in $Before.Keys) { if ($after[$k] -ne $Before[$k]) { $diff += $k } }
  $untracked = git -C $script:Repo ls-files --others --exclude-standard -- UnityProject/Assets UnityProject/ProjectSettings UnityProject/Packages
  return @($diff) + @($untracked | Where-Object { $_ })
}

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
Export-ModuleMember -Function Assert-Preflight, Enter-BatchLock, Exit-BatchLock, Start-Tracked, Register-Tracked, Stop-AllTracked, Get-AssetSnapshot, Compare-AssetSnapshot
