$ErrorActionPreference='Stop'
$env:DOTNET_PROCESSOR_COUNT='2'
if(Test-Path artifacts/part272-main-packet-controls){throw 'Never overwrite an actual Main control run'}
dotnet build tools/Erlang.DistributionPacketContrasts -m:1 --disable-build-servers -p:UseSharedCompilation=false *> artifacts/part272-main-packet-target.log
if($LASTEXITCODE -ne 0){throw 'Actual Main corrected packet target failed; log preserved'}
py -3 artifacts/part272-main-packet-controls.py Debug *> artifacts/part272-main-packet-controls.log
if($LASTEXITCODE -ne 0){throw 'Actual Main packet CLI controls failed; all raw outputs preserved'}
Get-Content artifacts/part272-main-packet-controls.log
