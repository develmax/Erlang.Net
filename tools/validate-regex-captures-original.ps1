param(
    [string]$ManagedDirectory = 'artifacts/library485/permanent',
    [string]$Admission = 'artifacts/library376-official-compiler/official-admission.json',
    [string]$Output = 'artifacts/otp-regex-captures-original'
)
$ErrorActionPreference = 'Stop'
$profile = Get-Content -LiteralPath $Admission -Raw | ConvertFrom-Json
if ($profile.SourceCommit -cne 'ad05823719d77c8faee87348ea39513d4e2f99c5' -or -not $profile.EveryExtractedFileMatchesArchive) { throw 'Unadmitted original OTP provider' }
if ((Get-FileHash -LiteralPath $profile.ExecutionManifestPath -Algorithm SHA256).Hash -cne $profile.ExecutionManifestSha256) { throw 'Original inventory changed' }
$files = Get-Content -LiteralPath $profile.ExecutionManifestPath -Raw | ConvertFrom-Json
$required = @('bin/erl.exe', 'bin/erlc.exe', 'erts-17.1/bin/erl.exe', 'erts-17.1/bin/beam.smp.dll', 'lib/stdlib-8.1/ebin/re.beam')
$selected = @($files | Where-Object { $_.Path -cin $required })
if ($selected.Count -ne $required.Count) { throw 'Original regex execution bindings are incomplete' }
function Confirm-RegexBindings {
    foreach ($file in $selected) {
        if ((Get-FileHash -LiteralPath (Join-Path $profile.InstalledOtpRoot $file.Path) -Algorithm SHA256).Hash -cne $file.Sha256) { throw "Original file changed: $($file.Path)" }
    }
}
Confirm-RegexBindings
$fixturePath = (Resolve-Path 'tests/Erlang.Tests/Resources/RegexCaptures485').Path
$fixture = Get-Content -LiteralPath (Join-Path $fixturePath 'manifest.json') -Raw | ConvertFrom-Json
if ($fixture.Commit -cne $profile.SourceCommit -or $fixture.Count -ne 176) { throw 'Regex fixture baseline differs' }
foreach ($file in $fixture.Files.PSObject.Properties) {
    if ((Get-FileHash -LiteralPath (Join-Path $fixturePath $file.Name) -Algorithm SHA256).Hash -cne $file.Value) { throw "Regex fixture changed: $($file.Name)" }
}
$originalRe = @($selected | Where-Object { $_.Path.EndsWith('/re.beam', [StringComparison]::Ordinal) })
if ($originalRe.Count -ne 1 -or $fixture.Files.'re.beam' -cne $originalRe[0].Sha256) { throw 'Original re image differs' }
New-Item -ItemType Directory -Path $Output -Force | Out-Null
$outputPath = [IO.Path]::GetFullPath($Output)
function Invoke-RegexOriginal([string]$Executable, [string[]]$Arguments, [string]$Name) {
    $start = [Diagnostics.ProcessStartInfo]::new($Executable)
    $start.UseShellExecute = $false
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    foreach ($argument in $Arguments) { $start.ArgumentList.Add($argument) }
    $process = [Diagnostics.Process]::Start($start)
    try {
        $stdout = $process.StandardOutput.ReadToEndAsync()
        $stderr = $process.StandardError.ReadToEndAsync()
        if (-not $process.WaitForExit(45000)) { $process.Kill($true); $process.WaitForExit(); throw 'Original regex process timed out' }
        $out = $stdout.GetAwaiter().GetResult()
        $err = $stderr.GetAwaiter().GetResult()
        [IO.File]::WriteAllText((Join-Path $outputPath ($Name + '.stdout.txt')), $out)
        [IO.File]::WriteAllText((Join-Path $outputPath ($Name + '.stderr.txt')), $err)
        if ($process.ExitCode -ne 0 -or $err.Length -ne 0) { throw "Original regex $Name failed" }
    } finally {
        $process.Dispose()
    }
}
Invoke-RegexOriginal (Join-Path $profile.InstalledOtpRoot 'bin/erlc.exe') @('+debug_info', '+deterministic', '-o', $outputPath, (Join-Path $fixturePath 'otp_capture485_matrix.erl')) 'compiler'
if ((Get-FileHash -LiteralPath (Join-Path $outputPath 'otp_capture485_matrix.beam') -Algorithm SHA256).Hash -cne $fixture.Files.'otp_capture485_matrix.beam') { throw 'Fresh original observer compilation differs' }
$originalPath = (Join-Path $outputPath 'original-values.etf').Replace('\', '/')
$expression = '176=otp_capture485_matrix:count(), R=[begin spawn(otp_capture485_matrix,worker,[{self(),I}]), receive {finished,I,V}->V after 30000->erlang:error({timeout,I}) end end || I<-lists:seq(1,176)], ok=file:write_file("' + $originalPath + '",term_to_binary(R,[deterministic])),halt().'
Invoke-RegexOriginal (Join-Path $profile.InstalledOtpRoot 'bin/erl.exe') @('+S', '2:2', '-noshell', '-pa', $outputPath, '-eval', $expression) 'observer'
$golden = (Get-FileHash -LiteralPath (Join-Path $fixturePath 'original-values.etf') -Algorithm SHA256).Hash
$managed = (Get-FileHash -LiteralPath (Join-Path $ManagedDirectory 'managed-values.etf') -Algorithm SHA256).Hash
$original = (Get-FileHash -LiteralPath $originalPath -Algorithm SHA256).Hash
Confirm-RegexBindings
$receipt = [ordered]@{ Baseline = $profile.SourceCommit; Count = 176; Golden = $golden; Managed = $managed; Original = $original; WholeRawEqual = ($golden -ceq $managed -and $golden -ceq $original); IncludesFullErrorsStacksAndBackingSizes = $true }
$receipt | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $outputPath 'comparison.json') -Encoding utf8
if (-not $receipt.WholeRawEqual) { throw 'Whole regex outcomes differ; raw files retained' }
$nativeFixtures = [IO.Path]::GetFullPath('tools/Erlang.OtpCatalogValidation/fixtures')
$nativeManifest = Get-Content -LiteralPath (Join-Path $nativeFixtures 'manifest.json') -Raw | ConvertFrom-Json
foreach ($file in $nativeManifest.PSObject.Properties) {
    if ((Get-FileHash -LiteralPath (Join-Path $nativeFixtures $file.Name) -Algorithm SHA256).Hash -cne $file.Value) { throw "Native observer changed: $($file.Name)" }
}
foreach ($row in @(@('native-indexed', 'otp_native485_indexed'), @('map-protocol', 'otp_native485_map_protocol'))) {
    Invoke-RegexOriginal (Join-Path $profile.InstalledOtpRoot 'bin/erlc.exe') @('+debug_info', '+deterministic', '-o', $outputPath, (Join-Path $nativeFixtures ($row[0] + '/' + $row[1] + '.erl'))) $row[1]
    $compiled = (Get-FileHash -LiteralPath (Join-Path $outputPath ($row[1] + '.beam')) -Algorithm SHA256).Hash
    if ($compiled -cne $nativeManifest.($row[0] + '/' + $row[1] + '.beam')) { throw 'Fresh native observer compilation differs' }
}
$nativeOutput = $outputPath.Replace('\', '/')
$expression = 'lists:foreach(fun(I)->spawn(otp_native485_indexed,worker,[{self(),I}]),receive {finished,I,V}->ok=file:write_file("' + $nativeOutput + '/native-"++integer_to_list(I)++".etf",term_to_binary(V,[deterministic])) after 30000->erlang:error({timeout,I}) end end,lists:seq(1,10)), spawn(otp_native485_map_protocol,worker,[self()]),receive {finished,R,K}->ok=file:write_file("' + $nativeOutput + '/map-protocol.etf",term_to_binary(R,[deterministic])),ok=file:write_file("' + $nativeOutput + '/map-keys.etf",term_to_binary(K,[deterministic])) after 30000->erlang:error(protocol_timeout) end,halt().'
Invoke-RegexOriginal (Join-Path $profile.InstalledOtpRoot 'bin/erl.exe') @('+S', '2:2', '-noshell', '-pa', $outputPath, '-eval', $expression) 'native-observer'
foreach ($index in 1..9) {
    $fresh = (Get-FileHash -LiteralPath (Join-Path $outputPath "native-$index.etf") -Algorithm SHA256).Hash
    $expected = (Get-FileHash -LiteralPath (Join-Path $nativeFixtures "native-indexed/original-$index.etf") -Algorithm SHA256).Hash
    if ($fresh -cne $expected) { throw "Native original comparison differs: $index" }
}
if ((Get-FileHash -LiteralPath (Join-Path $outputPath 'map-protocol.etf') -Algorithm SHA256).Hash -cne $nativeManifest.'map-protocol/original-protocol.etf') { throw 'Original own-state map protocol differs' }
Confirm-RegexBindings
Write-Host 'Original regex captures: 176/176 whole values/errors/stacks/backing sizes match'
Write-Host 'Original native bodies: 9/9 portable outcomes and own-state map protocol match; raw key tuples retained separately'
