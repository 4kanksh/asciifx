# AsciiFX: ASCII animations for the terminal and the desktop.
#
# Every folder in animations/ is one animation: a C# class plus a manifest (<name>.psd1) that lists its options.
# This file reads those manifests and creates two commands for each animation:
#   <name>          plays it full-screen in the terminal
#   desktop-<name>  shows it on the desktop, above the wallpaper and behind your windows (Windows only)

$script:Animations = [ordered]@{}
foreach ($dir in Get-ChildItem (Join-Path $PSScriptRoot 'animations') -Directory | Where-Object Name -NotLike '_*' | Sort-Object Name) {
    $manifest = Import-PowerShellDataFile (Join-Path $dir.FullName "$($dir.Name).psd1")
    $script:Animations[$manifest.Name] = $manifest
}

$script:Positions = 'center', 'top', 'bottom', 'left', 'right', 'top-left', 'top-right', 'bottom-left', 'bottom-right'

#region Compiling

# The C# is compiled the first time it is needed and cached under a hash of its source,
# so later sessions just load the cached DLL
function Get-CompiledAssembly([string]$Name, [string[]]$Sources, [string[]]$References) {
    $text = ($Sources | ForEach-Object { [IO.File]::ReadAllText($_) }) -join "`n"
    $key = $text + ($References -join ';') + $PSVersionTable.PSEdition + $PSVersionTable.PSVersion.Major
    $sha = [Security.Cryptography.SHA256]::Create()
    $hash = -join ($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($key))[0..5] | ForEach-Object { $_.ToString('x2') })

    $cache = Join-Path $(if ($env:LOCALAPPDATA) { $env:LOCALAPPDATA } else { Join-Path $HOME '.cache' }) 'AsciiFX'
    New-Item -ItemType Directory -Force $cache | Out-Null
    $dll = Join-Path $cache "$Name.$hash.dll"
    if (-not (Test-Path $dll)) {
        Write-Host 'AsciiFX: preparing animations (first run only)...' -ForegroundColor DarkGray
        # Compile from copies in the cache folder: the C# compiler can't read files from very long paths
        $staging = Join-Path $cache "src-$hash"
        New-Item -ItemType Directory -Force $staging | Out-Null
        try {
            $copies = foreach ($source in $Sources) {
                $copy = Join-Path $staging ([IO.Path]::GetFileName($source))
                Copy-Item $source $copy -Force
                $copy
            }
            $compile = @{ Path = $copies; OutputAssembly = $dll; OutputType = 'Library' }
            if ($References) { $compile.ReferencedAssemblies = $References }
            Add-Type @compile
        } finally {
            Remove-Item $staging -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
    $dll
}

function Initialize-AsciiFX([switch]$Desktop) {
    if (-not ('AsciiFX.IAnimation' -as [type])) {
        $sources = @(Join-Path $PSScriptRoot 'core/AsciiFX.Core.cs') +
            @(Get-ChildItem (Join-Path $PSScriptRoot 'animations') -Recurse -Filter *.cs |
                Where-Object { $_.Directory.Name -notlike '_*' } | Sort-Object FullName | ForEach-Object FullName)
        $dll = Get-CompiledAssembly 'AsciiFX.Animations' $sources
        if (-not ('AsciiFX.IAnimation' -as [type])) { Add-Type -Path $dll }
    }
    if ($Desktop -and -not ('AsciiFX.DesktopPlayer' -as [type])) {
        Add-Type -AssemblyName System.Windows.Forms, System.Drawing
        # PowerShell 7 also needs the libraries Windows Forms builds on named explicitly
        $winForms = if ($PSVersionTable.PSEdition -eq 'Core') {
            [System.Windows.Forms.Form], [System.Drawing.Graphics], [System.Drawing.Rectangle], [System.ComponentModel.Component] |
                ForEach-Object { $_.Assembly.Location } | Select-Object -Unique
        } else { 'System.Windows.Forms', 'System.Drawing' }
        $references = @([AsciiFX.IAnimation].Assembly.Location) + $winForms
        $dll = Get-CompiledAssembly 'AsciiFX.Desktop' @(Join-Path $PSScriptRoot 'core/AsciiFX.Desktop.cs') $references
        if (-not ('AsciiFX.DesktopPlayer' -as [type])) { Add-Type -Path $dll }
    }
}

#endregion

#region Running animations

# What an animation gets: its manifest defaults, then the desktop overrides, then whatever was typed
function Get-AnimationOptions($Animation, [string]$Mode, [Collections.IDictionary]$Bound) {
    $options = @{}
    if ($Animation.Palettes) { $options.Palette = $Animation.Palettes[0] }
    foreach ($o in $Animation.Options) {
        if ($o.Type -ne 'switch' -and $null -ne $o.Default) { $options[$o.Name] = $o.Default }
    }
    if ($Mode -eq 'desktop' -and $Animation.Desktop.Options) {
        foreach ($key in $Animation.Desktop.Options.Keys) { $options[$key] = $Animation.Desktop.Options[$key] }
    }
    foreach ($key in $Bound.Keys) {
        $options[$key] = if ($Bound[$key] -is [switch]) { $Bound[$key].IsPresent } else { $Bound[$key] }
    }
    $options
}

function New-AnimationInstance($Animation, [hashtable]$Options) {
    [Activator]::CreateInstance(($Animation.Class -as [type]), @(, [AsciiFX.Options]::new($Options)))
}

function Invoke-TerminalAnimation([string]$Name, [Collections.IDictionary]$Bound) {
    Initialize-AsciiFX
    $options = Get-AnimationOptions $script:Animations[$Name] 'terminal' $Bound
    $seconds = if ($options.Seconds) { [double]$options.Seconds } else { 0 }
    $fps = if ($options.Fps) { [int]$options.Fps } else { 0 }
    [AsciiFX.TerminalPlayer]::Run((New-AnimationInstance $script:Animations[$Name] $options), $seconds, $fps)
}

function Test-Windows { $PSVersionTable.PSEdition -eq 'Desktop' -or $IsWindows }

# Every desktop animation is its own hidden PowerShell running core/desktop-host.ps1. Its spot
# (-Position/-X/-Y) is the end of that command line, which is how one gets found again to replace or remove.
function Stop-DesktopAnimation([string]$Name = '*', [string]$Spot = '') {
    Get-CimInstance Win32_Process -Filter "Name = 'powershell.exe' OR Name = 'pwsh.exe'" |
        Where-Object { $_.CommandLine -like "*desktop-host.ps1*-Animation $Name *$Spot" } |
        ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
}

function Invoke-DesktopAnimation([string]$Name, [Collections.IDictionary]$Bound) {
    if (-not (Test-Windows)) { Write-Error 'Desktop animations need Windows.'; return }
    $animation = $script:Animations[$Name]
    $sky = $animation.Desktop.Layout -eq 'sky'
    $spot = if ($sky) { '-Position sky -X 0 -Y 0' } else {
        "-Position $(if ($Bound.Position) { $Bound.Position } else { 'center' }) " +
        "-X $(if ($Bound.ContainsKey('X')) { $Bound.X } else { -1 }) -Y $(if ($Bound.ContainsKey('Y')) { $Bound.Y } else { -1 })"
    }

    # -Stop alone removes every one of this animation; with a spot, just that one
    $spotGiven = @($Bound.Keys | Where-Object { $_ -in 'Position', 'X', 'Y' }).Count -gt 0
    Stop-DesktopAnimation $Name $(if ($Bound.Stop -and ($sky -or -not $spotGiven)) { '' } else { $spot })
    if ($Bound.Stop) { return }

    # -Size is the font size here, not the animation's own Size option
    $typed = @{}
    foreach ($key in $Bound.Keys) { $typed[$key] = $Bound[$key] }
    $fontSize = if ($typed.ContainsKey('Size')) { $typed.Size } else { $animation.Desktop.FontSize }
    $typed.Remove('Size')
    $options = Get-AnimationOptions $animation 'desktop' $typed
    $own = @($animation.Options | ForEach-Object Name) + 'Palette'
    $encoded = ($options.GetEnumerator() | Where-Object { $_.Key -in $own -or $animation.Desktop.Options.Keys -contains $_.Key } |
        ForEach-Object { "$($_.Key)=$($_.Value)" }) -join ';'

    # Launched through WMI, so it keeps running after the terminal that started it closes
    $exe = (Get-Process -Id $PID).Path
    if ($exe -notmatch '(powershell|pwsh)\.exe$') { $exe = 'powershell.exe' }
    $hostScript = Join-Path $PSScriptRoot 'core/desktop-host.ps1'
    $hidden = New-CimInstance -ClassName Win32_ProcessStartup -ClientOnly -Property @{ ShowWindow = [uint16]0 }
    $null = Invoke-CimMethod -ClassName Win32_Process -MethodName Create -Arguments @{
        CommandLine = "`"$exe`" -NoProfile -ExecutionPolicy Bypass -File `"$hostScript`" -Animation $Name " +
                      "-Options `"$encoded`" -FontSize $fontSize $spot"
        ProcessStartupInformation = $hidden
    }
}

# Called inside the hidden PowerShell that desktop-host.ps1 starts
function Start-DesktopWindow([string]$Name, [string]$Encoded, [int]$FontSize, [string]$Position, [int]$X, [int]$Y) {
    Initialize-AsciiFX -Desktop
    $animation = $script:Animations[$Name]
    $options = @{}
    foreach ($pair in $Encoded -split ';') { if ($pair -match '^(.+?)=(.*)$') { $options[$Matches[1]] = $Matches[2] } }
    $height = if ($options.Height) { [int]$options.Height } else { 40 }
    $d = $animation.Desktop
    [AsciiFX.DesktopPlayer]::Run((New-AnimationInstance $animation $options), $Name, $FontSize, $d.Layout,
        [int]$d.Cols, [int]$d.Rows, $height, [bool]$d.Outline, $Position, $X, $Y)
}

#endregion

#region Commands

function Format-Default($Value) {
    if ($Value -is [string]) { "'$Value'" } else { [string]::Format([Globalization.CultureInfo]::InvariantCulture, '{0}', $Value) }
}

# Builds the source of `<name>` (Mode terminal) or `desktop-<name>` (Mode desktop) from a manifest, help included
function New-CommandText($Animation, [string]$Mode) {
    $name = $Animation.Name
    $command = if ($Mode -eq 'desktop') { "desktop-$name" } else { $name }
    $params = New-Object Collections.Generic.List[string]
    $help = New-Object Collections.Generic.List[string]

    $help.Add('.SYNOPSIS')
    $help.Add($(if ($Mode -eq 'desktop') { "$($Animation.Description), on the desktop." } else { "$($Animation.Description), in the terminal." }))
    $help.Add('.DESCRIPTION')
    $help.Add($(if ($Mode -eq 'desktop') {
        'Shows the animation on the desktop: above the wallpaper, behind your windows, and click-through. ' +
        'Run it again for another spot, or with -Stop to remove it. Windows only.'
    } else { 'Plays full-screen in the terminal. Press any key to stop.' }))

    if ($Animation.Palettes) {
        $params.Add("[Parameter(Position = 0)][ValidateSet($(($Animation.Palettes | ForEach-Object { "'$_'" }) -join ', '))][string]`$Palette = '$($Animation.Palettes[0])'")
        $help.Add('.PARAMETER Palette'); $help.Add("Color scheme: $($Animation.Palettes -join ', ')")
    }
    if ($Mode -eq 'terminal') {
        $params.Add('[double]$Seconds = 0'); $help.Add('.PARAMETER Seconds'); $help.Add('Stop after this many seconds (0 = until a key is pressed)')
        $params.Add('[int]$Fps = 0'); $help.Add('.PARAMETER Fps'); $help.Add('Frames per second (0 = what the animation looks best at)')
    } else {
        $params.Add('[switch]$Stop'); $help.Add('.PARAMETER Stop')
        $help.Add($(if ($Animation.Desktop.Layout -eq 'sky') { 'Remove it' } else { 'Remove every one; with -Position/-X/-Y, just the one there' }))
        $params.Add("[int]`$Size = $($Animation.Desktop.FontSize)"); $help.Add('.PARAMETER Size'); $help.Add('Font size; bigger makes the whole animation bigger')
        if ($Animation.Desktop.Layout -ne 'sky') {
            $params.Add("[ValidateSet($(($script:Positions | ForEach-Object { "'$_'" }) -join ', '))][string]`$Position = 'center'")
            $help.Add('.PARAMETER Position'); $help.Add('Where on the screen: center, a side, or a corner')
            $params.Add('[int]$X = -1'); $help.Add('.PARAMETER X'); $help.Add('Exact pixels from the left of the screen (overrides -Position)')
            $params.Add('[int]$Y = -1'); $help.Add('.PARAMETER Y'); $help.Add('Exact pixels from the top of the screen (overrides -Position)')
        }
    }

    foreach ($o in $Animation.Options) {
        $where = if ($o.Where) { $o.Where } else { 'both' }
        if ($where -ne 'both' -and $where -ne $Mode) { continue }
        $params.Add($(if ($o.Type -eq 'switch') { "[switch]`$$($o.Name)" } else { "[$($o.Type)]`$$($o.Name) = $(Format-Default $o.Default)" }))
        $help.Add(".PARAMETER $($o.Name)")
        $help.Add($(if ($o.Type -eq 'switch') { $o.Help } else { "$($o.Help). Default: $($o.Default)" }))
    }

    $help.Add('.EXAMPLE')
    $help.Add($(if ($Animation.Palettes) { "$command $($Animation.Palettes[-1])" } else { $command }))
    $invoke = if ($Mode -eq 'desktop') { 'Invoke-DesktopAnimation' } else { 'Invoke-TerminalAnimation' }
    "function $command {`n<#`n$($help -join "`n")`n#>`nparam(`n    $($params -join ",`n    ")`n)`n$invoke '$name' `$PSBoundParameters`n}"
}

$script:Commands = foreach ($animation in $script:Animations.Values) {
    foreach ($mode in 'terminal', 'desktop') {
        . ([scriptblock]::Create((New-CommandText $animation $mode)))
        if ($mode -eq 'desktop') { "desktop-$($animation.Name)" } else { $animation.Name }
    }
}

function asciifx {
    <#
    .SYNOPSIS
    Lists the AsciiFX animations and the commands that start them.
    #>
    $script:Animations.Values | ForEach-Object {
        $name = $_.Name
        [pscustomobject]@{
            Animation   = $name
            Terminal    = (@($name) + @($_.Palettes | Select-Object -Skip 1 | ForEach-Object { "$name $_" })) -join ', '
            Desktop     = "desktop-$name"
            Description = $_.Description
        }
    } | Format-Table -AutoSize -Wrap
    'Options for any of them: Get-Help <command> -Detailed     Remove every desktop animation: desktop-clear'
}

function desktop-clear {
    <#
    .SYNOPSIS
    Removes every AsciiFX animation from the desktop.
    #>
    if (Test-Windows) { Stop-DesktopAnimation }
}

#endregion

Export-ModuleMember -Function (@('asciifx', 'desktop-clear') + $script:Commands)
