param(
    [string]$Configuration = 'Release',
    [string]$Version = '0.0.0-local'
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot 'src\MdPdf.Console\MdPdf.Console.csproj'
$artifactsRoot = Join-Path $repoRoot 'artifacts'
$stagingRoot = Join-Path $artifactsRoot 'staging'
$dotnetHome = Join-Path $repoRoot '.dotnet-home'
$appDataRoot = Join-Path $repoRoot '.appdata'

New-Item -ItemType Directory -Force -Path $artifactsRoot, $stagingRoot | Out-Null
New-Item -ItemType Directory -Force -Path $dotnetHome | Out-Null
New-Item -ItemType Directory -Force -Path $appDataRoot | Out-Null

$env:DOTNET_CLI_HOME = $dotnetHome
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_NOLOGO = '1'
$env:APPDATA = $appDataRoot
$env:LOCALAPPDATA = $appDataRoot

$targets = @(
    @{ Rid = 'win-x64'; AssetName = 'mdpdf-win-x64.zip'; ArchiveKind = 'zip' },
    @{ Rid = 'linux-x64'; AssetName = 'mdpdf-linux-x64.tar.gz'; ArchiveKind = 'tar.gz' }
)

foreach ($target in $targets) {
    $rid = $target.Rid
    $stagingDir = Join-Path $stagingRoot $rid
    $publishDir = Join-Path $stagingDir 'publish'
    $assetPath = Join-Path $artifactsRoot $target.AssetName

    if (Test-Path $stagingDir) {
        Remove-Item -Recurse -Force $stagingDir
    }
    if (Test-Path $assetPath) {
        Remove-Item -Force $assetPath
    }

    New-Item -ItemType Directory -Force -Path $publishDir | Out-Null

    & dotnet publish $projectPath `
        -c $Configuration `
        -r $rid `
        --self-contained true `
        --ignore-failed-sources `
        -p:Version=$Version `
        -p:NuGetAudit=false `
        -o $publishDir

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed for $rid"
    }

    if ($target.ArchiveKind -eq 'zip') {
        Compress-Archive -Path (Join-Path $publishDir '*') -DestinationPath $assetPath -Force
    }
    else {
        & tar -czf $assetPath -C $publishDir .
        if ($LASTEXITCODE -ne 0) {
            throw "tar archive creation failed for $rid"
        }
    }
}

foreach ($target in $targets) {
    Write-Host $target.AssetName
}
