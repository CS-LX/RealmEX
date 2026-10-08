param([Parameter(Mandatory=$true)][string]$GameDir, [string]$Output = "$PSScriptRoot/../../artifacts/ponder-preview", [switch]$SkipBuild)
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath("$PSScriptRoot/../..")
if (!$SkipBuild) {
    dotnet build "$PSScriptRoot/PonderPreview.csproj" --configuration Test --nologo -v quiet
    if ($LASTEXITCODE -ne 0) { throw 'Ponder preview build failed.' }
}
$hostBin = Join-Path $GameDir 'bin/Debug'
$previewBin = Join-Path $PSScriptRoot 'bin/Test/net10.0'
foreach ($file in @('Content.zip','glfw3.dll','openal32.dll','wrap_oal.dll','openxr_loader.dll')) {
    $source = Join-Path $hostBin $file
    if (Test-Path -LiteralPath $source) { Copy-Item -LiteralPath $source -Destination (Join-Path $previewBin $file) }
}
$outputPath = [IO.Path]::GetFullPath($Output)
New-Item -ItemType Directory -Force -Path $outputPath | Out-Null
$arguments = '"{0}" "{1}"' -f $root,$outputPath
$process = Start-Process -FilePath (Join-Path $previewBin 'PonderPreview.exe') -ArgumentList $arguments -WorkingDirectory $previewBin -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $previewBin 'render.log') -RedirectStandardError (Join-Path $previewBin 'render-errors.log')
$handle = $process.Handle
if (!$process.WaitForExit(45000)) { throw "Ponder preview timed out. Check process $($process.Id) and logs in $previewBin." }
$process.Refresh()
Get-Content (Join-Path $previewBin 'render.log')
Get-Content (Join-Path $previewBin 'render-errors.log')
if ($process.ExitCode -ne 0) { throw "Ponder preview failed: $($process.ExitCode)" }
