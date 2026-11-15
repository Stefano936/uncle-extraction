# UNCLE EXTRACTION - preparacion del entorno Windows (idempotente: se puede volver a ejecutar).
# Instala solo lo que falta, desde fuentes oficiales (winget), y conecta GitHub y Unity.
# No pide ni guarda contrasenas o tokens: los inicios de sesion se hacen en el navegador.
$ErrorActionPreference = 'Continue'
$Repo   = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$LogDir = Join-Path $Repo '.local-setup\logs'
New-Item -ItemType Directory -Force -Path $LogDir | Out-Null
$Log = Join-Path $LogDir ('setup_entorno_' + (Get-Date -Format 'yyyyMMdd_HHmmss') + '.log')
Start-Transcript -Path $Log -Force | Out-Null
$RepoUrl = 'https://github.com/Stefano936/uncle-extraction.git'

function Step($t) { Write-Host ''; Write-Host "==== $t ====" -ForegroundColor Cyan }
function Refresh-Path { $env:Path = [Environment]::GetEnvironmentVariable('Path','Machine') + ';' + [Environment]::GetEnvironmentVariable('Path','User') }
function Has($c) { [bool](Get-Command $c -ErrorAction SilentlyContinue) }
function Run { param([string]$Exe) $rest = $args; & $Exe @rest 2>&1 | ForEach-Object { "$_" } | Out-Host; return $LASTEXITCODE }
function Ask-User($msg) { Write-Host ''; Write-Host ">>> $msg" -ForegroundColor Yellow; Read-Host 'Pulsa Enter para continuar' | Out-Null }
function Find-Hub { @("$env:ProgramFiles\Unity Hub\Unity Hub.exe", "$env:LOCALAPPDATA\Programs\Unity Hub\Unity Hub.exe") | Where-Object { Test-Path $_ } | Select-Object -First 1 }
function Winget-Install($id) {
  Write-Host "Instalando $id desde winget (fuente oficial). Si Windows pide permiso de administrador, acepta." -ForegroundColor Yellow
  winget install --id $id -e --source winget
  Refresh-Path
}

Step '1. Inventario'
Write-Host ("Windows: " + [Environment]::OSVersion.VersionString + "  Usuario: $env:USERNAME  Repo: $Repo")
foreach ($c in 'winget','git','gh','unity','dotnet') {
  if (Has $c) { Write-Host ("{0,-7} OK  {1}" -f $c, (Get-Command $c).Source) } else { Write-Host ("{0,-7} NO ENCONTRADO" -f $c) }
}
if (Has git) { Run git --version | Out-Null; Run git lfs version | Out-Null }
if (Has gh)  { Run gh --version | Out-Null }
Write-Host ("Unity Hub: " + (Find-Hub))
if (-not (Has winget)) {
  Write-Host 'Falta winget (App Installer). Instalalo desde Microsoft Store: "App Installer", y volve a ejecutar este script.' -ForegroundColor Red
  Stop-Transcript | Out-Null; Read-Host 'Enter para cerrar'; exit 1
}

Step '2. Instalaciones faltantes'
if (-not (Has git)) { Winget-Install 'Git.Git' }
if (-not (Has gh))  { Winget-Install 'GitHub.cli' }
if (-not (Has gh) -and (Test-Path "$env:ProgramFiles\GitHub CLI\gh.exe")) { $env:Path += ";$env:ProgramFiles\GitHub CLI" }
if (-not (Find-Hub)) { Winget-Install 'Unity.UnityHub' }
if (-not (Has unity)) { Winget-Install 'Unity.CLI' }
foreach ($c in 'git','gh','unity') { Write-Host ("{0,-6} " -f $c) -NoNewline; if (Has $c) { Write-Host 'OK' } else { Write-Host 'FALTA (cerrar y reabrir el script puede refrescar el PATH)' -ForegroundColor Red } }
Write-Host ("Unity Hub: " + (Find-Hub))

Step '3. GitHub'
if (Has gh) {
  $null = Run gh auth status --hostname github.com
  if ($LASTEXITCODE -ne 0) {
    Ask-User 'Ahora se abre el inicio de sesion oficial de GitHub. Copia el codigo de un solo uso que aparece abajo, pegalo en github.com/login/device y autoriza "GitHub CLI". Nunca lo compartas en el chat.'
    gh auth login --hostname github.com --git-protocol https --web
  }
  $null = Run gh auth setup-git
  $null = Run gh auth status --hostname github.com
  $null = Run gh api repos/Stefano936/uncle-extraction --jq '"repo=" + .full_name + " private=" + (.private|tostring) + " default=" + .default_branch'
} else { Write-Host 'gh no disponible; se omite GitHub.' -ForegroundColor Red }

