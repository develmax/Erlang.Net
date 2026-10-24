param(
    [Parameter(Mandatory)][string]$ManagedDirectory,
    [string]$Admission = 'artifacts/library376-official-compiler/official-admission.json',
    [string]$Output = 'artifacts/otp-boot-original'
)
$ErrorActionPreference = 'Stop'
$profile = Get-Content -LiteralPath $Admission -Raw | ConvertFrom-Json
if ($profile.SourceCommit -cne 'ad05823719d77c8faee87348ea39513d4e2f99c5' -or -not $profile.EveryExtractedFileMatchesArchive) { throw 'Unadmitted original OTP provider' }
if ((Get-FileHash -LiteralPath $profile.ExecutionManifestPath -Algorithm SHA256).Hash -cne $profile.ExecutionManifestSha256) { throw 'Original provider inventory changed' }
$files = Get-Content -LiteralPath $profile.ExecutionManifestPath -Raw | ConvertFrom-Json
$selected = @($files | Where-Object { $_.Path -eq 'bin/erl.exe' -or $_.Path -match '^erts-[^/]+/bin/(erl.exe|beam.smp.dll)$' -or $_.Path -match '^lib/(erts-17.1/ebin/(atomics|counters|erts_internal)|sasl-4.4/ebin/(misc_supp|systools_lib)|stdlib-8.1/ebin/lists)\.beam$' })
if ($selected.Count -ne 9) { throw 'Original ERTS/SASL execution bindings are incomplete' }
foreach ($file in $selected) {
    if ((Get-FileHash -LiteralPath (Join-Path $profile.InstalledOtpRoot $file.Path) -Algorithm SHA256).Hash -cne $file.Sha256) { throw "Original execution file changed: $($file.Path)" }
}
$fixtureDirectory = (Resolve-Path 'tests/Erlang.Otp.Boot.Library.Tests/Fixtures').Path
$fixture = Get-Content -LiteralPath (Join-Path $fixtureDirectory 'fixture.json') -Raw | ConvertFrom-Json
if ($fixture.SourceCommit -cne $profile.SourceCommit -or $fixture.Outcomes -ne 19) { throw 'Observer provenance differs' }
foreach ($file in $fixture.Files.PSObject.Properties) {
    if ((Get-FileHash -LiteralPath (Join-Path $fixtureDirectory $file.Name) -Algorithm SHA256).Hash -cne $file.Value) { throw "Observer file changed: $($file.Name)" }
}
New-Item -ItemType Directory -Path $Output -Force | Out-Null
$originalPath = [IO.Path]::GetFullPath((Join-Path $Output 'original-values.etf'))
$pathBase64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($originalPath))
$evaluation = 'P=self(),spawn(otp_application463_observer,worker,[P]),receive {finished,R}->ok=file:write_file(base64:decode("' + $pathBase64 + '"),term_to_binary(R)),io:format("~w~n",[R]),halt() after 30000->halt(2) end.'
$start = [Diagnostics.ProcessStartInfo]::new((Join-Path $profile.InstalledOtpRoot 'bin/erl.exe'))
$start.UseShellExecute = $false
$start.RedirectStandardOutput = $true
$start.RedirectStandardError = $true
foreach ($argument in @('+S','1','-noshell','-noinput','-pa',$fixtureDirectory,'-eval',$evaluation)) { $start.ArgumentList.Add($argument) }
$process = [Diagnostics.Process]::Start($start)
try {
    $stdout = $process.StandardOutput.ReadToEndAsync()
    $stderr = $process.StandardError.ReadToEndAsync()
    if (-not $process.WaitForExit(45000)) { $process.Kill($true); $process.WaitForExit(); throw 'Original ERTS/SASL observer timed out' }
    [IO.File]::WriteAllText((Join-Path $Output 'stdout.txt'), $stdout.GetAwaiter().GetResult())
    [IO.File]::WriteAllText((Join-Path $Output 'stderr.txt'), $stderr.GetAwaiter().GetResult())
    if ($process.ExitCode -ne 0) { throw 'Original ERTS/SASL observer failed' }
    $originalHash = (Get-FileHash -LiteralPath $originalPath -Algorithm SHA256).Hash
    $managedHash = (Get-FileHash -LiteralPath (Join-Path $ManagedDirectory 'managed-values.etf') -Algorithm SHA256).Hash
    if ($originalHash -cne $fixture.Files.'original-values.etf' -or $managedHash -cne $originalHash) { throw 'Whole ERTS/SASL outcomes/stacks differ from original OTP' }
    @{ Passed = $true; Outcomes = 19; RawEtfSha256 = $originalHash; SourceCommit = $profile.SourceCommit; ExecutionBindings = $selected; Comparison = 'Whole raw ETF; no projection, frame removal or identity mapping' } | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $Output 'completion.json')
} finally { $process.Dispose() }
