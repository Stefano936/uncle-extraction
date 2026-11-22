[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$project = Join-Path $repo 'UnityProject'

$versionFile = Get-Content (Join-Path $project 'ProjectSettings/ProjectVersion.txt') -Raw
$editor = [regex]::Match($versionFile, '(?m)^m_EditorVersion: (\d+\.\d+\.\d+f\d+)\s*$').Groups[1].Value
if (!$editor -or $versionFile -notmatch "(?m)^m_EditorVersionWithRevision: $([regex]::Escape($editor)) \([a-f0-9]+\)\s*$") {
    throw 'ProjectVersion.txt debe fijar la misma versión en ambas entradas y declarar una revisión.'
}

$gameVersion = Get-Content (Join-Path $project 'Assets/Game/Scripts/Core/GameVersion.cs') -Raw
$number = [regex]::Match($gameVersion, 'const string Number = "(\d+\.\d+\.\d+)";').Groups[1].Value
$protocol = [regex]::Match($gameVersion, 'const ushort Protocol = ([1-9]\d*);').Groups[1].Value
if (!$number -or !$protocol -or [long]$protocol -gt [ushort]::MaxValue) {
    throw 'GameVersion.cs debe fijar una versión y un protocolo válidos.'
}

$manifest = (Get-Content (Join-Path $project 'Packages/manifest.json') -Raw | ConvertFrom-Json -AsHashtable).dependencies
$locked = (Get-Content (Join-Path $project 'Packages/packages-lock.json') -Raw | ConvertFrom-Json -AsHashtable).dependencies
if (!$manifest -or !$locked) { throw 'Faltan las dependencias del proyecto o el lock de Unity.' }
foreach ($name in $manifest.Keys) {
    if (!$locked.Contains($name) -or $locked[$name].depth -ne 0 -or $locked[$name].version -ne $manifest[$name]) {
        throw "Dependencia directa fuera de sincronía: $name"
    }
}

$buildSettings = Get-Content (Join-Path $project 'ProjectSettings/EditorBuildSettings.asset') -Raw
$enabledScenes = [regex]::Matches($buildSettings, '(?m)^\s*- enabled: 1\r?\n\s+path: ([^\r\n]+)') |
    ForEach-Object { $_.Groups[1].Value.Trim() }
foreach ($required in @('Assets/Game/Scenes/Frontend.unity', 'Assets/Game/Scenes/Bar_LastRound.unity')) {
    if ($required -notin $enabledScenes) { throw "Escena obligatoria deshabilitada o ausente: $required" }
}
foreach ($scene in $enabledScenes) {
    $file = Join-Path $project $scene
    if (!(Test-Path -LiteralPath $file -PathType Leaf) -or !(Test-Path -LiteralPath "$file.meta" -PathType Leaf)) {
        throw "Escena habilitada sin archivo o meta: $scene"
    }
}

$sha = git -C $repo rev-parse HEAD
if ($LASTEXITCODE -ne 0) { throw 'No se pudo identificar el commit.' }
"PASS archivo y configuración | SHA=$sha | Unity=$editor | juego=$number | protocolo=$protocol"
