$ErrorActionPreference = 'Stop'
$env:DOTNET_PROCESSOR_COUNT = '2'
$completed = Get-Content artifacts/root-part305-full-exit.json -Raw | ConvertFrom-Json
if ($completed.ExitCode -ne 0) { throw 'A successful actual Main full run is required' }
$fixture = Join-Path (Get-Location) 'artifacts/spawn-protocol-nif-290-38688662194-terminal/artifact-11951535100/nif-fixture-build/fixture.dll'
$hash = (Get-FileHash -LiteralPath $fixture).Hash
if ($hash -ne '235D9E8997B7760E120ECD5CFACA29F01D0936A8FD89EF8B23DDF7D0DB43883F') { throw 'Trusted fixture bytes changed' }
& dotnet tools/Erlang.NifNativeBackend/bin/Debug/net10.0/Erlang.NifNativeBackend.dll $fixture $hash *> artifacts/root-part305-main-native.log
$code = $LASTEXITCODE
@{ExitCode=$code; ActualMainAfterFull=$true; NativeExecuted=$true; FenceConcurrencyControlIncluded=$true; NonDefaultProviderRefusalIncluded=$true} | ConvertTo-Json | Set-Content artifacts/root-part305-main-native-exit.json
Get-Content artifacts/root-part305-main-native.log
if ($code -ne 0) { exit $code }
& py -3 -B artifacts/root-guard305-native.py
exit $LASTEXITCODE
