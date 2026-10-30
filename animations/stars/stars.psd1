@{
    Name        = 'stars'
    Class       = 'AsciiFX.Animations.Stars'
    Description = 'A subtle twinkling night sky with the occasional shooting star'
    Palettes    = @()

    # Each option becomes a parameter of `stars` and `desktop-stars`
    Options = @(
        @{ Name = 'Density';         Type = 'double'; Default = 1;   Help = 'How many stars: 0.5 is half as many, 2 is twice as many' }
        @{ Name = 'Fade';            Type = 'double'; Default = 0.7; Help = 'How much the stars thin out toward the bottom: 0 spreads them evenly, 1 leaves none at the bottom' }
        @{ Name = 'Height';          Type = 'int';    Default = 40;  Help = 'How far down the screen the sky reaches, in percent'; Where = 'desktop' }
        @{ Name = 'NoShootingStars'; Type = 'switch';                Help = 'Leave out the occasional shooting star' }
    )

    # The same animation under its own command, with different defaults (every option still works)
    Variants = @(
        @{ Command = 'desktop-stars-full'; Mode = 'desktop'; Description = 'A starry sky across the whole screen'
           Defaults = @{ Height = 100; Fade = 0 } }
    )

    # "sky" spans the full screen width from the top of the screen down; there is only ever one
    Desktop = @{ Layout = 'sky'; FontSize = 11; Outline = $false }
    Preview = @{ Cols = 80; Rows = 14; Seconds = 6; Fps = 10 }
}
