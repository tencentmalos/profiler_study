$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$projectPath = Join-Path $repoRoot 'ProfilerStudy.McpServer\ProfilerStudy.McpServer.csproj'
$serverPath = Join-Path $repoRoot 'ProfilerStudy.McpServer\bin\Release\net8.0\ProfilerStudy.McpServer.dll'

if (!(Test-Path $serverPath -PathType Leaf)) {
    $buildOutput = & dotnet build $projectPath -c Release -nologo -v minimal -p:TargetFrameworks=net8.0 2>&1
    foreach ($line in $buildOutput) {
        [Console]::Error.WriteLine($line)
    }
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}

if (!(Test-Path $serverPath -PathType Leaf)) {
    throw "ProfilerStudy MCP server build did not produce $serverPath"
}

& dotnet $serverPath @args
