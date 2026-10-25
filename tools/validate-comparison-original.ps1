param(
    [string]$ManagedDirectory = 'artifacts/library482/permanent',
    [string]$Admission = 'artifacts/library376-official-compiler/official-admission.json',
    [string]$Output = 'artifacts/otp-comparison-original'
)
$ErrorActionPreference = 'Stop'
$profile = Get-Content -LiteralPath $Admission -Raw | ConvertFrom-Json
if ($profile.SourceCommit -cne 'ad05823719d77c8faee87348ea39513d4e2f99c5' -or -not $profile.EveryExtractedFileMatchesArchive) { throw 'Unadmitted original OTP provider' }
if ((Get-FileHash -LiteralPath $profile.ExecutionManifestPath -Algorithm SHA256).Hash -cne $profile.ExecutionManifestSha256) { throw 'Original provider inventory changed' }
$files = Get-Content -LiteralPath $profile.ExecutionManifestPath -Raw | ConvertFrom-Json
$selected = @($files | Where-Object { $_.Path -in @('bin/erl.exe','erts-17.1/bin/erl.exe','erts-17.1/bin/beam.smp.dll','lib/compiler-10.0.6/ebin/compile.beam','lib/compiler-10.0.6/ebin/erl_bifs.beam','lib/compiler-10.0.6/ebin/beam_validator.beam','lib/compiler-10.0.6/ebin/beam_call_types.beam','lib/compiler-10.0.6/src/erl_bifs.erl','lib/compiler-10.0.6/src/beam_validator.erl','lib/compiler-10.0.6/src/beam_call_types.erl','lib/observer-2.19/ebin/crashdump_viewer.beam','lib/observer-2.19/src/crashdump_viewer.erl','lib/stdlib-8.1/ebin/lists.beam') })
if ($selected.Count -ne 13) { throw 'Original comparison execution/source bindings are incomplete' }
foreach ($file in $selected) {
    if ((Get-FileHash -LiteralPath (Join-Path $profile.InstalledOtpRoot $file.Path) -Algorithm SHA256).Hash -cne $file.Sha256) { throw "Original execution file changed: $($file.Path)" }
}
$fixtureDirectory = (Resolve-Path 'tests/Erlang.Tests/Resources/ComparisonOriginal482').Path
$fixture = Get-Content -LiteralPath (Join-Path $fixtureDirectory 'fixture.json') -Raw | ConvertFrom-Json
if ($fixture.SourceCommit -cne $profile.SourceCommit -or $fixture.ComparisonOutcomes -ne 160 -or $fixture.CrashdumpOutcomes -ne 20) { throw 'Observer provenance differs' }
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
    @{ Name = 'comparison'; Module = 'otp_frame_comparisons482_observer'; Golden = 'comparison-values.etf'; Outcomes = 160 },
    @{ Name = 'crashdump'; Module = 'otp_crashdump482_observer'; Golden = 'crashdump-values.etf'; Outcomes = 20 }
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
        if (-not $process.WaitForExit(45000)) { $process.Kill($true); $process.WaitForExit(); throw 'Original comparison observer timed out' }
        [IO.File]::WriteAllText((Join-Path $caseOutput 'stdout.txt'), $stdout.GetAwaiter().GetResult())
        [IO.File]::WriteAllText((Join-Path $caseOutput 'stderr.txt'), $stderr.GetAwaiter().GetResult())
        if ($process.ExitCode -ne 0) { throw 'Original comparison observer failed' }
        $originalHash = (Get-FileHash -LiteralPath $originalPath -Algorithm SHA256).Hash
        $managedHash = (Get-FileHash -LiteralPath (Join-Path (Join-Path $ManagedDirectory $case.Name) 'managed-values.etf') -Algorithm SHA256).Hash
        if ($originalHash -cne $fixture.Files.($case.Golden) -or $managedHash -cne $originalHash) { throw 'Whole comparison outcomes/effects/stacks differ from original OTP' }
        @{ Passed = $true; Outcomes = $case.Outcomes; RawEtfSha256 = $originalHash; SourceCommit = $profile.SourceCommit; ExecutionBindings = $selected; Serialization = 'term_to_binary/2 deterministic'; Comparison = 'Whole deterministic raw ETF; no projection, frame removal or identity mapping' } | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $caseOutput 'completion.json')
    } catch {
        $failures += ($case.Name + ': ' + $_.Exception.Message)
    } finally {
        if ($null -ne $process) { $process.Dispose() }
    }
}
$negativeOutput = Join-Path $Output 'negative-compiler'
New-Item -ItemType Directory -Path $negativeOutput -Force | Out-Null
$controls = Get-Content -LiteralPath (Join-Path $fixtureDirectory 'negative-controls.json') -Raw | ConvertFrom-Json
if ($controls.Count -ne 5) { throw 'Original negative compiler controls are incomplete' }
$negativeResults = @()
foreach ($control in $controls) {
    $process = $null
    try {
        $start = [Diagnostics.ProcessStartInfo]::new((Join-Path $profile.InstalledOtpRoot 'bin/erlc.exe'))
        $start.UseShellExecute = $false
        $start.RedirectStandardOutput = $true
        $start.RedirectStandardError = $true
        foreach ($argument in @('+deterministic','-o',[IO.Path]::GetFullPath($negativeOutput),(Join-Path $fixtureDirectory $control.Assembly))) { $start.ArgumentList.Add($argument) }
        $process = [Diagnostics.Process]::Start($start)
        $stdout = $process.StandardOutput.ReadToEndAsync()
        $stderr = $process.StandardError.ReadToEndAsync()
        if (-not $process.WaitForExit(45000)) { $process.Kill($true); $process.WaitForExit(); throw 'Original negative compiler control timed out' }
        $outputText = $stdout.GetAwaiter().GetResult()
        $errorText = $stderr.GetAwaiter().GetResult()
        [IO.File]::WriteAllText((Join-Path $negativeOutput ($control.Name + '.stdout.txt')), $outputText)
        [IO.File]::WriteAllText((Join-Path $negativeOutput ($control.Name + '.stderr.txt')), $errorText)
        if ($process.ExitCode -ne 1 -or -not ($outputText + $errorText).Contains($control.ExpectedError)) { throw 'Original compiler did not refuse the intended invalid register/root contract' }
        $negativeResults += @{ Name = $control.Name; Passed = $true; ExitCode = $process.ExitCode; ExpectedError = $control.ExpectedError; InvalidOriginalProgramExecuted = $false }
    } catch {
        $failures += ($control.Name + ': ' + $_.Exception.Message)
    } finally {
        if ($null -ne $process) { $process.Dispose() }
    }
}
@{ Passed = ($negativeResults.Count -eq 5); SourceCommit = $profile.SourceCommit; ExpectedRefusals = 5; Controls = $negativeResults; ExecutionBindings = $selected } | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $negativeOutput 'completion.json')
if ($failures.Count -ne 0) { throw ($failures -join '; ') }
