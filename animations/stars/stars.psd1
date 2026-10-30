@{
    Name        = 'stars'
    Class       = 'AsciiFX.Animations.Stars'
    Description = 'A subtle twinkling night sky with the occasional shooting star'
    Palettes    = @()

    # Each option becomes a parameter of `stars` and `desktop-stars`
    Options = @(
        @{ Name = 'Density';         Type = 'double'; Default = 1;  Help = 'How many stars: 0.5 is half as many, 2 is twice as many' }
        @{ Name = 'Height';          Type = 'int';    Default = 40; Help = 'How far down the screen the sky reaches, in percent'; Where = 'desktop' }
        @{ Name = 'NoShootingStars'; Type = 'switch';               Help = 'Leave out the occasional shooting star' }
    )

    # "sky" spans the full screen width and the top part of the screen; there is only ever one
    Desktop = @{ Layout = 'sky'; FontSize = 11; Outline = $false }
    Preview = @{ Cols = 80; Rows = 14; Seconds = 6; Fps = 10 }
}
