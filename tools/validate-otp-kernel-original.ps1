param(
    [Parameter(Mandatory)][string]$ManagedValues,
    [string]$Admission = 'artifacts/library376-official-compiler/official-admission.json',
    [string]$Output = 'artifacts/otp-kernel-original'
)
$ErrorActionPreference = 'Stop'
$profile = Get-Content -LiteralPath $Admission -Raw | ConvertFrom-Json
if ($profile.SourceCommit -cne 'ad05823719d77c8faee87348ea39513d4e2f99c5' -or -not $profile.EveryExtractedFileMatchesArchive) { throw 'Unadmitted original OTP provider' }
if ((Get-FileHash -LiteralPath $profile.ExecutionManifestPath -Algorithm SHA256).Hash -cne $profile.ExecutionManifestSha256) { throw 'Original provider inventory changed' }
$files = Get-Content -LiteralPath $profile.ExecutionManifestPath -Raw | ConvertFrom-Json
$selected = @($files | Where-Object { $_.Path -eq 'bin/erl.exe' -or $_.Path -match '^erts-[^/]+/bin/(erl.exe|beam.smp.dll)$' -or $_.Path -eq 'lib/kernel-11.0.4/ebin/inet_parse.beam' })
if ($selected.Count -ne 4) { throw 'Original execution bindings are incomplete' }
foreach ($file in $selected) {
    if ((Get-FileHash -LiteralPath (Join-Path $profile.InstalledOtpRoot $file.Path) -Algorithm SHA256).Hash -cne $file.Sha256) { throw "Original execution file changed: $($file.Path)" }
}
New-Item -ItemType Directory -Path $Output -Force | Out-Null
$evaluation = 'R=[inet_parse:address("127.0.0.1"),inet_parse:ipv4_address("127.1"),inet_parse:ipv4strict_address("127.1"),inet_parse:ipv6_address("::1"),inet_parse:address("invalid"),inet_parse:strict_address(42),inet_parse:ntoa({127,0,0,1}),inet_parse:ntoa({0,0,0,0,0,0,0,1})],io:format("~w~n",[R]),halt().'
$start = [Diagnostics.ProcessStartInfo]::new((Join-Path $profile.InstalledOtpRoot 'bin/erl.exe'))
$start.UseShellExecute = $false
$start.RedirectStandardOutput = $true
$start.RedirectStandardError = $true
foreach ($argument in @('+S','1','-noshell','-noinput','-eval',$evaluation)) { $start.ArgumentList.Add($argument) }
$process = [Diagnostics.Process]::Start($start)
try {
    $stdout = $process.StandardOutput.ReadToEndAsync()
    $stderr = $process.StandardError.ReadToEndAsync()
    if (-not $process.WaitForExit(30000)) { $process.Kill($true); $process.WaitForExit(); throw 'Original kernel observation timed out' }
    $raw = $stdout.GetAwaiter().GetResult()
    [IO.File]::WriteAllText((Join-Path $Output 'stdout.txt'), $raw)
    [IO.File]::WriteAllText((Join-Path $Output 'stderr.txt'), $stderr.GetAwaiter().GetResult())
    if ($process.ExitCode -ne 0) { throw 'Original kernel observation failed' }
    if ($raw.TrimEnd("`r", "`n") -cne [IO.File]::ReadAllText((Resolve-Path -LiteralPath $ManagedValues).Path)) { throw 'Original kernel outcomes differ from the managed runtime' }
    @{ Passed = $true; Outcomes = 8; SourceCommit = $profile.SourceCommit; ExecutionBindings = $selected; Comparison = 'Whole printed term; only trailing transport newline removed' } | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $Output 'completion.json')
} finally { $process.Dispose() }