Step '4. Repositorio local y remoto (solo lectura: fetch, sin merge ni push)'
Set-Location $Repo
$st = (git status 2>&1 | Out-String)
if ($st -match 'dubious ownership') { $p = ($Repo -replace '\\','/'); Write-Host "Agregando safe.directory solo para $p"; git config --global --add safe.directory $p }
$null = Run git status --short --branch
if (-not ((git remote) -contains 'origin')) { $null = Run git remote add origin $RepoUrl }
$null = Run git remote -v
$null = Run git fetch origin --prune
$null = Run git branch -a -vv
$null = Run git log --oneline --graph --decorate --all -n 40

Step '5. Unity'
if (Has unity) {
  $null = Run unity --version
  $null = Run unity auth status
  if ($LASTEXITCODE -ne 0) {
    Ask-User 'Ahora se abre el inicio de sesion oficial de Unity en el navegador. Inicia sesion (o crea la cuenta) y acepta las condiciones si te las pide.'
    unity auth login
    $null = Run unity auth status
  }
  $rel = (unity releases 2>&1 | Out-String)
  $vers = [regex]::Matches($rel, '6000\.3\.\d+f\d+') | ForEach-Object { $_.Value } | Sort-Object -Unique |
          Sort-Object { [version](($_ -replace 'f','.')) } -Descending
  Write-Host ("Versiones 6000.3 (Unity 6.3 LTS) encontradas: " + ($vers -join ', '))
  $inst = (unity editors -i 2>&1 | Out-String); Write-Host $inst
  $have = [regex]::Matches($inst, '6000\.3\.\d+f\d+') | ForEach-Object { $_.Value } | Select-Object -First 1
  if ($have) { Write-Host "Unity 6.3 ya instalado: $have"; $ver = $have }
  elseif ($vers) {
    $ver = $vers[0]
    Write-Host "Instalando Unity $ver + Windows Build Support (IL2CPP). Son varios GB; puede tardar." -ForegroundColor Yellow
    unity install $ver -m windows-il2cpp
    $inst = (unity editors -i 2>&1 | Out-String); Write-Host $inst
  } else { Write-Host 'No pude obtener la lista de versiones 6000.3 con el Unity CLI. Instalar desde Unity Hub > Installs > Install Editor > Unity 6.3 LTS con Windows Build Support (IL2CPP).' -ForegroundColor Red }
} else { Write-Host 'Unity CLI no disponible. Instalar el editor desde Unity Hub (ver arriba).' -ForegroundColor Red }

$exe = Get-ChildItem -Path "$env:ProgramFiles\Unity\Hub\Editor\6000.3.*\Editor\Unity.exe" -ErrorAction SilentlyContinue | Sort-Object FullName -Descending | Select-Object -First 1
if ($exe) {
  Write-Host "Editor: $($exe.FullName)"
  $il2cpp = Test-Path (Join-Path $exe.DirectoryName 'Data\PlaybackEngines\windowsstandalonesupport\Variations\win64_player_development_il2cpp')
  Write-Host "Modulo Windows IL2CPP presente: $il2cpp"
  $vlog = Join-Path $LogDir 'unity_version.txt'
  $p = Start-Process -FilePath $exe.FullName -ArgumentList '-version' -Wait -PassThru -NoNewWindow -RedirectStandardOutput $vlog
  Write-Host ("Unity -version (exit " + $p.ExitCode + "): " + (Get-Content $vlog -Raw))
} else { Write-Host 'Unity.exe 6000.3 no encontrado en la ruta por defecto del Hub.' }

Step 'Fin'
Write-Host "Registro guardado en: $Log" -ForegroundColor Green
Write-Host 'Licencia: abri Unity Hub > (icono de cuenta) > Manage licenses > Add > Get a free personal license, si todavia no tenes una activa.'
Write-Host 'Despues avisale a Claude en el chat.'
Stop-Transcript | Out-Null
Read-Host 'Enter para cerrar'
