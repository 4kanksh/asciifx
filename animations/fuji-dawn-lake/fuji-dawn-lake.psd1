@{
    Name        = 'fuji-dawn-lake'
    Class       = 'AsciiFX.Animations.FujiDawnLake'
    Description = 'A snow-capped volcano at dawn over a mirror lake, with cranes, a boat and drifting mist'
    Palettes    = @()

    # Each option becomes a parameter of `fuji-dawn-lake` and `desktop-fuji-dawn-lake`
    Options = @(
        @{ Name = 'Speed'; Type = 'double'; Default = 1; Help = 'Playback speed: 2 is twice as fast, 0.5 half as fast' }
    )

    # A landscape window for the desktop; -Size makes it bigger or smaller
    Desktop = @{ Layout = 'box'; Cols = 96; Rows = 27; FontSize = 10; Outline = $false }
    Preview = @{ Cols = 96; Rows = 27; Seconds = 6; Fps = 8 }
}
