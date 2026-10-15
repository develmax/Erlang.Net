$ErrorActionPreference = 'Stop'
$env:DOTNET_PROCESSOR_COUNT = '2'
$env:DOTNET_CLI_USE_MSBUILD_SERVER = '0'
$env:MSBUILDDISABLENODEREUSE = '1'
$env:UseSharedCompilation = 'false'
$env:RestoreDisableParallel = 'true'
$started = [DateTime]::UtcNow.ToString('O')
$code = 0
try {
    & ./tools/validate.ps1 *> artifacts/root-part297-full-validation.log
} catch {
    $_ | Out-String | Add-Content artifacts/root-part297-full-validation.log
    $code = 1
}
@{ExitCode=$code; StartedUtc=$started; FinishedUtc=[DateTime]::UtcNow.ToString('O'); CpuLimit=2; BuildNodeReuse=$false} | ConvertTo-Json | Set-Content artifacts/root-part297-full-exit.json
Get-Content artifacts/root-part297-full-validation.log -Tail 15
exit $code

