# Regenerates FunctionsToExport in AsciiFX.psd1 from the animations/ folder, so that typing a new
# animation's command loads the module automatically. New-Animation.ps1 runs this for you.
#   ./tools/Update-Manifest.ps1          update the list
#   ./tools/Update-Manifest.ps1 -Check   only report whether it is up to date (returns $true/$false; used by the tests)

param([switch]$Check)

$root = Split-Path $PSScriptRoot
$manifestPath = Join-Path $root 'AsciiFX.psd1'
$names = Get-ChildItem (Join-Path $root 'animations') -Directory | Where-Object Name -NotLike '_*' | Sort-Object Name |
    ForEach-Object { (Import-PowerShellDataFile (Join-Path $_.FullName "$($_.Name).psd1")).Name }

$lines = @("        'asciifx', 'desktop-clear'") + @($names | ForEach-Object { "        '$_', 'desktop-$_'" })
$block = "FunctionsToExport = @(`n" + ($lines -join ",`n") + "`n    )"

$text = [IO.File]::ReadAllText($manifestPath) -replace "`r`n", "`n"
$updated = $text -replace 'FunctionsToExport = @\([^)]*\)', $block

if ($Check) { return $updated -eq $text }
if ($updated -ne $text) {
    [IO.File]::WriteAllText($manifestPath, $updated, (New-Object Text.UTF8Encoding $false))
    Write-Host "Updated FunctionsToExport: $($names -join ', ')"
}
