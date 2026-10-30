@{
    # The command name: `template` plays it in the terminal, `desktop-template` on the desktop
    Name        = 'template'
    Class       = 'AsciiFX.Animations.Template'
    Description = 'Layered waves rolling across the screen'

    # Color schemes the user can pick as the first argument (`template ocean`); the first is the default.
    # Leave empty if there is only one look. Read the choice in C# with o.Text("Palette", ...).
    Palettes    = @()

    # Each option becomes a parameter of both commands.
    #   Type:  int, double, string, or switch (a switch is off unless given, e.g. -NoSparks)
    #   Where: 'terminal' or 'desktop' to offer it in only one of the two commands (default: both)
    Options = @(
        @{ Name = 'Speed'; Type = 'double'; Default = 1; Help = 'How fast the waves roll' }
        @{ Name = 'Waves'; Type = 'int';    Default = 3; Help = 'How many waves' }
    )

    # On the desktop: 'box' is a Cols x Rows window the user can place anywhere (several allowed);
    # 'sky' spans the screen width across the top (only one). Outline adds a dark edge for bright wallpapers.
    # Options here override the defaults above, for the desktop only.
    Desktop = @{ Layout = 'box'; Cols = 60; Rows = 16; FontSize = 12; Outline = $true }

    # Used by tools/Make-Preview.ps1 for the README preview
    Preview = @{ Cols = 60; Rows = 16; Seconds = 4; Fps = 12 }
}
