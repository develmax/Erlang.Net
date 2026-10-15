param(
    [Parameter(Mandatory)][string] $Repository,
    [Parameter(Mandatory)][string] $StateDirectory,
    [Parameter(Mandatory)][ValidatePattern('^[0-9a-fA-F]{64}$')][string] $ApprovedPackageSha256
)
$ErrorActionPreference = 'Stop'
$taskRepository = (Resolve-Path -LiteralPath $Repository).Path
$taskState = (Resolve-Path -LiteralPath $StateDirectory).Path
$taskLocal = Join-Path $taskState 'local-checks'
$taskValidator = Join-Path $taskRepository 'artifacts/raw-onload-stack-frame304-guard-revision1/validate-local304.py'
& py -3 $taskValidator --repository $taskRepository --expected-current-head 82ef740f951e6076f095c62b4efd62507d74adc5 --output $taskLocal
if ($LASTEXITCODE -ne 0) { throw 'Installed local304 passive checks failed; no stage.' }
& py -3 (Join-Path $PSScriptRoot 'finalize304.py') local-result --repository $taskRepository --state-directory $taskState --approved-package-sha256 $ApprovedPackageSha256 --output $taskLocal
if ($LASTEXITCODE -ne 0) { throw 'Installed local304 result guard failed; no stage.' }
$taskRegistry = Join-Path $taskState 'readiness-cli'
if (Test-Path -LiteralPath $taskRegistry) { throw 'Fresh readiness output required.' }
[IO.Directory]::CreateDirectory($taskRegistry) | Out-Null
$taskTool = Join-Path $taskRepository 'tools/Erlang.Tool/bin/Debug/net10.0/Erlang.Tool.dll'
if (-not (Test-Path -LiteralPath $taskTool -PathType Leaf)) { throw 'Already-built published303 tool required; never build here.' }
$taskArguments = @($taskTool, 'readiness', (Join-Path $taskRepository 'docs'), (Join-Path $taskRepository 'docs/validation/part-303/tests.json'), $taskRegistry)
& dotnet @taskArguments 1> (Join-Path $taskRegistry 'command.stdout') 2> (Join-Path $taskRegistry 'command.stderr')
$taskExit = $LASTEXITCODE
[IO.File]::WriteAllText((Join-Path $taskRegistry 'command-exit.json'), (([ordered]@{ ExitCode = $taskExit; Arguments = $taskArguments; BuildOrRuntimeTestsRun = $false; NativeOrOriginalExecuted = $false; RegistryQueryExecuted = $true } | ConvertTo-Json -Depth 5) + "`n"), [Text.UTF8Encoding]::new($false))
if ($taskExit -ne 0) { throw 'Actual readiness CLI failed; no stage.' }
& py -3 (Join-Path $PSScriptRoot 'finalize304.py') readiness-result --repository $taskRepository --state-directory $taskState --approved-package-sha256 $ApprovedPackageSha256 --output $taskRegistry
if ($LASTEXITCODE -ne 0) { throw 'Actual readiness report differs; no stage.' }
