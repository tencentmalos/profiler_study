$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..')
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
