param(
    [Parameter(Mandatory)][string] $Repository,
    [Parameter(Mandatory)][string] $StateDirectory,
    [Parameter(Mandatory)][ValidatePattern('^[0-9a-fA-F]{64}$')][string] $ApprovedPackageSha256
)

$ErrorActionPreference = 'Stop'
$taskRepository = (Resolve-Path -LiteralPath $Repository).Path
$taskState = (Resolve-Path -LiteralPath $StateDirectory).Path
$taskOutput = Join-Path $taskState 'readiness-cli'
if (Test-Path -LiteralPath $taskOutput) { throw 'Fresh actual readiness CLI output required.' }
[IO.Directory]::CreateDirectory($taskOutput) | Out-Null
$taskTool = Join-Path $taskRepository 'tools/Erlang.Tool/bin/Debug/net10.0/Erlang.Tool.dll'
if (-not (Test-Path -LiteralPath $taskTool -PathType Leaf)) { throw 'Already-built published301 tool required; this script never builds.' }
$taskArguments = @($taskTool, 'readiness', (Join-Path $taskRepository 'docs'), (Join-Path $taskRepository 'docs/validation/part-301/tests.json'), $taskOutput)
& dotnet @taskArguments 1> (Join-Path $taskOutput 'command.stdout') 2> (Join-Path $taskOutput 'command.stderr')
$taskExitCode = $LASTEXITCODE
$taskReceipt = [ordered]@{ ExitCode = $taskExitCode; Arguments = $taskArguments; BuildOrTestsRun = $false; NativeFixtureExecuted = $false; OriginalExecuted = $false; RegistryQueryExecuted = $true }
[IO.File]::WriteAllText((Join-Path $taskOutput 'command-exit.json'), ($taskReceipt | ConvertTo-Json -Depth 5) + "`n", [Text.UTF8Encoding]::new($false))
if ($taskExitCode -ne 0) { throw 'Actual readiness CLI failed; raw output retained. No staging.' }
& py -3 (Join-Path $PSScriptRoot 'finalize302.py') readiness-result --repository $taskRepository --state-directory $taskState --approved-package-sha256 $ApprovedPackageSha256 --cli-output $taskOutput
if ($LASTEXITCODE -ne 0) { throw 'Actual readiness output guard failed. No staging.' }
