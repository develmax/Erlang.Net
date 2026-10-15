$ErrorActionPreference='Stop'
$env:DOTNET_PROCESSOR_COUNT='2'
$env:DOTNET_CLI_USE_MSBUILD_SERVER='0'
$env:MSBUILDDISABLENODEREUSE='1'
$base=Join-Path (Get-Location) 'artifacts/root-nif-native299-isolated'
Push-Location "$base/candidate"
try {
 & dotnet build tools/Erlang.NifNativeBackend/Erlang.NifNativeBackend.csproj --disable-build-servers -m:1 -nr:false -p:UseSharedCompilation=false -p:RestoreDisableParallel=true *> "$base/initial-target.log"
 $code=$LASTEXITCODE
 @{ExitCode=$code;Stage='Actual Runtime-linked native harness target';NativeExecuted=$false}|ConvertTo-Json|Set-Content "$base/initial-target-exit.json"
 Get-Content "$base/initial-target.log" -Tail 18
 exit $code
} finally {Pop-Location}
