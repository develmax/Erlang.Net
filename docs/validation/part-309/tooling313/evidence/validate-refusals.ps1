$ErrorActionPreference = 'Stop'
$taskBase = $PSScriptRoot
$taskDll = Join-Path $taskBase 'candidate/tools/Erlang.BeamFixtures/bin/Debug/net10.0/BeamFixtureExport.dll'
$taskControls = Join-Path $taskBase 'refusal-controls'
if (Test-Path $taskControls) { throw 'Fresh refusal-controls directory required' }
New-Item -ItemType Directory $taskControls | Out-Null
$taskResults = [Collections.Generic.List[object]]::new()
function Invoke-Refusal([string]$name, [string[]]$arguments, [string]$expected, [string]$output, [bool]$outputExists) {
    $start = [Diagnostics.ProcessStartInfo]::new((Get-Command dotnet).Source)
    $start.UseShellExecute = $false
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $start.WorkingDirectory = Join-Path $taskBase 'candidate'
    $start.ArgumentList.Add($taskDll)
    foreach ($argument in $arguments) { $start.ArgumentList.Add($argument) }
    $process = [Diagnostics.Process]::Start($start)
    $stdout = $process.StandardOutput.ReadToEndAsync()
    $stderr = $process.StandardError.ReadToEndAsync()
    if (-not $process.WaitForExit(15000)) { $process.Kill($true); throw 'Refusal control timed out' }
    $outText = $stdout.GetAwaiter().GetResult()
    $errText = $stderr.GetAwaiter().GetResult()
    [IO.File]::WriteAllText((Join-Path $taskControls ($name + '.stdout.txt')), $outText)
    [IO.File]::WriteAllText((Join-Path $taskControls ($name + '.stderr.txt')), $errText)
    if ($process.ExitCode -ne 1 -or -not $errText.Contains($expected) -or $errText.Contains('OracleCommand.Run')) { throw "Wrong refusal boundary: $name" }
    if ($output -and (Test-Path $output) -ne $outputExists) { throw "Output creation boundary: $name" }
    $taskResults.Add(@{Name=$name;ExitCode=$process.ExitCode;ExpectedDiagnostic=$expected;RefusedBeforeOracleCommand=$true;OutputExistedBeforeAndAfter=$outputExists})
    $process.Dispose()
}
Invoke-Refusal 'usage' @() 'Usage: BeamFixtureExport' '' $false
$missingOutput = Join-Path $taskControls 'missing-output'
Invoke-Refusal 'missing-oracle' @((Join-Path $taskControls 'missing-oracle'), $missingOutput, (Join-Path $taskControls 'missing-evidence.json')) 'Installed OTP compiler/oracle is unavailable' $missingOutput $false
$oracle = Join-Path $taskControls 'precondition-only-oracle-layout'
New-Item -ItemType Directory "$oracle/bin", "$oracle/releases/29" | Out-Null
# Deliberately inert input-layout markers. They must never be launched.
[IO.File]::WriteAllBytes("$oracle/bin/erl.exe", [byte[]]::new(0))
[IO.File]::WriteAllBytes("$oracle/bin/erlc.exe", [byte[]]::new(0))
[IO.File]::WriteAllText("$oracle/releases/29/OTP_VERSION", '29.1.0')
$output = Join-Path $taskControls 'wrong-version-output'
Invoke-Refusal 'wrong-version' @($oracle,$output,(Join-Path $taskControls 'missing-evidence.json')) 'Installed OTP_VERSION must be exactly 29.1.1' $output $false
[IO.File]::WriteAllText("$oracle/releases/29/OTP_VERSION", '29.1.1')
$buildLog = Join-Path $taskControls 'synthetic-not-original-build.log'
[IO.File]::WriteAllText($buildLog, 'Synthetic file-input refusal control. No source build or original execution occurred.')
$evidence = @{SourceCommit='0000000000000000000000000000000000000000';SourceTag='OTP-29.1.1';SourceBuildCompleted=$true;InstalledOtpRoot=$oracle;SourceBuildLog=$buildLog;SourceBuildLogSha256=(Get-FileHash $buildLog).Hash}
$evidencePath = Join-Path $taskControls 'synthetic-precondition-evidence.json'
$evidence | ConvertTo-Json | Set-Content $evidencePath
$output = Join-Path $taskControls 'wrong-pin-output'
Invoke-Refusal 'wrong-pin' @($oracle,$output,$evidencePath) 'Source-build evidence does not bind the pinned source to this installation' $output $false
$evidence.SourceCommit = 'ad05823719d77c8faee87348ea39513d4e2f99c5'
$evidence.SourceBuildLogSha256 = ('0' * 64)
$evidence | ConvertTo-Json | Set-Content $evidencePath
$output = Join-Path $taskControls 'wrong-digest-output'
Invoke-Refusal 'wrong-log-digest' @($oracle,$output,$evidencePath) 'Source-build log digest differs from supplied provenance' $output $false
$evidence.SourceBuildLogSha256 = (Get-FileHash $buildLog).Hash
$evidence | ConvertTo-Json | Set-Content $evidencePath
$output = Join-Path $taskControls 'existing-output'
New-Item -ItemType Directory $output | Out-Null
$sentinel = Join-Path $output 'sentinel.txt'
[IO.File]::WriteAllText($sentinel, 'Preserve prior evidence')
$sentinelHash = (Get-FileHash $sentinel).Hash
Invoke-Refusal 'existing-output' @($oracle,$output,$evidencePath) 'Output directory already exists; prior evidence will not be overwritten' $output $true
if ((Get-FileHash $sentinel).Hash -ne $sentinelHash -or @(Get-ChildItem $output -File -Recurse).Count -ne 1) { throw 'Existing output changed' }
@{Status='PASS_ACTUAL_MANAGED_TOOL_REFUSALS_WITH_SYNTHETIC_INPUT_LAYOUTS';Expected=6;Passed=$taskResults.Count;Cases=$taskResults;OriginalExecutablesInvoked=$false;InertExecutableMarkersAreNotOriginalCompiler=$true;OriginalCompilationPerformed=$false;OriginalBodyExecutionPerformed=$false;NativeLoadingExecutionPerformed=$false;NoBeamFabricated=$true;SentinelPreserved=$true} | ConvertTo-Json -Depth 7 | Set-Content (Join-Path $taskBase 'refusal-controls-report.json')
Get-Content (Join-Path $taskBase 'refusal-controls-report.json') -TotalCount 8