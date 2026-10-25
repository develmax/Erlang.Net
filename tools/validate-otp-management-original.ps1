param(
    [Parameter(Mandatory)][string]$ManagedDirectory,
    [string]$Admission = 'artifacts/library376-official-compiler/official-admission.json',
    [string]$Output = 'artifacts/otp-management-original'
)
. "$PSScriptRoot/project-layout.ps1"
$ErrorActionPreference = 'Stop'
$profile = Get-Content -LiteralPath $Admission -Raw | ConvertFrom-Json
if ($profile.SourceCommit -cne 'ad05823719d77c8faee87348ea39513d4e2f99c5' -or -not $profile.EveryExtractedFileMatchesArchive) { throw 'Unadmitted original OTP provider' }
if ((Get-FileHash -LiteralPath $profile.ExecutionManifestPath -Algorithm SHA256).Hash -cne $profile.ExecutionManifestSha256) { throw 'Original provider inventory changed' }
$files = Get-Content -LiteralPath $profile.ExecutionManifestPath -Raw | ConvertFrom-Json
$selected = @($files | Where-Object { $_.Path -eq 'bin/erl.exe' -or $_.Path -match '^erts-[^/]+/bin/(erl.exe|beam.smp.dll)$' -or $_.Path -match '^lib/(diameter-2.7.2/ebin/(diameter_types|diameter_lib)|snmp-5.20.5/ebin/(snmp_pdus|snmp_pdus_basic|snmp_misc)|kernel-11.0.4/ebin/(inet_parse|inet)|stdlib-8.1/ebin/(lists|calendar|io_lib|io_lib_format|string|unicode|unicode_util|proplists))\.beam$' })
if ($selected.Count -ne 18) { throw 'Original Management execution bindings are incomplete' }
foreach ($file in $selected) {
    if ((Get-FileHash -LiteralPath (Join-Path $profile.InstalledOtpRoot $file.Path) -Algorithm SHA256).Hash -cne $file.Sha256) { throw "Original execution file changed: $($file.Path)" }
}
$packagedFiles = 0
foreach ($library in @('Diameter', 'Snmp')) {
    $directory = (Resolve-OriginalLibraryDirectory $library)
    $provenance = Get-Content -LiteralPath "$directory/provenance.json" -Raw | ConvertFrom-Json
    # The runner rebuilds its inventory JSON; ordering/serialization is not release identity.
    # Bind the same immutable archive and verify every packaged file below.
    if ($provenance.SourceCommit -cne $profile.SourceCommit -or $provenance.ArchiveSha256 -cne $profile.ArchiveSha256 -or $provenance.ReleaseTag -cne $profile.ReleaseTag) { throw 'Packaged library provider differs' }
    foreach ($file in $provenance.Files) {
        $relative = "lib/$($provenance.Application)-$($provenance.Version)/$($file.Path)"
        $binding = @($files | Where-Object Path -CEQ $relative)
        if ($binding.Count -ne 1 -or $binding[0].Sha256 -cne $file.Sha256 -or (Get-FileHash -LiteralPath "$directory/Otp/$($file.Path)" -Algorithm SHA256).Hash -cne $file.Sha256) { throw "Packaged original file differs: $relative" }
        $packagedFiles++
    }
}
if ($packagedFiles -ne 393) { throw 'Incomplete Management file inventory' }
$fixtureDirectory = (Resolve-Path 'tests/Erlang.Otp.Management.Library.Tests/Fixtures').Path
$fixture = Get-Content -LiteralPath (Join-Path $fixtureDirectory 'fixture.json') -Raw | ConvertFrom-Json
if ($fixture.SourceCommit -cne $profile.SourceCommit -or $fixture.Outcomes -ne 768) { throw 'Observer provenance differs' }
if ($fixture.Serialization -cne 'term_to_binary/2 with deterministic option; whole term') { throw 'Original observer serialization contract differs' }
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
$evaluation = 'P=self(),spawn(otp_management473_observer,worker,[P]),receive {finished,R}->ok=file:write_file(base64:decode("' + $pathBase64 + '"),term_to_binary(R,[deterministic])),io:format("~w~n",[R]),halt() after 30000->halt(2) end.'
$start = [Diagnostics.ProcessStartInfo]::new((Join-Path $profile.InstalledOtpRoot 'bin/erl.exe'))
$start.UseShellExecute = $false
$start.RedirectStandardOutput = $true
$start.RedirectStandardError = $true
foreach ($argument in @('+S','1','-noshell','-noinput','-pa',$fixtureDirectory,'-eval',$evaluation)) { $start.ArgumentList.Add($argument) }
$process = [Diagnostics.Process]::Start($start)
try {
    $stdout = $process.StandardOutput.ReadToEndAsync()
    $stderr = $process.StandardError.ReadToEndAsync()
    if (-not $process.WaitForExit(45000)) { $process.Kill($true); $process.WaitForExit(); throw 'Original Management observer timed out' }
    [IO.File]::WriteAllText((Join-Path $Output 'stdout.txt'), $stdout.GetAwaiter().GetResult())
    [IO.File]::WriteAllText((Join-Path $Output 'stderr.txt'), $stderr.GetAwaiter().GetResult())
    if ($process.ExitCode -ne 0) { throw 'Original Management observer failed' }
    $originalHash = (Get-FileHash -LiteralPath $originalPath -Algorithm SHA256).Hash
    $managedHash = (Get-FileHash -LiteralPath (Join-Path $ManagedDirectory 'managed-values.etf') -Algorithm SHA256).Hash
    if ($originalHash -cne $fixture.Files.'original-values.etf' -or $managedHash -cne $originalHash) { throw 'Whole Management outcomes/stacks differ from original OTP' }
    @{ Passed = $true; Outcomes = 768; PackagedOriginalFiles = $packagedFiles; RawEtfSha256 = $originalHash; SourceCommit = $profile.SourceCommit; ExecutionBindings = $selected; Serialization = 'term_to_binary/2 deterministic'; Comparison = 'Whole deterministic raw ETF; no projection, frame removal or identity mapping' } | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $Output 'completion.json')
} finally { $process.Dispose() }

