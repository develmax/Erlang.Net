$ErrorActionPreference='Stop'
$taskRoot=(Get-Location).Path
$proofRoot=Join-Path $taskRoot 'artifacts/root-part318-main1'
$freeze=Join-Path $proofRoot 'source-freeze.json'
$closurePath=Join-Path $proofRoot 'source-resource-closure.json'
if ((Get-FileHash -LiteralPath $freeze -Algorithm SHA256).Hash -cne '0368584A69E20A9A5FCB007CF6F6B2218BF3036FE5B77C2F1EFEE7DFEBC0D854') { throw 'Exact Main318 source freeze required' }
if ((Get-FileHash -LiteralPath $closurePath -Algorithm SHA256).Hash -cne '7B8C0913A6E46324BF8A40DBD8EB86105990AA6DA76B35EF9BA294E7A44D0C03') { throw 'Exact Main318 closure required' }
$closure=Get-Content -LiteralPath $closurePath -Raw | ConvertFrom-Json
if ($closure.Files.Count -ne 2272) { throw 'Expected 2272 source/resource files' }
foreach ($item in $closure.Files) {
    $sourcePath=Join-Path $taskRoot $item.Path
    if ((Get-Item -LiteralPath $sourcePath).Length -ne $item.Bytes -or (Get-FileHash -LiteralPath $sourcePath -Algorithm SHA256).Hash -cne $item.Sha256) { throw "Source changed: $($item.Path)" }
}
$attempt=Join-Path $proofRoot 'full-attempt.json'
if (Test-Path -LiteralPath $attempt) { throw 'Fresh full attempt required' }
@{ SourceFreezeSha256=(Get-FileHash -LiteralPath $freeze -Algorithm SHA256).Hash; SourceFilesVerified=2272; Context='Outside sandbox: unchanged Windows native named-pipe implementation needs access demonstrated by part317 same-DLL probes'; StartedUtc=(Get-Date).ToUniversalTime().ToString('o') } | ConvertTo-Json | Set-Content -LiteralPath $attempt -Encoding utf8NoBOM
$env:DOTNET_PROCESSOR_COUNT='2'
$env:DOTNET_CLI_USE_MSBUILD_SERVER='0'
$env:MSBUILDDISABLENODEREUSE='1'
$env:UseSharedCompilation='false'
$env:RestoreDisableParallel='true'
$started=(Get-Date).ToUniversalTime().ToString('o')
& pwsh -NoProfile -File tools/validate.ps1 *> (Join-Path $proofRoot 'full-validation.log')
$result=$LASTEXITCODE
@{ExitCode=$result; StartedUtc=$started; FinishedUtc=(Get-Date).ToUniversalTime().ToString('o'); Context='Outside sandbox'} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $proofRoot 'full-exit.json') -Encoding utf8NoBOM
Get-Content -LiteralPath (Join-Path $proofRoot 'full-validation.log') -Tail 18
exit $result
