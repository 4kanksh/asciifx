@{
    Name        = 'fire'
    Class       = 'AsciiFX.Animations.Fire'
    Description = 'A wall of fire with drifting embers and sparks'
    Palettes    = @('blue', 'mocha')

    # Each option becomes a parameter of `fire` and `desktop-fire`
    Options = @(
        @{ Name = 'Intensity';  Type = 'int';    Default = 8;   Help = 'How tall and bright the flames are (1-10)' }
        @{ Name = 'Wind';       Type = 'int';    Default = 4;   Help = 'Sideways drift: negative blows left, positive blows right' }
        @{ Name = 'Decay';      Type = 'double'; Default = 2.2; Help = 'How fast the flames cool; higher means shorter flames' }
        @{ Name = 'Turbulence'; Type = 'int';    Default = 8;   Help = 'How much the flames wobble' }
        @{ Name = 'Thickness';  Type = 'int';    Default = 3;   Help = 'Rows of fuel at the bottom' }
        @{ Name = 'NoEmbers';   Type = 'switch';                Help = 'Leave out the slowly rising embers' }
        @{ Name = 'NoSparks';   Type = 'switch';                Help = 'Leave out the fast sparks' }
        @{ Name = 'NoPulse';    Type = 'switch';                Help = 'Keep the flame height steady' }
    )

    Desktop = @{ Layout = 'box'; Cols = 80; Rows = 22; FontSize = 12; Outline = $false }
    Preview = @{ Cols = 64; Rows = 18; Seconds = 3; Fps = 10 }
}
