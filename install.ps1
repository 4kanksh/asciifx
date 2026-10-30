# Installs AsciiFX straight from GitHub, for anyone not using the PowerShell Gallery:
#   irm https://raw.githubusercontent.com/4kanksh/asciifx/main/install.ps1 | iex
# It downloads the latest code from the main branch and copies the module into your PowerShell modules
# folder (for both Windows PowerShell and PowerShell 7). Nothing else on your system is changed.

$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12

$work = Join-Path ([IO.Path]::GetTempPath()) ("asciifx-" + [guid]::NewGuid())
New-Item -ItemType Directory $work | Out-Null
try {
    Write-Host 'Downloading AsciiFX...'
    $zip = Join-Path $work 'asciifx.zip'
    Invoke-WebRequest 'https://github.com/4kanksh/asciifx/archive/refs/heads/main.zip' -OutFile $zip -UseBasicParsing
    Expand-Archive $zip $work
    $source = Join-Path $work 'asciifx-main'

    $documents = [Environment]::GetFolderPath('MyDocuments')
    foreach ($modules in (Join-Path $documents 'WindowsPowerShell/Modules'), (Join-Path $documents 'PowerShell/Modules')) {
        $target = Join-Path $modules 'AsciiFX'
        Remove-Item $target -Recurse -Force -ErrorAction SilentlyContinue
        New-Item -ItemType Directory -Force $target | Out-Null
        foreach ($item in 'AsciiFX.psd1', 'AsciiFX.psm1', 'README.md', 'LICENSE', 'core', 'animations') {
            Copy-Item (Join-Path $source $item) $target -Recurse
        }
        Get-ChildItem $target -Recurse -File | Unblock-File
        Write-Host "Installed to $target"
    }
    Write-Host ''
    Write-Host 'Done! Open a new terminal and type: asciifx' -ForegroundColor Green
} finally {
    Remove-Item $work -Recurse -Force -ErrorAction SilentlyContinue
}
