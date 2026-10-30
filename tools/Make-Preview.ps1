# Renders README previews: an animated SVG per animation at docs/previews/<name>.svg.
# Every frame is a block of colored text that CSS shows in turn, so it plays anywhere SVGs do, GitHub included.
#   ./tools/Make-Preview.ps1               all animations
#   ./tools/Make-Preview.ps1 -Name donut   just one

param([string[]]$Name)

$root = Split-Path $PSScriptRoot
$module = Import-Module (Join-Path $root 'AsciiFX.psd1') -Force -DisableNameChecking -PassThru
& $module { Initialize-AsciiFX }
if (-not $Name) { $Name = & $module { $script:Animations.Keys } }
$outDir = Join-Path $root 'docs/previews'
New-Item -ItemType Directory -Force $outDir | Out-Null

$fontSize = 12; $lineHeight = 16; $charWidth = 7.22; $pad = 20

foreach ($n in $Name) {
    $animation = & $module { param($n) $script:Animations[$n] } $n
    if (-not $animation) { Write-Error "No animation named '$n'."; continue }
    $p = $animation.Preview
    $anim = & $module { param($a) New-AnimationInstance $a (Get-AnimationOptions $a 'terminal' @{}) } $animation
    $anim.Resize($p.Cols, $p.Rows)
    $chars = New-Object char[] ($p.Cols * $p.Rows)
    $colors = New-Object int[] ($p.Cols * $p.Rows)
    $hex = $anim.Palette | ForEach-Object { '#{0:x6}' -f $_ }
    $count = [int][Math]::Round($p.Seconds * $p.Fps)
    $dt = $p.Seconds / $count
    $width = [int]($p.Cols * $charWidth + 2 * $pad)
    $height = $p.Rows * $lineHeight + 2 * $pad

    $svg = New-Object Text.StringBuilder
    [void]$svg.AppendLine("<svg xmlns=`"http://www.w3.org/2000/svg`" width=`"$width`" height=`"$height`" viewBox=`"0 0 $width $height`" role=`"img`" aria-label=`"$($animation.Description)`">")
    [void]$svg.AppendLine('<style>')
    [void]$svg.AppendLine("text{font-family:'JetBrains Mono',Consolas,'DejaVu Sans Mono',Menlo,monospace;font-size:${fontSize}px;white-space:pre;visibility:hidden;animation:show $($p.Seconds)s infinite}")
    [void]$svg.AppendLine("@keyframes show{0%{visibility:visible}$([Math]::Round(100 / $count, 4))%{visibility:hidden}100%{visibility:hidden}}")
    [void]$svg.AppendLine('</style>')
    [void]$svg.AppendLine("<rect width=`"$width`" height=`"$height`" rx=`"12`" fill=`"#1e1e2e`"/>")

    for ($i = 0; $i -lt $count; $i++) {
        $anim.Frame($i * $dt, $chars, $colors)
        [void]$svg.Append("<text xml:space=`"preserve`" style=`"animation-delay:$([Math]::Round($i * $dt, 3))s`">")
        for ($y = 0; $y -lt $p.Rows; $y++) {
            $row = $y * $p.Cols
            $last = $p.Cols - 1
            while ($last -ge 0 -and $colors[$row + $last] -lt 0) { $last-- }
            if ($last -lt 0) { continue }
            [void]$svg.Append("<tspan x=`"$pad`" y=`"$($pad + ($y + 1) * $lineHeight - 4)`">")
            $start = 0
            while ($start -le $last) {
                $c = $colors[$row + $start]; $end = $start + 1
                while ($end -le $last -and $colors[$row + $end] -eq $c) { $end++ }
                $text = if ($c -lt 0) { ' ' * ($end - $start) } else {
                    [Security.SecurityElement]::Escape((New-Object string ($chars, ($row + $start), ($end - $start))))
                }
                [void]$svg.Append($(if ($c -lt 0) { $text } else { "<tspan fill=`"$($hex[$c])`">$text</tspan>" }))
                $start = $end
            }
            [void]$svg.Append('</tspan>')
        }
        [void]$svg.AppendLine('</text>')
    }
    [void]$svg.AppendLine('</svg>')

    $out = Join-Path $outDir "$n.svg"
    [IO.File]::WriteAllText($out, $svg.ToString(), (New-Object Text.UTF8Encoding $false))
    '{0,-8} {1} frames, {2:N0} KB' -f $n, $count, ((Get-Item $out).Length / 1KB)
}
