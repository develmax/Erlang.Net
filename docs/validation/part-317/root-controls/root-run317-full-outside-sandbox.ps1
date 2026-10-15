$ErrorActionPreference = 'Stop'
$taskRoot = (Get-Location).Path
$proofRoot = Join-Path $taskRoot 'artifacts/root-part317-main1'
$freezePath = Join-Path $proofRoot 'source-freeze.json'
if ((Get-FileHash -LiteralPath $freezePath -Algorithm SHA256).Hash -cne 'EE1516A086A0DE54C8A1CF12656DB45A13C157807349739B07BDEDB500DE1CD8') { throw 'Main317 source authority changed' }
$sourceClosurePath = Join-Path $proofRoot 'source-resource-closure.json'
if ((Get-FileHash -LiteralPath $sourceClosurePath -Algorithm SHA256).Hash -cne 'A39996D476F7B7D19F1CC7D641E3C2916A2AD66CD4E16C75E0222FEFB099FD26') { throw 'Main317 closure authority changed' }
$closure = Get-Content -LiteralPath $sourceClosurePath -Raw | ConvertFrom-Json
foreach ($item in $closure.Files) {
    $sourcePath = Join-Path $taskRoot $item.Path
    if ((Get-Item -LiteralPath $sourcePath).Length -ne $item.Bytes -or (Get-FileHash -LiteralPath $sourcePath -Algorithm SHA256).Hash -cne $item.Sha256) { throw "Source changed: $($item.Path)" }
}
$first = Get-Content -LiteralPath (Join-Path $proofRoot 'tests-first-failed.json') -Raw | ConvertFrom-Json
if ($first.Passed -ne 6651 -or $first.Failed -ne 16) { throw 'First failed report changed' }
foreach ($test in $first.Tests | Where-Object Status -ne 'Passed') {
    if (-not $test.Name.StartsWith('oracle/compiled-module/erlang-port-native-option-') -or -not $test.Error.Contains('got {error,error,eacces}')) { throw 'Unexpected first failure' }
}
$probeRoot = Join-Path $taskRoot 'artifacts/root-part317-pipe-diagnostic'
$restricted = Get-Content -LiteralPath (Join-Path $probeRoot 'default-result.json') -Raw | ConvertFrom-Json
$unrestricted = Get-Content -LiteralPath (Join-Path $probeRoot 'outside-sandbox-result.json') -Raw | ConvertFrom-Json
if ($restricted.RuntimeSha256 -cne $unrestricted.RuntimeSha256 -or @($restricted.Results | Where-Object { $_.Passed -or $_.NativeErrorCode -ne 5 }).Count -ne 0 -or @($unrestricted.Results | Where-Object { -not $_.Passed }).Count -ne 0) { throw 'Named pipe environment diagnosis changed' }
$attemptPath = Join-Path $proofRoot 'full-outside-sandbox-attempt.json'
if (Test-Path -LiteralPath $attemptPath) { throw 'Fresh full attempt required' }
@{ SourceFreezeSha256 = (Get-FileHash -LiteralPath $freezePath -Algorithm SHA256).Hash; FirstReportSha256 = (Get-FileHash -LiteralPath (Join-Path $proofRoot 'tests-first-failed.json') -Algorithm SHA256).Hash; SourceFilesVerified = $closure.Files.Count; SourceChanged = $false; ExpectedResultsChanged = $false; Context = 'Outside sandbox after same-DLL named-pipe AccessDenied reproduction'; StartedUtc = (Get-Date).ToUniversalTime().ToString('o') } | ConvertTo-Json | Set-Content -LiteralPath $attemptPath -Encoding utf8NoBOM
Move-Item -LiteralPath (Join-Path $proofRoot 'full-validation.log') -Destination (Join-Path $proofRoot 'full-validation-first-failed.log')
Move-Item -LiteralPath (Join-Path $proofRoot 'full-exit.json') -Destination (Join-Path $proofRoot 'full-exit-first-failed.json')
$env:DOTNET_PROCESSOR_COUNT='2'
$env:DOTNET_CLI_USE_MSBUILD_SERVER='0'
$env:MSBUILDDISABLENODEREUSE='1'
$env:UseSharedCompilation='false'
$env:RestoreDisableParallel='true'
$started=(Get-Date).ToUniversalTime().ToString('o')
& pwsh -NoProfile -File tools/validate.ps1 *> (Join-Path $proofRoot 'full-validation.log')
$result=$LASTEXITCODE
@{ ExitCode = $result; StartedUtc = $started; FinishedUtc = (Get-Date).ToUniversalTime().ToString('o'); Context = 'Outside sandbox'; SourceChanged = $false } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $proofRoot 'full-exit.json') -Encoding utf8NoBOM
Get-Content -LiteralPath (Join-Path $proofRoot 'full-validation.log') -Tail 18
exit $result
