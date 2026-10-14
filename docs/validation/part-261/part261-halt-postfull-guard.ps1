param(
    [string]$MainRoot = 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net',
    [string]$FullLog = 'artifacts/part261-main-validation.log'
)

$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath($MainRoot)
$log = Get-Item -LiteralPath (Join-Path $root $FullLog)
if (-not ([IO.File]::ReadAllText($log.FullName).Contains('Validation passed'))) { throw 'Full pipeline has not reported completion' }
$beforeRetry = Get-Item -LiteralPath (Join-Path $root 'artifacts/part261-initial-failed-tests.json')
$started = $beforeRetry.CreationTimeUtc
$failedBeforeRetry = Get-Content -LiteralPath $beforeRetry.FullName -Raw | ConvertFrom-Json
if ($failedBeforeRetry.Failed -ne 16 -or $beforeRetry.LastWriteTimeUtc -ge $started -or $started -ge $log.LastWriteTimeUtc) { throw 'Before-retry capture marker is invalid' }
$tests = Get-Content -LiteralPath (Join-Path $root 'artifacts/tests.json') -Raw | ConvertFrom-Json
$previous = Get-Content -LiteralPath (Join-Path $root 'docs/validation/part-249/tests.json') -Raw | ConvertFrom-Json
if ($tests.Passed -ne 6470 -or $tests.Failed -ne 0 -or $previous.Passed -ne 6464) { throw 'Actual full test counts differ' }
$oldNames = @($previous.Tests.Name)
$names = @($tests.Tests.Name)
if (($names[0..6463] | ConvertTo-Json -Compress) -ne ($oldNames | ConvertTo-Json -Compress)) { throw 'Old6464 test prefix changed' }
$tail = @('mfa/erlang:halt/0', 'mfa/erlang:halt/1', 'mfa/erlang:halt/2', 'BEAM prepared publication, native metadata and actual generation leases')
if (($names[6464..6467] | ConvertTo-Json -Compress) -ne ($tail | ConvertTo-Json -Compress)) { throw 'New typed MFA/prepared tail changed' }
$oldMfas = @($oldNames | Where-Object { $_.StartsWith('mfa/') })
$mfas = @($names | Where-Object { $_.StartsWith('mfa/') })
if ($oldMfas.Count -ne 484 -or $mfas.Count -ne 487 -or @($mfas | Where-Object { $_.StartsWith('mfa/erlang:') }).Count -ne 329) { throw 'Actual directly tested registry counts differ' }

