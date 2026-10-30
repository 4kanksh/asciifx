@{
    Name        = 'galaxy'
    Class       = 'AsciiFX.Animations.Galaxy'
    Description = 'A rotating spiral galaxy with a glowing core and twinkling stars'
    Palettes    = @('blue', 'magenta')

    # Each option becomes a parameter of `galaxy` and `desktop-galaxy`
    Options = @(
        @{ Name = 'Arms';    Type = 'int';    Default = 2;    Help = 'Number of spiral arms' }
        @{ Name = 'Speed';   Type = 'double'; Default = 20;   Help = 'Rotation speed, in degrees per second' }
        @{ Name = 'Size';    Type = 'int';    Default = 70;   Help = 'Percent of the terminal the galaxy fills'; Where = 'terminal' }
        @{ Name = 'Twist';   Type = 'double'; Default = 0.45; Help = 'How tightly the arms wind' }
        @{ Name = 'Noise';   Type = 'double'; Default = 0.15; Help = 'How scattered the arm stars are' }
        @{ Name = 'Glow';    Type = 'double'; Default = 0.45; Help = 'Core glow and the halo around stars' }
        @{ Name = 'Twinkle'; Type = 'double'; Default = 0.35; Help = 'How much the stars flicker (0-1)' }
    )

    # On the desktop the galaxy fills its own window, so Size is fixed there
    Desktop = @{ Layout = 'box'; Cols = 72; Rows = 30; FontSize = 12; Outline = $true; Options = @{ Size = 95 } }
    Preview = @{ Cols = 64; Rows = 26; Seconds = 6; Fps = 12 }
}
