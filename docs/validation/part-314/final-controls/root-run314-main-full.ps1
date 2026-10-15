$ErrorActionPreference = 'Stop'
$env:DOTNET_PROCESSOR_COUNT = '2'
$env:DOTNET_CLI_USE_MSBUILD_SERVER = '0'
$env:MSBUILDDISABLENODEREUSE = '1'
$env:UseSharedCompilation = 'false'
$env:RestoreDisableParallel = 'true'
$partDirectory = 'artifacts/root-part314-main1'
if (Test-Path "$partDirectory/full-exit.json") { throw 'Existing full-run receipt' }
if (!(Test-Path "$partDirectory/source-freeze.json")) { throw 'Final source authority required' }
$partStarted = [DateTime]::UtcNow.ToString('O')
$partCode = 0
try { & ./tools/validate.ps1 *> "$partDirectory/full-validation.log" }
catch { $_ | Out-String | Add-Content "$partDirectory/full-validation.log"; $partCode = 1 }
@{ExitCode=$partCode; StartedUtc=$partStarted; FinishedUtc=[DateTime]::UtcNow.ToString('O'); CpuLimit=2; BuildNodeReuse=$false} | ConvertTo-Json | Set-Content "$partDirectory/full-exit.json"
Get-Content "$partDirectory/full-validation.log" -Tail 16
exit $partCode