$contracts = @(Get-Content -LiteralPath (Join-Path $root 'docs/validation/part-249/halt-predeclared28.json') -Raw | ConvertFrom-Json)
if ($contracts.Count -ne 28) { throw 'Expected repaired28 ledger' }
$hostRows = @(Get-Content -LiteralPath (Join-Path $root 'artifacts/halt-subprocess-host-results.json') -Raw | ConvertFrom-Json)
if ($hostRows.Count -ne 56) { throw 'Host report must contain exactly56, excluding typed MFA children' }
foreach ($contract in $contracts) {
    $actual = @($hostRows | Where-Object Name -eq $contract.Name)
    if ($actual.Count -ne 2 -or (@($actual.Backend | Sort-Object) -join ',') -ne '0,1') { throw "Missing both host backends: $($contract.Name)" }
}
$directFiles = @(Get-ChildItem -LiteralPath (Join-Path $root 'artifacts/halt-mfa-cases') -Recurse -Filter 'mfa-result.json' | Where-Object { $_.CreationTimeUtc -ge $started })
if ($directFiles.Count -ne 3) { throw 'Expected exactly3 current-run typed MFA result files' }
$direct = @($directFiles | ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw | ConvertFrom-Json })
if ((@($direct.Name | Sort-Object) -join ',') -ne 'halt-one-status,halt-two-flush-false,halt-zero') { throw 'Typed direct cases changed' }
if (@($direct | Where-Object Backend -ne 1).Count) { throw 'Typed MFA must execute compiled child' }
$rawProof = @()
foreach ($row in @($hostRows) + @($direct)) {
    $contract = @($contracts | Where-Object Name -eq $row.Name)[0]
    $directory = Get-Item -LiteralPath $row.WorkingDirectory
    if ($directory.CreationTimeUtc -lt $started -or $row.TimedOut -or $row.InfrastructureError) { throw "Stale/failed child: $($row.Name)" }
    if (($row.Sources | ConvertTo-Json -Depth 20 -Compress) -ne ($contract.Sources | ConvertTo-Json -Depth 20 -Compress)) { throw 'Actual child source identity changed' }
    if ($row.RawExitCode -ne $contract.ExpectedWindowsExit) { throw "Raw exit mismatch: $($row.Name)" }
    $checkpoint = Get-Content -LiteralPath (Join-Path $directory.FullName 'process-raw-evidence.json') -Raw | ConvertFrom-Json
    if ($checkpoint.RawExitCode -ne $row.RawExitCode -or $checkpoint.TimedOut -or $checkpoint.InfrastructureError -or -not $checkpoint.StandardOutputComplete -or -not $checkpoint.StandardErrorComplete) { throw 'Raw checkpoint differs' }
    foreach ($stream in @('StandardOutput', 'StandardError')) {
        $file = if ($stream -eq 'StandardOutput') { 'process-stdout.raw' } else { 'process-stderr.raw' }
        $bytes = [IO.File]::ReadAllBytes((Join-Path $directory.FullName $file))
        if ([Convert]::ToHexString($bytes) -ne [Convert]::ToHexString([byte[]]@($row.$stream))) { throw 'Raw captured stream bytes differ' }
        $expected = if ($stream -eq 'StandardOutput') { $contract.ExpectedStandardOutput } else { $contract.ExpectedWindowsStandardError }
        if ($null -ne $expected -and [Convert]::ToHexString($bytes) -ne [Convert]::ToHexString([byte[]]@($expected))) { throw 'Declared finite stream bytes differ' }
    }
    $expectedStage = if ($contract.ExpectedObservation -eq 0) { 'halt-request-observed' } else { 'entry-returned' }
    if ($row.Stage -ne $expectedStage -or $row.Evidence.Stage -ne $expectedStage) { throw 'Nonreturn/entry stage differs' }
    if ($contract.ExpectedObservation -ne 0 -and $row.Evidence.EntryExitReason -ne 'normal') { throw 'Expected normal strict error entry completion' }
    if ($contract.ExpectedObservation -eq 0 -and ($row.Evidence.RequestKind -ne $contract.ExpectedRequestKind -or $row.Evidence.LogicalStatus -ne $contract.ExpectedLogicalStatus)) { throw 'Actual typed halt intent differs' }
    $rawProof += @{ Name=$row.Name; Backend=$row.Backend; Directory=$directory.FullName; Exit=$row.RawExitCode; Stage=$row.Stage }
}
$dumps = @()
foreach ($row in @($hostRows | Where-Object Name -eq 'halt-real-clr-dump')) {
    $path = Join-Path $row.WorkingDirectory 'requested-clr.dmp'
    $bytes = [IO.File]::ReadAllBytes($path)
    if ($bytes.Length -le 4 -or [Convert]::ToHexString($bytes[0..3]) -ne '4D444D50' -or $null -eq $row.Evidence.LogicalStack) { throw 'Actual Windows dump/stack proof missing' }
    $dumps += @{ Backend=$row.Backend; Path=$path; Bytes=$bytes.Length; Sha256=(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash; Format='MDMP; not BEAM crash dump' }
}
$packages = @(Get-ChildItem -LiteralPath (Join-Path $root 'artifacts/packages') -Filter '*.nupkg')
if ($packages.Count -ne 2) { throw 'Expected two owned packages' }
$packageProof = @()
foreach ($package in $packages) {
    if ($package.LastWriteTimeUtc -lt $started) { throw 'Stale package' }
    $zip = [IO.Compression.ZipFile]::OpenRead($package.FullName)
    try {
        if (@($zip.Entries | Where-Object FullName -match '(?i)LICENSE\.local').Count) { throw 'Private license packed' }
        foreach ($notice in @('ROSLYN-LICENSE.txt','DOTNET-LICENSE.txt','DOTNET-ThirdPartyNotices.txt','OTP-LICENSE.txt','OTP-PORTS.md')) {
            $entry = $zip.GetEntry("licenses/$notice")
            if ($null -eq $entry) { throw 'Missing source notice' }
            $stream = $entry.Open()
            try { $sha = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($stream)) } finally { $stream.Dispose() }
            if ($sha -ne (Get-FileHash -LiteralPath (Join-Path $root "docs/$notice") -Algorithm SHA256).Hash) { throw 'Notice bytes differ' }
        }
        $entries = @($zip.Entries | Where-Object Name -eq 'Erlang.Runtime.dll')
        if (-not $entries.Count) { throw 'Promoted runtime missing' }
        foreach ($entry in $entries) {
            $stream = $entry.Open()
            try { $sha = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($stream)) } finally { $stream.Dispose() }
            if ($sha -ne (Get-FileHash -LiteralPath (Join-Path $root 'src/Erlang.Runtime/bin/Debug/net10.0/Erlang.Runtime.dll') -Algorithm SHA256).Hash) { throw 'Packaged promoted runtime differs' }
        }
        $packageProof += @{ Name=$package.Name; Sha256=(Get-FileHash -LiteralPath $package.FullName -Algorithm SHA256).Hash; FiveNoticesExact=$true; PrivateLicenseAbsent=$true; RuntimeBytesExact=$true }
    } finally { $zip.Dispose() }
}
@{ Status='PASS'; Passed=6470; Old6464PrefixExact=$true; Old484MfaOrderExact=$true; ActualDefaultMfaTests=487; ActualErlangMfaTests=329; HostChildren=56; TypedMfaChildren=3; TotalSdkChildExecutions=59; StrictContracts=28; RawProof=$rawProof; ActualDumps=$dumps; Packages=$packageProof; NewNativeProofClaimed=$false } | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $root 'artifacts/part261-halt-postfull-guard.json') -Encoding utf8NoBOM
