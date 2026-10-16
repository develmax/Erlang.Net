param([Parameter(Mandatory=$true)][ValidatePattern('^[A-Fa-f0-9]{64}$')][string]$MainFreezeSha256,[Parameter(Mandatory=$true)][ValidatePattern('^[A-Fa-f0-9]{64}$')][string]$ClosureSha256)
$ErrorActionPreference='Stop'
$taskRoot=(Get-Location).Path
$proofRoot=Join-Path $taskRoot 'artifacts/root-part327-main1'
$freeze=Join-Path $proofRoot 'source-freeze.json'
$closurePath=Join-Path $proofRoot 'source-resource-closure.json'
if ((Get-FileHash -LiteralPath $freeze -Algorithm SHA256).Hash -cne $MainFreezeSha256.ToUpperInvariant()) { throw 'Exact Main327 source freeze required' }
if ((Get-FileHash -LiteralPath $closurePath -Algorithm SHA256).Hash -cne $ClosureSha256.ToUpperInvariant()) { throw 'Exact Main327 closure required' }
$sourceFreeze=Get-Content -LiteralPath $freeze -Raw | ConvertFrom-Json
$closure=Get-Content -LiteralPath $closurePath -Raw | ConvertFrom-Json
if ($closure.Files.Count -ne 2379 -or $sourceFreeze.SourcePaths -ne $sourceFreeze.Files.Count -or $sourceFreeze.CSharpPaths -ne 17 -or $sourceFreeze.FinalSourceClosureSha256 -cne $ClosureSha256.ToUpperInvariant()) { throw 'Expected declared owned/17/2379 final source closure' }
if ($sourceFreeze.ExpectedFullPassed -ne 6729 -or $sourceFreeze.SourceClosurePaths -ne 2379) { throw 'Expected actual Main envelope6729/2379' }
foreach ($item in $closure.Files) {
    $sourcePath=Join-Path $taskRoot $item.Path
    if ((Get-Item -LiteralPath $sourcePath).Length -ne $item.Bytes -or (Get-FileHash -LiteralPath $sourcePath -Algorithm SHA256).Hash -cne $item.Sha256) { throw "Source changed: $($item.Path)" }
}
$attempt=Join-Path $proofRoot 'full-attempt.json'
foreach ($name in @('full-attempt.json','full-validation.log','full-exit.json','full-guard.json')) { if (Test-Path -LiteralPath (Join-Path $proofRoot $name)) { throw "Fresh full output required: $name" } }
@{ SourceFreezeSha256=$MainFreezeSha256.ToUpperInvariant(); SourceClosureSha256=$ClosureSha256.ToUpperInvariant(); SourceFilesVerified=2379; Context='Outside sandbox: unchanged Windows native named-pipe implementation needs access demonstrated by part317 same-DLL probes'; StartedUtc=(Get-Date).ToUniversalTime().ToString('o') } | ConvertTo-Json | Set-Content -LiteralPath $attempt -Encoding utf8NoBOM
$env:DOTNET_PROCESSOR_COUNT='2'
$env:DOTNET_CLI_USE_MSBUILD_SERVER='0'
$env:MSBUILDDISABLENODEREUSE='1'
$env:UseSharedCompilation='false'
$env:RestoreDisableParallel='true'
$started=(Get-Date).ToUniversalTime().ToString('o')
& pwsh -NoProfile -File tools/validate.ps1 *> (Join-Path $proofRoot 'full-validation.log')
$result=$LASTEXITCODE
@{ ExitCode=$result; StartedUtc=$started; FinishedUtc=(Get-Date).ToUniversalTime().ToString('o'); Context='Outside sandbox' } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $proofRoot 'full-exit.json') -Encoding utf8NoBOM
Get-Content -LiteralPath (Join-Path $proofRoot 'full-validation.log') -Tail 18
exit $result
