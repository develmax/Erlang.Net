$ErrorActionPreference='Stop'
$env:DOTNET_PROCESSOR_COUNT='2'
$env:DOTNET_CLI_USE_MSBUILD_SERVER='0'
$env:MSBUILDDISABLENODEREUSE='1'
$env:UseSharedCompilation='false'
$env:RestoreDisableParallel='true'
$part316Root=(Get-Location).Path
$part316Evidence=Join-Path $part316Root 'artifacts/allocator-public-sys-profile316-composed1'
$part316Candidate=Join-Path $part316Evidence 'candidate'
$part316Focus=Join-Path $part316Root 'artifacts/source-purge-stage315-focus/bin/Debug/net10.0/Focus.dll'
if (Test-Path "$part316Evidence/focus-exit.json") { throw 'Existing attempt' }
if ((Get-FileHash "$part316Evidence/formatted-core-source-freeze.json").Hash -cne 'EDF2CD42E162933F73EA2AA1588494F3754F2DB1AF529FD707C792E48149D6B5') { throw 'Source mismatch' }
$part316Code=0
try {
 Push-Location $part316Candidate
 try {
  & dotnet build tests/Erlang.Tests --disable-build-servers -m:1 -nr:false -p:UseSharedCompilation=false -p:RestoreDisableParallel=true --nologo *> "$part316Evidence/target-build.log"
  if ($LASTEXITCODE -ne 0) { throw 'Target build failed' }
  & dotnet $part316Focus tests/Erlang.Tests/bin/Debug/net10.0/Erlang.Tests.dll "$part316Evidence/focus-results.json" SystemAllocatorPublicHostChecks NativePayloadProviderHostChecks AllocatorQueryMailboxHostChecks MailboxSelectionChecks *> "$part316Evidence/focus.log"
  if ($LASTEXITCODE -ne 0) { throw 'HOST focus failed' }
 } finally { Pop-Location }
} catch { $_ | Out-String | Add-Content "$part316Evidence/focus.log"; $part316Code=1 }
@{ExitCode=$part316Code;NativeOrOriginalExecuted=$false;MainEdited=$false;SourceFreezeSha256='EDF2CD42E162933F73EA2AA1588494F3754F2DB1AF529FD707C792E48149D6B5'} | ConvertTo-Json | Set-Content "$part316Evidence/focus-exit.json"
Get-Content "$part316Evidence/target-build.log" -Tail 9
if (Test-Path "$part316Evidence/focus.log") { Get-Content "$part316Evidence/focus.log" -Tail 12 }
exit $part316Code
