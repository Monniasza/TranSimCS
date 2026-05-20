param(
    [string]$PublishDir = "bin\Release",
    [string]$NsisPath = "C:\Program Files (x86)\NSIS\makensis.exe",
    [string]$Script = "setup.nsi"
)

if (!(Test-Path $NsisPath)) {
    throw "NSIS not found at: $NsisPath"
}

$NsisPath = (Resolve-Path $NsisPath).Path

# Compute size
$sizeBytes = (Get-ChildItem $PublishDir -Recurse | Measure-Object Length -Sum).Sum
$sizeKb = [math]::Ceiling($sizeBytes / 1KB)

Write-Host "INSTALLSIZE=$sizeKb"

# Run NSIS
& "$NsisPath" @(
    "-DINSTALLSIZE=$sizeKb"
    $Script
)