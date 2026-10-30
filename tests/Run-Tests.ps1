# Checks that every animation compiles, draws sensible frames, and gets its commands.
# Works in Windows PowerShell 5.1 and PowerShell 7:  ./tests/Run-Tests.ps1   (exits with 1 if anything fails)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot
$failures = New-Object Collections.Generic.List[string]

function Test-Case([string]$What, [scriptblock]$Test) {
    try {
        $result = & $Test
        if ($result -contains $false) { throw 'check returned false' }
        Write-Host "  ok    $What" -ForegroundColor Green
    } catch {
        $failures.Add("${What}: $_")
        Write-Host "  FAIL  ${What}: $_" -ForegroundColor Red
    }
}

# Draws a few frames and checks every cell holds a character and a valid color index
function Test-Frames($Anim, [int]$Cols, [int]$Rows) {
    $Anim.Resize($Cols, $Rows)
    $chars = New-Object char[] ($Cols * $Rows)
    $colors = New-Object int[] ($Cols * $Rows)
    $drawn = 0
    foreach ($t in 0, 0.4, 1.3, 2.7, 5.0) {
        $Anim.Frame($t, $chars, $colors)
        for ($k = 0; $k -lt $colors.Length; $k++) {
            if ($colors[$k] -lt -1 -or $colors[$k] -ge $Anim.Palette.Length) { throw "color index $($colors[$k]) out of range at t=$t" }
            if ($colors[$k] -ge 0) {
                if ([int]$chars[$k] -lt 32 -or [int]$chars[$k] -gt 126) { throw "non-printable character at t=$t" }
                $drawn++
            }
        }
    }
    # A tiny area can legitimately be empty (a sparse starfield), so only larger ones must show something
    if ($drawn -eq 0 -and $Cols * $Rows -ge 1000) { throw "nothing drawn at ${Cols}x$Rows" }
}

Write-Host "AsciiFX tests on PowerShell $($PSVersionTable.PSVersion) ($($PSVersionTable.PSEdition))"
$module = Import-Module (Join-Path $root 'AsciiFX.psd1') -Force -DisableNameChecking -PassThru
$isWindowsHost = $PSVersionTable.PSEdition -eq 'Desktop' -or $IsWindows

Test-Case 'core and animations compile' { & $module { Initialize-AsciiFX }; [bool]('AsciiFX.IAnimation' -as [type]) }
# The desktop window always runs in Windows PowerShell 5.1, whichever PowerShell starts it
if ($PSVersionTable.PSEdition -eq 'Desktop') {
    Test-Case 'desktop player compiles' { & $module { Initialize-AsciiFX -Desktop }; [bool]('AsciiFX.DesktopPlayer' -as [type]) }
} elseif ($isWindowsHost) {
    Test-Case 'Windows PowerShell 5.1 is there to run desktop animations' {
        Test-Path (Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe')
    }
}
Test-Case 'manifest exports every command (run tools/Update-Manifest.ps1 if not)' { & (Join-Path $root 'tools/Update-Manifest.ps1') -Check }
Test-Case 'asciifx lists every animation' {
    $listing = asciifx | Out-String
    & $module { $script:Animations.Keys } | ForEach-Object { $listing -match [regex]::Escape($_) }
}

foreach ($name in & $module { $script:Animations.Keys }) {
    $animation = & $module { param($n) $script:Animations[$n] } $name
    Test-Case "${name}: has its terminal and desktop commands" {
        [bool](Get-Command $name -Module AsciiFX -ErrorAction SilentlyContinue)
        [bool](Get-Command "desktop-$name" -Module AsciiFX -ErrorAction SilentlyContinue)
    }
    Test-Case "${name}: every option has a name, type and help" {
        foreach ($o in $animation.Options) { $o.Name -and $o.Type -in 'int', 'double', 'string', 'switch' -and $o.Help }
    }
    foreach ($variant in $animation.Variants) {
        Test-Case "${name}: variant $($variant.Command) has its command and only known options" {
            [bool](Get-Command $variant.Command -Module AsciiFX -ErrorAction SilentlyContinue)
            $variant.Mode -in 'terminal', 'desktop'
            $known = @($animation.Options | ForEach-Object Name)
            foreach ($key in $variant.Defaults.Keys) { $key -in $known }
        }
        Test-Case "${name}: variant $($variant.Command) draws frames" {
            $options = & $module { param($a, $m, $d) Get-AnimationOptions $a $m $d } $animation $variant.Mode $variant.Defaults
            $anim = & $module { param($a, $o) New-AnimationInstance $a $o } $animation $options
            Test-Frames $anim 240 60
        }
    }
    $palettes = @($animation.Palettes)
    if ($palettes.Count -eq 0) { $palettes = @('') }
    foreach ($palette in $palettes) {
        $label = if ($palette) { "$name $palette" } else { $name }
        Test-Case "${label}: draws frames at several sizes" {
            $options = & $module { param($a) Get-AnimationOptions $a 'terminal' @{} } $animation
            if ($palette) { $options.Palette = $palette }
            $anim = & $module { param($a, $o) New-AnimationInstance $a $o } $animation $options
            Test-Frames $anim 80 24; Test-Frames $anim 200 50; Test-Frames $anim 12 6
        }
    }
    Test-Case "${name}: draws frames with its desktop settings" {
        $options = & $module { param($a) Get-AnimationOptions $a 'desktop' @{} } $animation
        $anim = & $module { param($a, $o) New-AnimationInstance $a $o } $animation $options
        if ($animation.Desktop.Layout -eq 'sky') { Test-Frames $anim 240 28 } else { Test-Frames $anim $animation.Desktop.Cols $animation.Desktop.Rows }
    }
}

Test-Case 'the template for new animations compiles and draws' {
    $source = [IO.File]::ReadAllText((Join-Path $root 'animations/_template/template.cs'))
    if (-not ('AsciiFX.Animations.Template' -as [type])) { Add-Type -TypeDefinition $source -ReferencedAssemblies ([AsciiFX.IAnimation].Assembly.Location) }
    $options = New-Object AsciiFX.Options (@{})
    Test-Frames (New-Object AsciiFX.Animations.Template $options) 60 16
}

Write-Host ''
if ($failures.Count) {
    Write-Host "$($failures.Count) failed." -ForegroundColor Red
    exit 1
}
Write-Host 'All tests passed.' -ForegroundColor Green
