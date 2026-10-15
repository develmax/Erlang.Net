$ErrorActionPreference = 'Stop'
$env:DOTNET_PROCESSOR_COUNT = '2'
$env:DOTNET_CLI_USE_MSBUILD_SERVER = '0'
$env:MSBUILDDISABLENODEREUSE = '1'
$env:UseSharedCompilation = 'false'
$env:RestoreDisableParallel = 'true'
$formatDirectory = Join-Path (Get-Location) 'artifacts/allocator-public-sys-profile316-composed1'
$candidateDirectory = Join-Path $formatDirectory 'candidate'
$tokenTool = 'artifacts/spawn-protocol-pressure-integrated-isolated5/candidate/tests/PressureTokenGuard/bin/Debug/net10.0/PressureTokenGuard.dll'
if (Test-Path "$formatDirectory/tokens-before.json") { throw 'Existing token proof' }
& dotnet $tokenTool "$formatDirectory/pre-format-token-inputs.json" "$formatDirectory/tokens-before.json"
if ($LASTEXITCODE -ne 0) { throw 'Initial token capture failed' }
$formatIncludes = @(Get-Content "$formatDirectory/pre-format-token-inputs.json" -Raw | ConvertFrom-Json)
& dotnet format whitespace --folder $candidateDirectory --include @formatIncludes --verbosity minimal *> "$formatDirectory/sdk-format.log"
if ($LASTEXITCODE -ne 0) { throw 'SDK formatter failed' }
& dotnet tools/Erlang.Style/bin/Debug/net10.0/Erlang.Style.dll --write $candidateDirectory *> "$formatDirectory/custom-format.log"
if ($LASTEXITCODE -ne 0) { throw 'Layout formatter failed' }
& dotnet $tokenTool "$formatDirectory/pre-format-token-inputs.json" "$formatDirectory/tokens-after.json"
if ($LASTEXITCODE -ne 0) { throw 'Final token capture failed' }
Get-Content "$formatDirectory/sdk-format.log"
Get-Content "$formatDirectory/custom-format.log"