# Retain native-dependent reference contracts separately. They are not managed
# compatibility passes and never substitute for the whole comparison above.
if ($fixture.NativeDependentOriginalOnlyOutcomes -ne 18 -or $fixture.NativeDependentManagedAcceptance) { throw 'Native-dependent scope differs' }
$nativeBindings = @($files | Where-Object { $_.Path -in @('lib/stdlib-8.1/ebin/re.beam', 'lib/asn1-5.5.2/ebin/asn1rt_nif.beam', 'lib/asn1-5.5.2/priv/lib/asn1rt_nif.dll') })
if ($nativeBindings.Count -ne 3) { throw 'Native-only original bindings are incomplete' }
foreach ($binding in $nativeBindings) {
    if ((Get-FileHash -LiteralPath (Join-Path $profile.InstalledOtpRoot $binding.Path) -Algorithm SHA256).Hash -cne $binding.Sha256) { throw 'Native-only original binding differs' }
}
$nativePath = [IO.Path]::GetFullPath((Join-Path $Output 'native-required-original.etf'))
$nativeBase64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($nativePath))
$nativeEval = 'P=self(),spawn(otp_management473_observer,native_worker,[P]),receive {finished,R}->18=length(R),ok=file:write_file(base64:decode("' + $nativeBase64 + '"),term_to_binary(R,[deterministic])),halt() after 30000->halt(2) end.'
$nativeStart = [Diagnostics.ProcessStartInfo]::new((Join-Path $profile.InstalledOtpRoot 'bin/erl.exe'))
$nativeStart.UseShellExecute = $false
$nativeStart.RedirectStandardOutput = $true
$nativeStart.RedirectStandardError = $true
foreach ($argument in @('+S','1','-noshell','-noinput','-pa',$fixtureDirectory,'-eval',$nativeEval)) { $nativeStart.ArgumentList.Add($argument) }
$nativeProcess = [Diagnostics.Process]::Start($nativeStart)
try {
    $nativeStdout = $nativeProcess.StandardOutput.ReadToEndAsync()
    $nativeStderr = $nativeProcess.StandardError.ReadToEndAsync()
    if (-not $nativeProcess.WaitForExit(45000)) { $nativeProcess.Kill($true); $nativeProcess.WaitForExit(); throw 'Native-only original observer timed out' }
    [IO.File]::WriteAllText((Join-Path $Output 'native-stdout.txt'), $nativeStdout.GetAwaiter().GetResult())
    [IO.File]::WriteAllText((Join-Path $Output 'native-stderr.txt'), $nativeStderr.GetAwaiter().GetResult())
    if ($nativeProcess.ExitCode -ne 0) { throw 'Native-only original observer failed' }
    $nativeHash = (Get-FileHash -LiteralPath $nativePath -Algorithm SHA256).Hash
    if ($nativeHash -cne $fixture.Files.'native-required-values.etf') { throw 'Native-only original contracts differ' }
    @{ OriginalOnly = $true; Outcomes = 18; ManagedAcceptance = $false; RawEtfSha256 = $nativeHash; ExecutionBindings = $nativeBindings; Required = $fixture.NativeRequired } | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $Output 'native-required.json')
} finally { $nativeProcess.Dispose() }
