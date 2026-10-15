$ErrorActionPreference = 'Stop'
$env:DOTNET_PROCESSOR_COUNT = '2'
$env:DOTNET_CLI_USE_MSBUILD_SERVER = '0'
$env:MSBUILDDISABLENODEREUSE = '1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$taskBase = $PSScriptRoot
Push-Location $taskBase
try {
 & dotnet build focus/RegressionFocus.csproj -c Debug -m:1 -nr:false -p:UseSharedCompilation=false -p:RestoreDisableParallel=true *> "$taskBase/target-red-semantic.log"
 $taskExit = $LASTEXITCODE
 @{ExitCode=$taskExit;MainEdited=$false;NativeExecuted=$false} | ConvertTo-Json | Set-Content "$taskBase/target-red-semantic-exit.json"
 Get-Content "$taskBase/target-red-semantic.log" -Tail 22
 if ($taskExit -ne 0) { exit $taskExit }
 & dotnet "$taskBase/focus/bin/Debug/net10.0/RegressionFocus.dll" "$taskBase/candidate/tests/Erlang.Tests/bin/Debug/net10.0/Erlang.Tests.dll" RawOnLoadExportsHostChecks *> "$taskBase/red-semantic-before-latch.log"
 $taskExit = $LASTEXITCODE
 @{ExitCode=$taskExit;ExpectedSemanticFailure=$true;MainEdited=$false;NativeExecuted=$false} | ConvertTo-Json | Set-Content "$taskBase/red-semantic-before-latch-exit.json"
 Get-Content "$taskBase/red-semantic-before-latch.log"
 exit $taskExit
} finally { Pop-Location }
