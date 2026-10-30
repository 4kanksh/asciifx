# Publishes AsciiFX to the PowerShell Gallery. The GitHub "Publish" workflow runs this when a version tag
# (like v1.0.1) is pushed; it can also be run by hand:  ./tools/Publish.ps1 -ApiKey <your gallery API key>
# Only the files users need are published: the module, core/, animations/ (without the template), README and LICENSE.

param([string]$ApiKey = $env:PSGALLERY_API_KEY)

$ErrorActionPreference = 'Stop'
if (-not $ApiKey) { throw 'Pass -ApiKey, or set PSGALLERY_API_KEY.' }

$root = Split-Path $PSScriptRoot
$version = (Import-PowerShellDataFile (Join-Path $root 'AsciiFX.psd1')).ModuleVersion
if ($env:GITHUB_REF_NAME -and $env:GITHUB_REF_NAME -ne "v$version") {
    throw "Tag $env:GITHUB_REF_NAME does not match ModuleVersion $version in AsciiFX.psd1."
}

# Publish-Module wants a folder named after the module
$temp = if ($env:RUNNER_TEMP) { $env:RUNNER_TEMP } else { [IO.Path]::GetTempPath() }
$stage = Join-Path $temp 'asciifx-publish/AsciiFX'
Remove-Item (Split-Path $stage) -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force $stage | Out-Null
foreach ($item in 'AsciiFX.psd1', 'AsciiFX.psm1', 'README.md', 'LICENSE', 'core', 'animations') {
    Copy-Item (Join-Path $root $item) $stage -Recurse
}
Remove-Item (Join-Path $stage 'animations/_template') -Recurse -Force

Publish-Module -Path $stage -NuGetApiKey $ApiKey -Verbose
Write-Host "Published AsciiFX $version."
