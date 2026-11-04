# Regenerates FunctionsToExport in AsciiFX.psd1 from the animations/ folder, so that typing a new
# animation's command loads the module automatically. New-Animation.ps1 runs this for you.
#   ./tools/Update-Manifest.ps1          update the list
#   ./tools/Update-Manifest.ps1 -Check   only report whether it is up to date (returns $true/$false; used by the tests)

param([switch]$Check)

$root = Split-Path $PSScriptRoot
$manifestPath = Join-Path $root 'AsciiFX.psd1'
$animations = Get-ChildItem (Join-Path $root 'animations') -Directory | Where-Object Name -NotLike '_*' | Sort-Object Name |
    ForEach-Object { Import-PowerShellDataFile (Join-Path $_.FullName "$($_.Name).psd1") }
$names = $animations | ForEach-Object Name

# One line per animation: its two commands, then any variant commands
$lines = @("        'asciifx', 'desktop-clear'") + @($animations | ForEach-Object {
    # (Where-Object drops the $null a manifest without Variants would otherwise add)
    $commands = @($_.Name, "desktop-$($_.Name)") + @($_.Variants | Where-Object { $_ } | ForEach-Object Command)
    '        ' + (($commands | ForEach-Object { "'$_'" }) -join ', ')
})
$block = "FunctionsToExport = @(`n" + ($lines -join ",`n") + "`n    )"

$text = [IO.File]::ReadAllText($manifestPath) -replace "`r`n", "`n"
$updated = $text -replace 'FunctionsToExport = @\([^)]*\)', $block

if ($Check) { return $updated -eq $text }
if ($updated -ne $text) {
    [IO.File]::WriteAllText($manifestPath, $updated, (New-Object Text.UTF8Encoding $false))
    Write-Host "Updated FunctionsToExport: $($names -join ', ')"
}
