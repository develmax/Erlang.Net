param(
    [string]$ManagedDirectory = 'artifacts/library484/permanent',
    [string]$PublicDirectory = 'artifacts/otp-megaco-managed/receive484',
    [string]$Admission = 'artifacts/library376-official-compiler/official-admission.json',
    [string]$Output = 'artifacts/otp-receive-length-original'
)
$ErrorActionPreference = 'Stop'
$profile = Get-Content -LiteralPath $Admission -Raw | ConvertFrom-Json
if ($profile.SourceCommit -cne 'ad05823719d77c8faee87348ea39513d4e2f99c5' -or -not $profile.EveryExtractedFileMatchesArchive) { throw 'Unadmitted original OTP provider' }
if ((Get-FileHash -LiteralPath $profile.ExecutionManifestPath -Algorithm SHA256).Hash -cne $profile.ExecutionManifestSha256) { throw 'Original provider inventory changed' }
$files = Get-Content -LiteralPath $profile.ExecutionManifestPath -Raw | ConvertFrom-Json
$required = @('bin/erl.exe','bin/erlc.exe','erts-17.1/bin/erl.exe','erts-17.1/bin/beam.smp.dll','lib/compiler-10.0.6/ebin/compile.beam','lib/compiler-10.0.6/ebin/beam_validator.beam','lib/compiler-10.0.6/ebin/beam_opcodes.beam','lib/megaco-4.9.2/ebin/megaco_trans_sender.beam','lib/megaco-4.9.2/src/engine/megaco_trans_sender.erl')
$selected = @($files | Where-Object { $_.Path -cin $required })
if ($selected.Count -ne $required.Count) { throw 'Original length execution/source bindings are incomplete' }
function Confirm-OriginalBindings {
    foreach ($file in $selected) {
        if ((Get-FileHash -LiteralPath (Join-Path $profile.InstalledOtpRoot $file.Path) -Algorithm SHA256).Hash -cne $file.Sha256) { throw "Original execution file changed: $($file.Path)" }
    }
}
Confirm-OriginalBindings
$fixtureDirectory = (Resolve-Path 'tests/Erlang.Tests/Resources/ReceiveLength484').Path
$fixture = Get-Content -LiteralPath (Join-Path $fixtureDirectory 'fixture.json') -Raw | ConvertFrom-Json
if ($fixture.SourceCommit -cne $profile.SourceCommit -or $fixture.Outcomes -ne 576 -or $fixture.Serialization -cne 'term_to_binary/2 deterministic; whole term') { throw 'Length observer provenance differs' }
foreach ($file in $fixture.Files.PSObject.Properties) {
    if ((Get-FileHash -LiteralPath (Join-Path $fixtureDirectory $file.Name) -Algorithm SHA256).Hash -cne $file.Value) { throw "Length observer file changed: $($file.Name)" }
}
$megaco = @($selected | Where-Object { $_.Path.EndsWith('/ebin/megaco_trans_sender.beam', [StringComparison]::Ordinal) })
if ($megaco.Count -ne 1 -or $fixture.Files.'megaco_trans_sender.beam' -cne $megaco[0].Sha256) { throw 'Whole megaco image differs from admitted original' }
New-Item -ItemType Directory -Path $Output -Force | Out-Null
$outputPath = [IO.Path]::GetFullPath($Output)
function Invoke-OriginalLengthProcess([string]$Executable, [string[]]$Arguments, [string]$Name) {
    $start = [Diagnostics.ProcessStartInfo]::new($Executable)
    $start.UseShellExecute = $false
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    foreach ($argument in $Arguments) { $start.ArgumentList.Add($argument) }
    $process = [Diagnostics.Process]::Start($start)
    try {
        $stdout = $process.StandardOutput.ReadToEndAsync()
        $stderr = $process.StandardError.ReadToEndAsync()
        if (-not $process.WaitForExit(45000)) { $process.Kill($true); $process.WaitForExit(); throw 'Original length process timed out' }
        $out = $stdout.GetAwaiter().GetResult()
        $err = $stderr.GetAwaiter().GetResult()
        [IO.File]::WriteAllText((Join-Path $outputPath ($Name + '.stdout.txt')), $out)
        [IO.File]::WriteAllText((Join-Path $outputPath ($Name + '.stderr.txt')), $err)
        if ($process.ExitCode -ne 0 -or $err.Length -ne 0) { throw "Original length $Name failed" }
        return $out
    } finally {
        $process.Dispose()
    }
}
# Compile the unchanged observer again with the admitted original compiler.
$null = Invoke-OriginalLengthProcess (Join-Path $profile.InstalledOtpRoot 'bin/erlc.exe') @('+debug_info','+deterministic','-o',$outputPath,(Join-Path $fixtureDirectory 'otp_receive_length484_observer.erl')) 'compiler'
if ((Get-FileHash -LiteralPath (Join-Path $outputPath 'otp_receive_length484_observer.beam') -Algorithm SHA256).Hash -cne $fixture.Files.'otp_receive_length484_observer.beam') { throw 'Fresh original observer compilation differs' }
$originalPath = Join-Path $outputPath 'original-values.etf'
$pathBase64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($originalPath))
$evaluation = 'P=self(),spawn(otp_receive_length484_observer,worker,[P]),receive {finished,R}->ok=file:write_file(base64:decode("' + $pathBase64 + '"),term_to_binary(R,[deterministic])),io:format("~p",[length(R)]),halt() after 30000->halt(2) end.'
$out = Invoke-OriginalLengthProcess (Join-Path $profile.InstalledOtpRoot 'bin/erl.exe') @('+S','1','-noshell','-noinput','-pa',$outputPath,'-eval',$evaluation) 'observer'
if ($out -cne '576') { throw 'Original length outcome count differs' }
$originalHash = (Get-FileHash -LiteralPath $originalPath -Algorithm SHA256).Hash
if ($originalHash -cne $fixture.Files.'original-values.etf') { throw 'Fresh original length outcomes differ from golden' }
foreach ($directory in @($ManagedDirectory,$PublicDirectory)) {
    if ((Get-FileHash -LiteralPath (Join-Path $directory 'managed-values.etf') -Algorithm SHA256).Hash -cne $originalHash) { throw "Whole length selections and queues differ: $directory" }
}
foreach ($module in @('length_host_guard','otp_receive484_priority')) {
    $null = Invoke-OriginalLengthProcess (Join-Path $profile.InstalledOtpRoot 'bin/erlc.exe') @('+debug_info','+deterministic','-o',$outputPath,(Join-Path $fixtureDirectory ($module + '.erl'))) ($module + '-compiler')
    if ((Get-FileHash -LiteralPath (Join-Path $outputPath ($module + '.beam')) -Algorithm SHA256).Hash -cne $fixture.Files.($module + '.beam')) { throw "Fresh original priority compilation differs: $module" }
}
$priorityPath = Join-Path $outputPath 'priority-values.etf'
$priorityBase64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($priorityPath))
$priorityEvaluation = 'otp_receive484_priority:run(base64:decode("' + $priorityBase64 + '")),halt().'
$priorityOut = Invoke-OriginalLengthProcess (Join-Path $profile.InstalledOtpRoot 'bin/erl.exe') @('+S','1','-noshell','-noinput','-pa',$outputPath,'-eval',$priorityEvaluation) 'priority'
if ($fixture.PriorityInputCells -ne 64017 -or $fixture.PriorityObserverParentPriority -cne 'high' -or $priorityOut -cne '64017') { throw 'Original priority traversal profile differs' }
$priorityHash = (Get-FileHash -LiteralPath $priorityPath -Algorithm SHA256).Hash
if ($priorityHash -cne $fixture.Files.'priority-values.etf' -or (Get-FileHash -LiteralPath (Join-Path $ManagedDirectory 'priority-managed-values.etf') -Algorithm SHA256).Hash -cne $priorityHash) { throw 'Whole priority guard result and remaining queue differ' }
Confirm-OriginalBindings
@{ Passed = $true; Outcomes = 576; SourceCommit = $profile.SourceCommit; RawEtfSha256 = $originalHash; PriorityRawEtfSha256 = $priorityHash; PriorityInputCells = 64017; PriorityObserverParentPriority = 'high'; PriorityOriginalRunningTraceAndSuspendedFrameChecked = $true; ExecutionBindings = $selected; FreshOriginalCompilation = $true; Serialization = 'term_to_binary/2 deterministic'; Comparison = 'Whole raw ETF; no projection'; CooperativeHostControlsAreNotOriginalSchedulerEquivalence = $true } | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $outputPath 'completion.json')
Write-Host 'Original receive484: 576 whole outcomes and priority result/full queue match .NET execution'
