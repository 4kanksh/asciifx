# AsciiFX

**ASCII animations for your terminal and your Windows desktop.**
A wall of fire, a rotating galaxy, a spinning 3D donut and a quiet starry sky. Play them full-screen in the
terminal, or leave them running on the desktop, above your wallpaper and behind your windows.

<p align="center">
  <img src="docs/previews/donut.svg" alt="Spinning 3D donut" width="400" />
  <img src="docs/previews/galaxy.svg" alt="Rotating galaxy" width="400" />
</p>
<p align="center">
  <img src="docs/previews/fire.svg" alt="Wall of fire" width="400" />
  <img src="docs/previews/stars.svg" alt="Starry sky" width="400" />
</p>

---

## Install

In PowerShell:

```powershell
Install-Module AsciiFX -Scope CurrentUser
```

That's it. Open a new terminal window and the commands are ready, with nothing to add to your profile.

<details>
<summary>Other ways to install</summary>

**Straight from GitHub (no PowerShell Gallery):**

```powershell
irm https://raw.githubusercontent.com/4kanksh/asciifx/main/install.ps1 | iex
```

This downloads the latest code and copies it into your PowerShell modules folder. [Read the script first](install.ps1) if you like; it's short.

**By hand:** download this repository as a ZIP, and copy `AsciiFX.psd1`, `AsciiFX.psm1`, `core/` and `animations/`
into a folder named `AsciiFX` inside `Documents\WindowsPowerShell\Modules` (or `Documents\PowerShell\Modules` for PowerShell 7).

</details>

**Update:** `Update-Module AsciiFX`  **Remove:** `desktop-clear; Uninstall-Module AsciiFX`

## Quick start

```powershell
asciifx                          # list every animation
fire                             # play one in the terminal; press any key to stop
desktop-donut -Position top-left # put one on your desktop
desktop-clear                    # take every desktop animation away again
```

The first time you run an animation it takes a few seconds to prepare (it compiles itself once).
After that it starts instantly.

---

## The animations

Every animation has two commands:

