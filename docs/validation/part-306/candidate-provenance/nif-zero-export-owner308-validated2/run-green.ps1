$ErrorActionPreference = 'Stop'
$env:DOTNET_PROCESSOR_COUNT = '2'
$env:DOTNET_CLI_USE_MSBUILD_SERVER = '0'
$env:MSBUILDDISABLENODEREUSE = '1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$taskBase = $PSScriptRoot
Push-Location $taskBase
try {
 & dotnet build focus/RegressionFocus.csproj -c Debug -m:1 -nr:false -p:UseSharedCompilation=false -p:RestoreDisableParallel=true *> "$taskBase/target-green.log"
 $taskExit = $LASTEXITCODE
 @{ExitCode=$taskExit;MainEdited=$false;NativeExecuted=$false} | ConvertTo-Json | Set-Content "$taskBase/target-green-exit.json"
 Get-Content "$taskBase/target-green.log" -Tail 22
 if ($taskExit -ne 0) { exit $taskExit }
 & dotnet "$taskBase/focus/bin/Debug/net10.0/RegressionFocus.dll" "$taskBase/candidate/tests/Erlang.Tests/bin/Debug/net10.0/Erlang.Tests.dll" NifBeamOwnerHostChecks *> "$taskBase/green-host-after-production.log"
 $taskExit = $LASTEXITCODE
 @{ExitCode=$taskExit;ExpectedSemanticFailure=$false;MainEdited=$false;NativeExecuted=$false} | ConvertTo-Json | Set-Content "$taskBase/green-host-after-production-exit.json"
 Get-Content "$taskBase/green-host-after-production.log"
 exit $taskExit
} finally { Pop-Location }
