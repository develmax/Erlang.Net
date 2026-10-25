param(
    [string]$ManagedDirectory = 'artifacts/library481/permanent',
    [string]$Admission = 'artifacts/library376-official-compiler/official-admission.json',
    [string]$Output = 'artifacts/otp-frame-original'
)
$ErrorActionPreference = 'Stop'
$profile = Get-Content -LiteralPath $Admission -Raw | ConvertFrom-Json
if ($profile.SourceCommit -cne 'ad05823719d77c8faee87348ea39513d4e2f99c5' -or -not $profile.EveryExtractedFileMatchesArchive) { throw 'Unadmitted original OTP provider' }
if ((Get-FileHash -LiteralPath $profile.ExecutionManifestPath -Algorithm SHA256).Hash -cne $profile.ExecutionManifestSha256) { throw 'Original provider inventory changed' }
$files = Get-Content -LiteralPath $profile.ExecutionManifestPath -Raw | ConvertFrom-Json
$selected = @($files | Where-Object { $_.Path -in @('bin/erl.exe','erts-17.1/bin/erl.exe','erts-17.1/bin/beam.smp.dll','lib/ssl-11.7.7/ebin/ssl_handshake.beam','lib/ssl-11.7.7/src/ssl_handshake.erl','lib/ssl-11.7.7/ebin/tls_v1.beam','lib/ssl-11.7.7/src/tls_v1.erl','lib/stdlib-8.1/ebin/lists.beam','lib/parsetools-2.8/ebin/leex.beam','lib/parsetools-2.8/src/leex.erl') })
if ($selected.Count -ne 10) { throw 'Original frame execution/source bindings are incomplete' }
foreach ($file in $selected) {
    if ((Get-FileHash -LiteralPath (Join-Path $profile.InstalledOtpRoot $file.Path) -Algorithm SHA256).Hash -cne $file.Sha256) { throw "Original execution file changed: $($file.Path)" }
}
$fixtureDirectory = (Resolve-Path 'tests/Erlang.Tests/Resources/FrameOriginal481').Path
$fixture = Get-Content -LiteralPath (Join-Path $fixtureDirectory 'fixture.json') -Raw | ConvertFrom-Json
if ($fixture.SourceCommit -cne $profile.SourceCommit -or $fixture.ContextOutcomes -ne 10 -or $fixture.RecordOutcomes -ne 4 -or $fixture.TlsOutcomes -ne 96) { throw 'Observer provenance differs' }
if ($fixture.Serialization -cne 'term_to_binary/2 deterministic; whole term') { throw 'Original observer serialization contract differs' }
if ($fixture.CompilerBindings.Count -ne 3) { throw 'Observer compiler bindings are incomplete' }
foreach ($binding in $fixture.CompilerBindings) {
    $admitted = @($files | Where-Object Path -CEQ $binding.Path)
    if ($admitted.Count -ne 1 -or $admitted[0].Sha256 -cne $binding.Sha256 -or (Get-FileHash -LiteralPath (Join-Path $profile.InstalledOtpRoot $binding.Path) -Algorithm SHA256).Hash -cne $binding.Sha256) { throw 'Observer compiler provider differs' }
}
foreach ($file in $fixture.Files.PSObject.Properties) {
    if ((Get-FileHash -LiteralPath (Join-Path $fixtureDirectory $file.Name) -Algorithm SHA256).Hash -cne $file.Value) { throw "Observer file changed: $($file.Name)" }
}
$failures = @()
$cases = @(
    @{ Name = 'context'; Module = 'otp_context481_observer'; Golden = 'context-values.etf'; Outcomes = 10 },
    @{ Name = 'record'; Module = 'otp_record481_observer'; Golden = 'record-values.etf'; Outcomes = 4 },
    @{ Name = 'tls'; Module = 'otp_tls481_observer'; Golden = 'tls-values.etf'; Outcomes = 96 }
)
foreach ($case in $cases) {
    $caseOutput = Join-Path $Output $case.Name
    New-Item -ItemType Directory -Path $caseOutput -Force | Out-Null
    $originalPath = [IO.Path]::GetFullPath((Join-Path $caseOutput 'original-values.etf'))
    $pathBase64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($originalPath))
    $evaluation = 'P=self(),spawn(' + $case.Module + ',worker,[P]),receive {finished,R}->ok=file:write_file(base64:decode("' + $pathBase64 + '"),term_to_binary(R,[deterministic])),io:format("~w~n",[R]),halt() after 30000->halt(2) end.'
    $process = $null
    try {
        $start = [Diagnostics.ProcessStartInfo]::new((Join-Path $profile.InstalledOtpRoot 'bin/erl.exe'))
        $start.UseShellExecute = $false
        $start.RedirectStandardOutput = $true
        $start.RedirectStandardError = $true
        foreach ($argument in @('+S','1','-noshell','-noinput','-pa',$fixtureDirectory,'-eval',$evaluation)) { $start.ArgumentList.Add($argument) }
        $process = [Diagnostics.Process]::Start($start)
        $stdout = $process.StandardOutput.ReadToEndAsync()
        $stderr = $process.StandardError.ReadToEndAsync()
        if (-not $process.WaitForExit(45000)) { $process.Kill($true); $process.WaitForExit(); throw 'Original frame observer timed out' }
        [IO.File]::WriteAllText((Join-Path $caseOutput 'stdout.txt'), $stdout.GetAwaiter().GetResult())
        [IO.File]::WriteAllText((Join-Path $caseOutput 'stderr.txt'), $stderr.GetAwaiter().GetResult())
        if ($process.ExitCode -ne 0) { throw 'Original frame observer failed' }
        $originalHash = (Get-FileHash -LiteralPath $originalPath -Algorithm SHA256).Hash
        $managedHash = (Get-FileHash -LiteralPath (Join-Path (Join-Path $ManagedDirectory $case.Name) 'managed-values.etf') -Algorithm SHA256).Hash
        if ($originalHash -cne $fixture.Files.($case.Golden) -or $managedHash -cne $originalHash) { throw 'Whole frame outcomes/effects/stacks differ from original OTP' }
        @{ Passed = $true; Outcomes = $case.Outcomes; RawEtfSha256 = $originalHash; SourceCommit = $profile.SourceCommit; ExecutionBindings = $selected; Serialization = 'term_to_binary/2 deterministic'; Comparison = 'Whole deterministic raw ETF; no projection, frame removal or identity mapping' } | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $caseOutput 'completion.json')
    } catch {
        $failures += ($case.Name + ': ' + $_.Exception.Message)
    } finally {
        if ($null -ne $process) { $process.Dispose() }
    }
}
if ($failures.Count -ne 0) { throw ($failures -join '; ') }
