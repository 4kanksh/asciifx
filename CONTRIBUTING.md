# Adding an animation to AsciiFX

An animation is one folder in `animations/` with two files:

```
animations/rain/
├── rain.cs     the drawing: a C# class implementing IAnimation
└── rain.psd1   the settings: name, description, palettes and options
```

From those, AsciiFX creates the `rain` and `desktop-rain` commands, their options, tab completion and
`Get-Help` text, and runs it in both the terminal and on the desktop. No other file needs editing.

**From a GIF instead?** For a picture, skip the C#: `python tools/New-PictureAnimation.py <image> <name>` writes a
`.picture` file of pixels and a manifest whose `Picture = '<name>.picture'` line makes AsciiFX play it with the
built-in picture player. It saves to `~/.config/asciifx/animations` by default; use `--out animations/<name>` to put
it in the repository instead (only for pictures you have the right to share).

## 1. Create it from the template

From the repository folder:

```powershell
./tools/New-Animation.ps1 -Name rain
Import-Module ./AsciiFX.psd1 -Force -DisableNameChecking
rain
```

You'll see the template's rolling waves. That's your starting point.

## 2. Draw your frames (`rain.cs`)

```csharp
public class Rain : IAnimation
{
    public Rain(Options o)          // read your settings: o.Number("Speed", 1), o.Flag("NoThunder"), o.Text("Palette", "blue")
    public int[] Palette { get; }   // your colors as 0xRRGGBB, e.g. Colors.Gradient(16, 0x1e1e2e, 0x89b4fa)
    public int FrameMs { get; }     // milliseconds between frames (33 is about 30 fps)
    public void Resize(int cols, int rows)                   // called first, and when the area changes size
    public void Frame(double t, char[] chars, int[] colors)  // draw the frame at t seconds
}
```

In `Frame`, fill **every** cell: `chars[k]` is the character and `colors[k]` is an index into `Palette`
(`-1` for an empty cell), where `k = row * cols + column`.

Tips:
- **Use `t` for movement** (seconds since the start) rather than counting frames, so the speed stays the same at any frame rate.
  For particles, keep the previous `t` and move by the difference.
- **Cells are about 2.2 times taller than wide.** Multiply horizontal distances by `Cell.Aspect` to keep circles round.
- **Keep `Frame` fast**: it runs 20-50 times a second. Allocate arrays in `Resize`, not in `Frame`.
- **Stick to plain ASCII characters** (codes 32-126) so it looks right in every terminal and font.
- Helpers: `Colors.Gradient(count, stops...)`, `Colors.At(t, stops...)`, `Colors.Mix(a, b, f)` and `Colors.Parse("#rrggbb", fallback)`.
- The code is compiled as C# 5 in Windows PowerShell, so avoid newer syntax such as `$"..."` strings, `=>` members and `out var`.

## 3. Describe your settings (`rain.psd1`)

```powershell
@{
    Name        = 'rain'                          # the command name
    Class       = 'AsciiFX.Animations.Rain'
    Description = 'Rain falling past a window'    # shown by `asciifx` and in help
    Palettes    = @('blue', 'neon')               # `rain neon`; the first is the default. @() if there's only one look

    Options = @(
        @{ Name = 'Speed';     Type = 'double'; Default = 1; Help = 'How fast the rain falls' }
        @{ Name = 'NoThunder'; Type = 'switch';              Help = 'Leave out the lightning flashes' }
        @{ Name = 'Size';      Type = 'int';    Default = 70; Help = 'Percent of the terminal it fills'; Where = 'terminal' }
    )

    Desktop = @{ Layout = 'box'; Cols = 60; Rows = 20; FontSize = 12; Outline = $true }
    Preview = @{ Cols = 60; Rows = 20; Seconds = 4; Fps = 12 }
}
```

- **Option types:** `int`, `double`, `string`, or `switch` (off unless given; name these `NoSomething` for features that are on by default).
- **`Where`:** `'terminal'` or `'desktop'` offers an option in only one of the two commands.
- **`Desktop.Layout`:** `'box'` is a `Cols` x `Rows` window the user can place anywhere, several at once.
  `'sky'` spans the screen width across the top, one at a time (like `stars`).
- **`Desktop.Options`:** values used on the desktop instead of the defaults, e.g. `@{ Size = 90 }` to fill the window.
- **`Outline`** draws a dark edge behind the characters, which helps on bright wallpapers. Leave it off for subtle animations.
- **`Variants`** (optional) add extra commands for the same animation with other defaults; every option still works on them.
  For example, stars has `desktop-stars-full`:
  ```powershell
  Variants = @(
      @{ Command = 'desktop-stars-full'; Mode = 'desktop'; Description = 'A starry sky across the whole screen'
         Defaults = @{ Height = 100; Fade = 0 } }
  )
  ```
  Run `./tools/Update-Manifest.ps1` after adding one, so the new command loads automatically.

## 4. Check it and add it to the README

```powershell
./tests/Run-Tests.ps1                    # compiles everything and checks every animation's frames
./tools/Make-Preview.ps1 -Name rain      # renders docs/previews/rain.svg for the README
```

Then add a section for it to `README.md` (copy one of the existing ones) and a line to `CHANGELOG.md`.
`New-Animation.ps1` has already added your commands to `AsciiFX.psd1`; if you renamed anything, run
`./tools/Update-Manifest.ps1`.

## Releasing (maintainers)

1. Bump `ModuleVersion` in `AsciiFX.psd1` and add the changes to `CHANGELOG.md`.
2. Commit, then tag and push: `git tag v1.1.0; git push origin main --tags`.
3. The **Publish** workflow runs the tests and publishes to the PowerShell Gallery. It needs the repository
   secret `PSGALLERY_API_KEY`, an API key from [powershellgallery.com](https://www.powershellgallery.com/account/apikeys).