| Command | What it does |
|---|---|
| `<name>` | Plays full-screen in the terminal. Press any key (or Ctrl+C) to stop. |
| `desktop-<name>` | Shows it on your desktop, above the wallpaper and behind your windows. See [Desktop mode](#desktop-mode). |

Options work the same way in both. To see every option with its default, use `Get-Help <command> -Detailed`,
for example `Get-Help fire -Detailed`. Press <kbd>Tab</kbd> after a command to cycle through the choices.

### 🔥 fire

A wall of fire: heat rises from the bottom, drifts with the wind and cools as it climbs, while embers float up and sparks shoot out.

```powershell
fire                      # blue flames
fire mocha                # Catppuccin Mocha colors: purple smoke, red, peach and yellow
fire -Wind -6 -Intensity 10 -NoSparks
desktop-fire -Position bottom
```

| Option | Default | What it does |
|---|---|---|
| *(first word)* | `blue` | Colors: `blue` or `mocha` |
| `-Intensity` | 8 | How tall and bright the flames are (1-10) |
| `-Wind` | 4 | Sideways drift: negative blows left, positive blows right |
| `-Decay` | 2.2 | How fast the flames cool; higher means shorter flames |
| `-Turbulence` | 8 | How much the flames wobble |
| `-Thickness` | 3 | Rows of fuel at the bottom |
| `-NoEmbers` `-NoSparks` `-NoPulse` | | Leave out the embers, the sparks, or the slow pulsing |

### 🌌 galaxy

A spiral galaxy that slowly rotates: stars along curling arms around a glowing core, each twinkling on its own rhythm.

```powershell
galaxy                    # blue
galaxy magenta            # purple and pink
galaxy -Arms 4 -Speed 40 -Twist 0.7
desktop-galaxy magenta -Position bottom-right
```

| Option | Default | What it does |
|---|---|---|
| *(first word)* | `blue` | Colors: `blue` or `magenta` |
| `-Arms` | 2 | Number of spiral arms |
| `-Speed` | 20 | Rotation speed, in degrees per second |
| `-Size` | 70 | Percent of the terminal the galaxy fills (terminal only) |
| `-Twist` | 0.45 | How tightly the arms wind |
| `-Noise` | 0.15 | How scattered the arm stars are |
| `-Glow` | 0.45 | Core glow and the halo around stars |
| `-Twinkle` | 0.35 | How much the stars flicker (0-1) |

### 🍩 donut

A spinning 3D donut, lit from one side and shaded with `.,-~:;=!*#$@`. It is real 3D: perspective, a light source,
and the front of the donut hiding the back.

```powershell
donut
donut -Color '#f5c2e7' -Thickness 70
donut -RotationX 3 -RotationY 2 -LightAngle 135
desktop-donut -Position top-left
```

| Option | Default | What it does |
|---|---|---|
| `-Color` | `'#cdd6f4'` | Hex color of the donut |
| `-RotationX` | 2 | Tumble speed (forward and back) |
| `-RotationY` | 1 | Spin speed (left and right) |
| `-Size` | 75 | Percent of the terminal the donut fills (terminal only) |
| `-Thickness` | 50 | Tube thickness (1-100) |
| `-LightAngle` | 45 | Where the light comes from, in degrees (0 = right, 90 = top) |
| `-LightHeight` | 45 | How far in front of the donut the light sits, in degrees |

### ✨ stars

A calm night sky: sparse stars that slowly twinkle, thinning toward the horizon, with a faint shooting star
every half minute or so. Made to sit quietly above a dark or aurora wallpaper.

```powershell
stars
desktop-stars                       # across the top 40% of the screen
desktop-stars-full                  # across the whole screen, evenly spread
desktop-stars -Height 30 -Density 0.6
desktop-stars -NoShootingStars
```

| Option | Default | What it does |
|---|---|---|
| `-Density` | 1 | How many stars: 0.5 is half as many, 2 is twice as many |
| `-Fade` | 0.7 | How much the stars thin out toward the bottom: 0 spreads them evenly, 1 leaves none at the bottom |
| `-Height` | 40 | How far down the screen the sky reaches, in percent (desktop only) |
| `-NoShootingStars` | | Leave out the occasional shooting star |

`desktop-stars-full` is `desktop-stars` with `-Height 100 -Fade 0`, and takes the same options.
There is only ever one starry sky, so starting either one replaces the other.

### Terminal options for every animation

| Option | Default | What it does |
|---|---|---|
| `-Seconds` | 0 | Stop by itself after this many seconds (0 = run until a key is pressed) |
| `-Fps` | | Frames per second, if you want something other than the animation's own speed |

---

## Desktop mode

`desktop-<name>` puts an animation on the desktop itself. It sits **above your wallpaper** (a static picture, or a
live wallpaper from an app like Lively) and **below every window**, so it only shows where you can see the desktop.
Its background is see-through, clicks go straight through it, and it doesn't appear in the taskbar or Alt+Tab.
It keeps running after you close the terminal that started it.

```powershell
desktop-donut                              # center of the screen
desktop-donut -Position top-left           # a named spot
desktop-donut -X 200 -Y 120                # exact pixels from the top-left corner
desktop-donut -Position top-left -Size 10  # -Size is the font size: bigger means a bigger animation
```

**Several at once.** Each spot holds one animation, so starting one somewhere new adds another:

```powershell
desktop-donut -Position top-left
desktop-donut -Position bottom-right -Color '#f5c2e7'
desktop-galaxy magenta -Position top-right
desktop-stars
```

Running a command again for a spot that's already taken **replaces** what's there (handy for changing its size or color).
`desktop-stars` and `desktop-stars-full` cover the screen from the top, so there is only ever one sky.

**Removing them:**

| Command | Removes |
|---|---|
| `desktop-donut -Stop -Position top-left` | Just the donut in that spot |
| `desktop-donut -Stop` | Every donut |
| `desktop-clear` | Every AsciiFX animation on the desktop |

Positions: `center`, `top`, `bottom`, `left`, `right`, `top-left`, `top-right`, `bottom-left`, `bottom-right`.

Desktop animations don't come back after a restart. Run the commands again, or add them to your PowerShell profile
or a startup task if you'd like them every time.

---

## Requirements

- **Windows 10 or 11** with **Windows PowerShell 5.1** (built in) or **PowerShell 7**
- A terminal with true color, such as **Windows Terminal** (the default on Windows 11)
- The terminal animations may also work in PowerShell 7 on macOS and Linux; desktop mode is Windows only

## Troubleshooting

**"The term 'fire' is not recognized"**
Open a new terminal window after installing. If it still isn't found, run `Import-Module AsciiFX` and check for errors.

**"…cannot be loaded because running scripts is disabled on this system"**
Windows blocks PowerShell scripts by default. Allow ones you install yourself with:
`Set-ExecutionPolicy -Scope CurrentUser RemoteSigned`

**The first run takes a few seconds**
That's normal: AsciiFX compiles its animations once and caches them in `%LOCALAPPDATA%\AsciiFX`.

**I can't see the desktop animation**
It sits behind your windows, so minimize them to see it. The "Show desktop" button (or Win+D) can cover it, so minimize instead.
Desktop icons in the same spot show underneath it.

**The colors look wrong or I see odd symbols**
Use a terminal with true color support, like Windows Terminal. The old console window (conhost) shows fewer colors.

**Something got stuck**
`desktop-clear` removes every desktop animation. In the terminal, any key or Ctrl+C stops an animation.

---

## Adding your own animation

Each animation is one small folder: a C# class that draws a frame, plus a short settings file. Everything else
(both commands, the options, tab completion, help, desktop mode) is created from it automatically.

```powershell
./tools/New-Animation.ps1 -Name rain   # copies the template to animations/rain/
```

See **[CONTRIBUTING.md](CONTRIBUTING.md)** for the full walkthrough.

## How it works

- `animations/<name>/` holds a class implementing `IAnimation` (`Resize` and `Frame`: which character and color go in
  each cell at time *t*) and a `.psd1` manifest with its description, palettes and options.
- `AsciiFX.psm1` reads the manifests and generates the `<name>` and `desktop-<name>` commands from them.
- `core/AsciiFX.Core.cs` has the terminal player (full-screen with escape codes); `core/AsciiFX.Desktop.cs` has the
  desktop player: a borderless, click-through window with a see-through background, kept just above the desktop layer.
  Each desktop animation runs as its own hidden Windows PowerShell 5.1 process (built into Windows), even when started from PowerShell 7.
- The C# is compiled on first use and cached by a hash of the source, so it stays readable here and fast when you run it.

## Credits

- The spinning donut follows the classic idea behind Andy Sloane's [donut.c](https://www.a1k0n.net/2011/07/20/donut-math.html).
- Inspired by the animations on [asciiart.eu](https://www.asciiart.eu/animations).
- Colors from [Catppuccin](https://catppuccin.com).

## License

[MIT](LICENSE) © Akanksh Singh
