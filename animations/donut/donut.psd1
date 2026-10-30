@{
    Name        = 'donut'
    Class       = 'AsciiFX.Animations.Donut'
    Description = 'A spinning, lit 3D donut'
    Palettes    = @()

    # Each option becomes a parameter of `donut` and `desktop-donut`
    Options = @(
        @{ Name = 'Color';       Type = 'string'; Default = '#cdd6f4'; Help = "Hex color of the donut, e.g. '#f5c2e7'" }
        @{ Name = 'RotationX';   Type = 'double'; Default = 2;         Help = 'Tumble speed (forward and back)' }
        @{ Name = 'RotationY';   Type = 'double'; Default = 1;         Help = 'Spin speed (left and right)' }
        @{ Name = 'Size';        Type = 'int';    Default = 75;        Help = 'Percent of the terminal the donut fills'; Where = 'terminal' }
        @{ Name = 'Thickness';   Type = 'int';    Default = 50;        Help = 'Tube thickness (1-100)' }
        @{ Name = 'LightAngle';  Type = 'double'; Default = 45;        Help = 'Where the light comes from, in degrees (0 = right, 90 = top)' }
        @{ Name = 'LightHeight'; Type = 'double'; Default = 45;        Help = 'How far in front of the donut the light sits, in degrees' }
    )

    Desktop = @{ Layout = 'box'; Cols = 60; Rows = 24; FontSize = 14; Outline = $true; Options = @{ Size = 90 } }
    Preview = @{ Cols = 60; Rows = 24; Seconds = 10.472; Fps = 12 }  # one full turn, so the preview loops seamlessly
}
