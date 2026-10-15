$ErrorActionPreference = 'Stop'
$env:DOTNET_PROCESSOR_COUNT = '2'
$env:DOTNET_CLI_USE_MSBUILD_SERVER = '0'
$env:MSBUILDDISABLENODEREUSE = '1'
$env:UseSharedCompilation = 'false'
$env:RestoreDisableParallel = 'true'
if (Test-Path artifacts/root-part312-revision2/full-exit.json) { throw 'Refusing to overwrite the full-run receipt' }
if (!(Test-Path artifacts/root-part312-revision2/source-freeze.json)) { throw 'Final source authority is missing' }
$started = [DateTime]::UtcNow.ToString('O')
$code = 0
try { & ./tools/validate.ps1 *> artifacts/root-part312-revision2/full-validation.log }
catch { $_ | Out-String | Add-Content artifacts/root-part312-revision2/full-validation.log; $code = 1 }
@{ExitCode=$code; StartedUtc=$started; FinishedUtc=[DateTime]::UtcNow.ToString('O'); CpuLimit=2; BuildNodeReuse=$false} | ConvertTo-Json | Set-Content artifacts/root-part312-revision2/full-exit.json
Get-Content artifacts/root-part312-revision2/full-validation.log -Tail 15
exit $code
