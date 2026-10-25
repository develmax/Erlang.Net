param(
    [string]$ManagedDirectory = 'artifacts/library477/permanent',
    [string]$Admission = 'artifacts/library376-official-compiler/official-admission.json',
    [string]$Output = 'artifacts/otp-apply-original'
)
$ErrorActionPreference = 'Stop'
$profile = Get-Content -LiteralPath $Admission -Raw | ConvertFrom-Json
if ($profile.SourceCommit -cne 'ad05823719d77c8faee87348ea39513d4e2f99c5' -or -not $profile.EveryExtractedFileMatchesArchive) { throw 'Unadmitted original OTP provider' }
if ((Get-FileHash -LiteralPath $profile.ExecutionManifestPath -Algorithm SHA256).Hash -cne $profile.ExecutionManifestSha256) { throw 'Original provider inventory changed' }
$files = Get-Content -LiteralPath $profile.ExecutionManifestPath -Raw | ConvertFrom-Json
$selected = @($files | Where-Object { $_.Path -in @('bin/erl.exe','erts-17.1/bin/erl.exe','erts-17.1/bin/beam.smp.dll','lib/odbc-2.17.1/ebin/odbc.beam','lib/stdlib-8.1/ebin/lists.beam','lib/erts-17.1/ebin/erlang.beam','lib/erts-17.1/src/erlang.erl') })
if ($selected.Count -ne 7) { throw 'Original apply execution bindings are incomplete' }
foreach ($file in $selected) {
    if ((Get-FileHash -LiteralPath (Join-Path $profile.InstalledOtpRoot $file.Path) -Algorithm SHA256).Hash -cne $file.Sha256) { throw "Original execution file changed: $($file.Path)" }
}
$fixtureDirectory = (Resolve-Path 'tests/Erlang.Tests/Resources/ApplyOriginal477').Path
$fixture = Get-Content -LiteralPath (Join-Path $fixtureDirectory 'fixture.json') -Raw | ConvertFrom-Json
if ($fixture.SourceCommit -cne $profile.SourceCommit -or $fixture.Outcomes -ne 115) { throw 'Observer provenance differs' }
if ($fixture.Serialization -cne 'term_to_binary/2 deterministic; whole term') { throw 'Original observer serialization contract differs' }
$applySource = Join-Path $profile.InstalledOtpRoot 'lib/erts-17.1/src/erlang.erl'
if ((Get-FileHash -LiteralPath $applySource -Algorithm SHA256).Hash -cne $fixture.ApplyFunctionSource.Sha256 -or $fixture.ApplyFunctionSource.Line -ne 10558 -or [IO.File]::ReadAllLines($applySource)[10557] -cne '    erlang:apply(Fun, Args).') { throw 'Pinned apply/2 continuation source differs' }
foreach ($include in $fixture.CompilerIncludes) {
    if ((Get-FileHash -LiteralPath $include.Path -Algorithm SHA256).Hash -cne $include.Sha256) { throw 'Original observer include differs' }
}
if ($fixture.CompilerBindings.Count -ne 3) { throw 'Observer compiler bindings are incomplete' }
foreach ($binding in $fixture.CompilerBindings) {
    $admitted = @($files | Where-Object Path -CEQ $binding.Path)
    if ($admitted.Count -ne 1 -or $admitted[0].Sha256 -cne $binding.Sha256 -or (Get-FileHash -LiteralPath (Join-Path $profile.InstalledOtpRoot $binding.Path) -Algorithm SHA256).Hash -cne $binding.Sha256) { throw 'Observer compiler provider differs' }
}
foreach ($file in $fixture.Files.PSObject.Properties) {
    if ((Get-FileHash -LiteralPath (Join-Path $fixtureDirectory $file.Name) -Algorithm SHA256).Hash -cne $file.Value) { throw "Observer file changed: $($file.Name)" }
}
New-Item -ItemType Directory -Path $Output -Force | Out-Null
$originalPath = [IO.Path]::GetFullPath((Join-Path $Output 'original-values.etf'))
$pathBase64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($originalPath))
$evaluation = 'P=self(),spawn(otp_apply477_observer,worker,[P]),receive {finished,R}->ok=file:write_file(base64:decode("' + $pathBase64 + '"),term_to_binary(R,[deterministic])),io:format("~w~n",[R]),halt() after 30000->halt(2) end.'
$start = [Diagnostics.ProcessStartInfo]::new((Join-Path $profile.InstalledOtpRoot 'bin/erl.exe'))
$start.UseShellExecute = $false
$start.RedirectStandardOutput = $true
$start.RedirectStandardError = $true
foreach ($argument in @('+S','1','-noshell','-noinput','-pa',$fixtureDirectory,'-eval',$evaluation)) { $start.ArgumentList.Add($argument) }
$process = [Diagnostics.Process]::Start($start)
try {
    $stdout = $process.StandardOutput.ReadToEndAsync()
    $stderr = $process.StandardError.ReadToEndAsync()
    if (-not $process.WaitForExit(45000)) { $process.Kill($true); $process.WaitForExit(); throw 'Original dynamic apply observer timed out' }
    [IO.File]::WriteAllText((Join-Path $Output 'stdout.txt'), $stdout.GetAwaiter().GetResult())
    [IO.File]::WriteAllText((Join-Path $Output 'stderr.txt'), $stderr.GetAwaiter().GetResult())
    if ($process.ExitCode -ne 0) { throw 'Original dynamic apply observer failed' }
    $originalHash = (Get-FileHash -LiteralPath $originalPath -Algorithm SHA256).Hash
    $managedHash = (Get-FileHash -LiteralPath (Join-Path $ManagedDirectory 'managed-values.etf') -Algorithm SHA256).Hash
    if ($originalHash -cne $fixture.Files.'original-values.etf' -or $managedHash -cne $originalHash) { throw 'Whole dynamic apply outcomes/stacks differ from original OTP' }

    @{ Passed = $true; Outcomes = 115; RawEtfSha256 = $originalHash; SourceCommit = $profile.SourceCommit; ExecutionBindings = $selected; Serialization = 'term_to_binary/2 deterministic'; Comparison = 'Whole deterministic raw ETF; no projection, frame removal or identity mapping' } | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $Output 'completion.json')
} finally { $process.Dispose() }
