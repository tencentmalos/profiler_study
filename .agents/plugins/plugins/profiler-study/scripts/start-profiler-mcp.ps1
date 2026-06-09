$ErrorActionPreference = 'Stop'

function Get-CodexHome {
    if (![string]::IsNullOrWhiteSpace($env:CODEX_HOME)) {
        return $env:CODEX_HOME
    }
    return Join-Path $env:USERPROFILE '.codex'
}

function Invoke-CommandToStdErr {
    param(
        [string]$FilePath,
        [string[]]$Arguments
    )

    $output = & $FilePath @Arguments 2>&1
    foreach ($line in $output) {
        [Console]::Error.WriteLine($line)
    }
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}

function Convert-SourceToRepoUrl {
    param([string]$Source)

    if ($Source -match '^https://') {
        return $Source
    }
    if ($Source -match '^[^/\\]+/[^/\\]+(@.+)?$') {
        $repo = ($Source -split '@')[0]
        return "https://github.com/$repo.git"
    }
    return $null
}

function Get-MarketplaceSource {
    $configPath = Join-Path (Get-CodexHome) 'config.toml'
    if (!(Test-Path $configPath -PathType Leaf)) {
        return $null
    }

    $content = Get-Content -Path $configPath -Raw
    $match = [regex]::Match($content, "(?s)\[marketplaces\.profiler-study\].*?source\s*=\s*'([^']+)'")
    if ($match.Success) {
        return $match.Groups[1].Value
    }
    return $null
}

function Get-SourceRoot {
    if (![string]::IsNullOrWhiteSpace($env:PROFILER_STUDY_REPO_ROOT) -and (Test-Path $env:PROFILER_STUDY_REPO_ROOT -PathType Container)) {
        return (Resolve-Path $env:PROFILER_STUDY_REPO_ROOT).Path
    }

    $source = Get-MarketplaceSource
    if (![string]::IsNullOrWhiteSpace($source)) {
        $localSource = $source -replace '^\\\\\?\\', ''
        if (Test-Path $localSource -PathType Container) {
            return (Resolve-Path $localSource).Path
        }

        $repoUrl = Convert-SourceToRepoUrl $source
        if (![string]::IsNullOrWhiteSpace($repoUrl)) {
            $installRoot = Join-Path $env:LOCALAPPDATA 'ProfilerStudyMcp'
            $repoRoot = Join-Path $installRoot 'source'
            if (!(Test-Path $repoRoot -PathType Container)) {
                New-Item -ItemType Directory -Force -Path $installRoot | Out-Null
                Invoke-CommandToStdErr 'git' @('clone', $repoUrl, $repoRoot)
            }
            else {
                Invoke-CommandToStdErr 'git' @('-C', $repoRoot, 'pull', '--ff-only')
            }
            return (Resolve-Path $repoRoot).Path
        }
    }

    $repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..\..') -ErrorAction SilentlyContinue
    if ($repoRoot -and (Test-Path (Join-Path $repoRoot 'ProfilerStudy.McpServer\ProfilerStudy.McpServer.csproj') -PathType Leaf)) {
        return $repoRoot.Path
    }

    throw 'Unable to locate ProfilerStudy repository source. Set PROFILER_STUDY_REPO_ROOT or install from a Codex Git marketplace source.'
}

$repoRoot = Get-SourceRoot
$projectPath = Join-Path $repoRoot 'ProfilerStudy.McpServer\ProfilerStudy.McpServer.csproj'
$publishDir = Join-Path $repoRoot 'ProfilerStudy.McpServer\publish\codex'
$serverPath = Join-Path $publishDir 'ProfilerStudy.McpServer.dll'

if (!(Test-Path $serverPath -PathType Leaf)) {
    $buildOutput = & dotnet publish $projectPath -c Release -nologo -v minimal -p:TargetFrameworks=net8.0 -o $publishDir 2>&1
    foreach ($line in $buildOutput) {
        [Console]::Error.WriteLine($line)
    }
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}

if (!(Test-Path $serverPath -PathType Leaf)) {
    throw "ProfilerStudy MCP server publish did not produce $serverPath"
}

& dotnet $serverPath @args
