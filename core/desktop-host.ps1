# Runs one desktop animation window. The desktop-<name> commands start this in a hidden PowerShell;
# it is not meant to be run by hand.

param(
    [string]$Animation,
    [string]$Options,
    [int]$FontSize = 12,
    [string]$Position = 'center',
    [int]$X = -1,
    [int]$Y = -1
)

$module = Import-Module (Join-Path $PSScriptRoot '../AsciiFX.psd1') -PassThru
& $module { Start-DesktopWindow @args } $Animation $Options $FontSize $Position $X $Y
