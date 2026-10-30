# Changelog

## 1.1.0 — 2026-10-31

- **`desktop-stars-full`**: a starry sky across the whole screen, evenly spread
- **stars**: new `-Fade` option for how much the stars thin out toward the bottom (0 spreads them evenly)
- The desktop sky now starts at the very top of the screen, so `-Height 100` covers all of it
- Animations can now declare *variants* in their manifest: extra commands with different defaults

## 1.0.0 — 2026-10-31

First release.

- **fire**: a wall of fire with embers and sparks, in `blue` and Catppuccin `mocha`
- **galaxy**: a rotating spiral galaxy, in `blue` and `magenta`
- **donut**: a spinning, lit 3D donut in any color
- **stars**: a subtle twinkling sky with the occasional shooting star
- Every animation plays in the terminal (`<name>`) and on the desktop (`desktop-<name>`), above the wallpaper
  and behind your windows, with several at once in different spots
- `asciifx` lists the animations; `desktop-clear` removes every desktop animation
- New animations are a folder with a C# class and a settings file; see CONTRIBUTING.md
