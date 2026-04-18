[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [string]$Version = 'latest',
    [string]$Repo = 'maffaciolli/MdPdf',
    [string]$InstallRoot = "$env:LOCALAPPDATA\Programs\MdPdf"
)

$ErrorActionPreference = 'Stop'

function Get-DownloadUrl {
    param(
        [string]$RepoName,
        [string]$RequestedVersion
    )

    if ($RequestedVersion -eq 'latest') {
        return "https://github.com/$RepoName/releases/latest/download/mdpdf-win-x64.zip"
    }

    return "https://github.com/$RepoName/releases/download/$RequestedVersion/mdpdf-win-x64.zip"
}

function Ensure-UserPathContains {
    param(
        [string]$BinDirectory
    )

    $currentPath = [Environment]::GetEnvironmentVariable('Path', 'User')
    $pathEntries = @()

    if (-not [string]::IsNullOrWhiteSpace($currentPath)) {
        $pathEntries = @($currentPath -split ';' | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    }

    if ($pathEntries -contains $BinDirectory) {
        $env:Path = if ([string]::IsNullOrWhiteSpace($env:Path)) {
            $BinDirectory
        }
        elseif (-not (($env:Path -split ';') -contains $BinDirectory)) {
            "$env:Path;$BinDirectory"
        }
        else {
            $env:Path
        }

        return
    }

    $updatedPath = if ([string]::IsNullOrWhiteSpace($currentPath)) {
        $BinDirectory
    }
    else {
        "$currentPath;$BinDirectory"
    }

    [Environment]::SetEnvironmentVariable('Path', $updatedPath, 'User')
    $env:Path = if ([string]::IsNullOrWhiteSpace($env:Path)) {
        $updatedPath
    }
    elseif (-not (($env:Path -split ';') -contains $BinDirectory)) {
        "$env:Path;$BinDirectory"
    }
    else {
        $env:Path
    }
}

$binDirectory = Join-Path $InstallRoot 'bin'
$currentDirectory = Join-Path $InstallRoot 'current'
$downloadUrl = Get-DownloadUrl -RepoName $Repo -RequestedVersion $Version
$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("mdpdf-install-" + [guid]::NewGuid().ToString('N'))
$archivePath = Join-Path $tempRoot 'mdpdf-win-x64.zip'
$extractPath = Join-Path $tempRoot 'extract'
$shimPath = Join-Path $binDirectory 'mdpdf.cmd'
$packagedExePath = Join-Path $currentDirectory 'MdPdf.Console.exe'

if ($WhatIfPreference) {
    Write-Host "Would download: $downloadUrl"
    Write-Host "Would install to: $currentDirectory"
    Write-Host "Would create shim: $shimPath"
    Write-Host "Would ensure user PATH contains: $binDirectory"
    Write-Host 'Browser prerequisite: Chrome, Edge, or Chromium must already be installed.'
    return
}

New-Item -ItemType Directory -Path $tempRoot -Force | Out-Null
New-Item -ItemType Directory -Path $binDirectory -Force | Out-Null

try {
    Invoke-WebRequest -Uri $downloadUrl -OutFile $archivePath
    Expand-Archive -Path $archivePath -DestinationPath $extractPath -Force

    if (Test-Path $currentDirectory) {
        Remove-Item -LiteralPath $currentDirectory -Recurse -Force
    }

    Move-Item -LiteralPath $extractPath -Destination $currentDirectory

    @"
@echo off
"$packagedExePath" %*
"@ | Set-Content -LiteralPath $shimPath -Encoding ASCII -NoNewline

    Ensure-UserPathContains -BinDirectory $binDirectory
}
finally {
    if (Test-Path $tempRoot) {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}

Write-Host "MdPdf installed to: $currentDirectory"
Write-Host "Command shim: $shimPath"
Write-Host "PATH update: added $binDirectory to the user PATH."
Write-Host 'Open a new terminal for the PATH change to take effect.'
Write-Host 'Browser prerequisite: Chrome, Edge, or Chromium must already be installed.'
