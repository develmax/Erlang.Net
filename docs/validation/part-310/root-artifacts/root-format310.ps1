$ErrorActionPreference = 'Stop'
$env:DOTNET_PROCESSOR_COUNT = '2'
$stage = Join-Path (Get-Location) 'artifacts/root-part310-combined-isolated'
$tokenTool = 'artifacts/spawn-protocol-pressure-integrated-isolated5/candidate/tests/PressureTokenGuard/bin/Debug/net10.0/PressureTokenGuard.dll'
if ((Test-Path "$stage/tokens-before.json") -or (Test-Path "$stage/tokens-after.json")) { throw 'Token evidence already exists' }
& dotnet $tokenTool "$stage/pre-format-token-inputs.json" "$stage/tokens-before.json"
if ($LASTEXITCODE -ne 0) { throw 'Token capture failed' }
& dotnet tools/Erlang.Style/bin/Debug/net10.0/Erlang.Style.dll --write "$stage/candidate" *> "$stage/custom-format.log"
if ($LASTEXITCODE -ne 0) { throw 'Source layout failed' }
& dotnet $tokenTool "$stage/pre-format-token-inputs.json" "$stage/tokens-after.json"
if ($LASTEXITCODE -ne 0) { throw 'Formatted token capture failed' }
& py -3 -B artifacts/root-guard310-format.py
if ($LASTEXITCODE -ne 0) { throw 'Token equality guard failed' }
Get-Content "$stage/custom-format.log"
