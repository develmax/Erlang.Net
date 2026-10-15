$ErrorActionPreference = 'Stop'
$env:DOTNET_PROCESSOR_COUNT = '2'
$env:DOTNET_CLI_USE_MSBUILD_SERVER = '0'
$env:MSBUILDDISABLENODEREUSE = '1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$taskBase = $PSScriptRoot
Push-Location $taskBase
try {
 & dotnet build focus/RegressionFocus.csproj -c Debug -m:1 -nr:false -p:UseSharedCompilation=false -p:RestoreDisableParallel=true *> "$taskBase/target-initial.log"
 $taskExit = $LASTEXITCODE
 @{ExitCode=$taskExit;MainEdited=$false;NativeExecuted=$false} | ConvertTo-Json | Set-Content "$taskBase/target-initial-exit.json"
 Get-Content "$taskBase/target-initial.log" -Tail 22
 exit $taskExit
} finally { Pop-Location }
