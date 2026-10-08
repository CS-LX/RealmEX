<#
.SYNOPSIS
    RealmEX mod packaging script (.scmod = plain ZIP).
#>
param(
    [Parameter(Mandatory)]
    [string]$BuildOutputDir,

    [string]$Configuration = "Release",

    [string]$ModFileName = "RealmEX",

    [string]$ArtifactDir = "",

    [string]$PackageLabel = "",

    [string]$GitSha = "",

    [string]$Version = ""
)

$ErrorActionPreference = "Stop"

function Get-ModinfoVersion {
    $modinfoPath = Join-Path (Split-Path $PSScriptRoot -Parent) "modinfo.json"
    if (-not (Test-Path $modinfoPath)) {
        return $null
    }
    $modinfo = Get-Content $modinfoPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $v = [string]$modinfo.Version
    if ([string]::IsNullOrWhiteSpace($v)) { return $null }
    return $v.Trim()
}

function Resolve-PackageBaseName {
    param(
        [string]$DefaultName,
        [string]$Label,
        [string]$Sha,
        [string]$ExplicitVersion,
        [bool]$IsArtifactOutput
    )

    if ($Label -eq "ci" -and -not [string]::IsNullOrWhiteSpace($Sha)) {
        $shortSha = $Sha.Trim()
        if ($shortSha.Length -gt 7) {
            $shortSha = $shortSha.Substring(0, 7)
        }
        return "RealmEX-ci.$shortSha"
    }

    $releaseVersion = $ExplicitVersion
    if ([string]::IsNullOrWhiteSpace($releaseVersion) -and $IsArtifactOutput -and [string]::IsNullOrWhiteSpace($Label)) {
        $releaseVersion = Get-ModinfoVersion
    }

    if (-not [string]::IsNullOrWhiteSpace($releaseVersion)) {
        return "RealmEX-$releaseVersion"
    }

    return $DefaultName
}

$BuildOutputDir = [IO.Path]::GetFullPath($BuildOutputDir).TrimEnd('\', '/') + '\'
$ScriptDir = $PSScriptRoot
$ConfigPath = Join-Path $ScriptDir "pack.config.json"

if (-not (Test-Path $BuildOutputDir)) {
    Write-Error "[PackMod] ERROR: Build output directory does not exist: $BuildOutputDir"
    exit 1
}

$DestDir = $null
$useArtifactNaming = $false

if (-not [string]::IsNullOrWhiteSpace($ArtifactDir)) {
    $DestDir = $ArtifactDir
    $useArtifactNaming = $true
}
elseif (Test-Path $ConfigPath) {
    $Config = Get-Content $ConfigPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $DestDir = $Config.ModsFolder
    if ($Config.ModFileName) {
        $ModFileName = $Config.ModFileName
    }
}
else {
    Write-Host ""
    Write-Host "[PackMod] INFO: pack.config.json not found and -ArtifactDir not set; skipping deployment." -ForegroundColor Yellow
    Write-Host "[PackMod] Copy tools\pack.config.example.json to tools\pack.config.json for local deploy." -ForegroundColor Yellow
    Write-Host ""
    exit 0
}

if ([string]::IsNullOrWhiteSpace($DestDir)) {
    Write-Error "[PackMod] ERROR: destination directory is empty."
    exit 1
}

if (-not (Test-Path $DestDir)) {
    New-Item -ItemType Directory -Path $DestDir -Force | Out-Null
}

$packageBaseName = Resolve-PackageBaseName `
    -DefaultName $ModFileName `
    -Label $PackageLabel `
    -Sha $GitSha `
    -ExplicitVersion $Version `
    -IsArtifactOutput $useArtifactNaming

$TempZip = Join-Path $env:TEMP ("RealmEX-" + [Guid]::NewGuid().ToString('N') + '.scmod')
$DestFile = Join-Path $DestDir "$packageBaseName.scmod"

Write-Host ""
Write-Host "[PackMod] ----------------------------------------" -ForegroundColor Cyan
Write-Host "[PackMod] Mod     : RealmEX" -ForegroundColor Cyan
Write-Host "[PackMod] Config  : $Configuration" -ForegroundColor Cyan
Write-Host "[PackMod] Source  : $BuildOutputDir" -ForegroundColor Cyan
Write-Host "[PackMod] Target  : $DestFile" -ForegroundColor Cyan
Write-Host "[PackMod] ----------------------------------------" -ForegroundColor Cyan

Write-Host "[PackMod] Compressing (plaintext)..." -ForegroundColor Cyan
# Package current source assets only. Incremental build directories can retain deleted diagnostic templates.
$packageFiles = @(Get-ChildItem -LiteralPath $BuildOutputDir -File | Where-Object { $_.Extension -in '.dll', '.json', '.png', '.pdb' })
$sourceRoot = Split-Path $ScriptDir -Parent
foreach ($directory in @('Assets', 'ThirdParty')) {
    foreach ($sourceFile in Get-ChildItem -LiteralPath (Join-Path $sourceRoot $directory) -File -Recurse) {
        $relative = $sourceFile.FullName.Substring($sourceRoot.Length + 1)
        $builtFile = Join-Path $BuildOutputDir $relative
        if (Test-Path -LiteralPath $builtFile -PathType Leaf) { $packageFiles += Get-Item -LiteralPath $builtFile }
    }
}
try {
    Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem
    $archive = [IO.Compression.ZipFile]::Open($TempZip, [IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($file in $packageFiles) {
            $entry = $file.FullName.Substring($BuildOutputDir.Length).Replace('\', '/')
            [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $file.FullName, $entry, [IO.Compression.CompressionLevel]::Fastest) | Out-Null
        }
    }
    finally { $archive.Dispose() }
    Move-Item -LiteralPath $TempZip -Destination $DestFile -Force
}
finally {
    if (Test-Path -LiteralPath $TempZip) { Remove-Item -LiteralPath $TempZip -Force }
}

Write-Host "[PackMod] OK - Packaged: $DestFile" -ForegroundColor Green
Write-Host ""
