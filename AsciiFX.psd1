@{
    RootModule        = 'AsciiFX.psm1'
    ModuleVersion     = '1.2.0'
    GUID              = '98ed09a5-be37-475e-9428-0ab396509e07'
    Author            = 'Akanksh Singh'
    Copyright         = '(c) 2026 Akanksh Singh. MIT License.'
    Description       = 'ASCII animations (fire, galaxy, 3D donut, starry sky) for the terminal, and live on your Windows desktop above the wallpaper.'
    PowerShellVersion = '5.1'

    # Listed explicitly so typing any of these loads the module automatically.
    # tools/Update-Manifest.ps1 regenerates this list from the animations/ folder.
    FunctionsToExport = @(
        'asciifx', 'desktop-clear',
        'donut', 'desktop-donut',
        'fire', 'desktop-fire',
        'fuji-dawn-lake', 'desktop-fuji-dawn-lake',
        'galaxy', 'desktop-galaxy',
        'stars', 'desktop-stars', 'desktop-stars-full'
    )
    CmdletsToExport   = @()
    VariablesToExport = @()
    AliasesToExport   = @()

    PrivateData = @{
        PSData = @{
            Tags         = @('ascii', 'animation', 'terminal', 'desktop', 'wallpaper', 'fire', 'galaxy', 'donut', 'stars', 'Windows')
            LicenseUri   = 'https://github.com/4kanksh/asciifx/blob/main/LICENSE'
            ProjectUri   = 'https://github.com/4kanksh/asciifx'
            ReleaseNotes = 'https://github.com/4kanksh/asciifx/blob/main/CHANGELOG.md'
        }
    }
}
