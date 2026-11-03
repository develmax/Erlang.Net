param(
    [string]$Admission = 'artifacts/library376-official-compiler/official-admission.json',
    [string]$Output = 'artifacts/math-port/oracle-replay'
)

$ErrorActionPreference = 'Stop'
$repository = Split-Path -Parent $PSScriptRoot
Push-Location $repository
try {
    $profile = Get-Content -LiteralPath $Admission -Raw | ConvertFrom-Json
    if ($profile.ReleaseTag -ne 'OTP-29.1.1' -or $profile.SourceCommit -ne 'ad05823719d77c8faee87348ea39513d4e2f99c5') {
        throw 'The math oracle must use the pinned OTP 29.1.1 release'
    }
    if ((Get-FileHash -LiteralPath $profile.ExecutionManifestPath).Hash -ne $profile.ExecutionManifestSha256) {
        throw 'Oracle execution manifest identity changed'
    }
    $manifest = Get-Content -LiteralPath $profile.ExecutionManifestPath -Raw | ConvertFrom-Json
    $targets = @('bin/erl.exe', 'erts-17.1/bin/erl.exe', 'erts-17.1/bin/beam.smp.dll', 'lib/stdlib-8.1/ebin/math.beam')
    $selected = @($manifest | Where-Object Path -In $targets)
    if ($selected.Count -ne $targets.Count) {
        throw 'Incomplete oracle execution identity'
    }
    foreach ($entry in $selected) {
        if ((Get-FileHash -LiteralPath (Join-Path $profile.InstalledOtpRoot $entry.Path)).Hash -ne $entry.Sha256) {
            throw "Oracle execution identity changed: $($entry.Path)"
        }
    }
    New-Item -ItemType Directory -Path $Output -Force | Out-Null
    $outputPath = (Resolve-Path -LiteralPath $Output).Path.Replace('\', '/')
    $escapedPath = $outputPath.Replace('"', '\"')
    $expression = '{module,beam_math_observer392}=code:load_abs("tests/Erlang.Tests/Resources/LibraryDependencies392/beam_math_observer392"), ' +
        'ok=file:write_file("' + $escapedPath + '/original-values.etf",term_to_binary(beam_math_observer392:run())), ' +
        'ok=file:write_file("' + $escapedPath + '/original-stacks.etf",term_to_binary(beam_math_observer392:run_stacks())), halt().'
    & (Join-Path $profile.InstalledOtpRoot 'bin/erl.exe') -noshell -eval $expression
    if ($LASTEXITCODE -ne 0) {
        throw "Original math observer failed with exit code $LASTEXITCODE"
    }
    foreach ($kind in @('values', 'stacks')) {
        $fresh = (Get-FileHash -LiteralPath (Join-Path $Output "original-$kind.etf")).Hash
        $retained = (Get-FileHash -LiteralPath "tests/Erlang.Tests/Resources/LibraryDependencies392/math394-$kind.etf").Hash
        if ($fresh -ne $retained) {
            throw "Fresh original math $kind differ from the retained corpus"
        }
        Write-Output "$kind $fresh MATCH"
    }
}
finally {
    Pop-Location
}
