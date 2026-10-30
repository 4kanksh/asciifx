# Creates a new animation from the template: animations/<name>/<name>.cs and <name>.psd1.
# Then write your Frame() in the .cs file, list your options in the .psd1, and try it out.
#   ./tools/New-Animation.ps1 -Name rain

param(
    [Parameter(Mandatory)]
    [ValidatePattern('^[a-z][a-z0-9]*$')]  # it becomes the command name, so lowercase letters and digits
    [string]$Name
)

$root = Split-Path $PSScriptRoot
$target = Join-Path $root "animations/$Name"
if (Test-Path $target) { throw "animations/$Name already exists." }
$class = $Name.Substring(0, 1).ToUpper() + $Name.Substring(1)

New-Item -ItemType Directory $target | Out-Null
foreach ($ext in 'cs', 'psd1') {
    $text = [IO.File]::ReadAllText((Join-Path $root "animations/_template/template.$ext")) -replace "`r`n", "`n"
    $text = ($text -split "`n" | Where-Object { $_ -notmatch 'New-Animation\.ps1' }) -join "`n"  # the "copy this" note
    $text = $text -creplace 'Template', $class -creplace 'template', $Name
    [IO.File]::WriteAllText((Join-Path $target "$Name.$ext"), $text, (New-Object Text.UTF8Encoding $false))
}
& (Join-Path $PSScriptRoot 'Update-Manifest.ps1')

Write-Host ''
Write-Host "Created animations/$Name/$Name.cs and $Name.psd1. Try it:" -ForegroundColor Green
Write-Host "  Import-Module ./AsciiFX.psd1 -Force -DisableNameChecking; $Name"
Write-Host "Then make it yours: edit Frame() in $Name.cs and the options in $Name.psd1."
Write-Host "When it looks right: ./tools/Make-Preview.ps1 -Name $Name, ./tests/Run-Tests.ps1, and add it to the README."
