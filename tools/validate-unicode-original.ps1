param(
    [string]$Admission = 'artifacts/library376-official-compiler/official-admission.json',
    [string]$Output = 'artifacts/unicode-compatibility',
    [string]$RunnerDll,
    [switch]$CaptureOnly,
    [switch]$IncludeUpstreamSuite
)

$ErrorActionPreference = 'Stop'
$repository = Split-Path $PSScriptRoot -Parent
Push-Location $repository
try {
    $profile = Get-Content -LiteralPath $Admission -Raw | ConvertFrom-Json
    if ($profile.ReleaseVersion -cne '29.1.1' -or
        $profile.SourceCommit -cne 'ad05823719d77c8faee87348ea39513d4e2f99c5' -or
        -not $profile.EveryExtractedFileMatchesArchive -or
        (Get-FileHash -LiteralPath $profile.ExecutionManifestPath -Algorithm SHA256).Hash -cne $profile.ExecutionManifestSha256) {
        throw 'Unicode requires the admitted official OTP 29.1.1 provider.'
    }
    $inventory = Get-Content -LiteralPath $profile.ExecutionManifestPath -Raw | ConvertFrom-Json
    $closure = @($inventory | Where-Object {
        $_.Path -eq 'bin/erl.exe' -or
        $_.Path -match '^erts-[^/]+/bin/(erl.exe|beam.smp.dll)$' -or
        $_.Path -match '^lib/(stdlib-8.1|erts-17.1)/(ebin|src)/(unicode|unicode_util|lists|erts_internal)\.(beam|erl)$'
    })
    if ($closure.Count -lt 10) { throw 'Incomplete Unicode oracle closure.' }
    function Test-Closure {
        foreach ($entry in $closure) {
            if ((Get-FileHash -LiteralPath (Join-Path $profile.InstalledOtpRoot $entry.Path) -Algorithm SHA256).Hash -cne $entry.Sha256) {
                throw 'Unicode oracle input changed.'
            }
        }
    }
    Test-Closure
    $directory = [IO.Path]::GetFullPath($Output)
    if (Test-Path -LiteralPath $directory) { throw 'Choose a new output directory.' }
    New-Item -ItemType Directory -Path $directory | Out-Null
    $relative = [IO.Path]::GetRelativePath($repository, $directory).Replace('\', '/')
    if ($relative.Contains('"') -or $relative.Contains("`n")) { throw 'Unsupported output path.' }
    $expression = '{ok,unicode_port_cases}=compile:file("tools/Validation/Erlang.UnicodeCompatibility/unicode_port_cases.erl",[{outdir,"' + $relative + '"}]),' +
        'true=code:add_patha("' + $relative + '"),ok=unicode_port_cases:write("' + $relative + '"),' +
        'ok=unicode_port_cases:write_extra("' + $relative + '"),halt().'
    & (Join-Path $profile.InstalledOtpRoot 'bin/erl.exe') -noshell -eval $expression 2>&1 |
        Tee-Object -FilePath (Join-Path $directory 'oracle.log')
    if ($LASTEXITCODE -ne 0) { throw 'Unicode oracle failed.' }
    if ($IncludeUpstreamSuite) {
        $suite = 'tools/Validation/Erlang.UnicodeCompatibility/Reference/unicode_SUITE.erl'
        if ((Get-FileHash -LiteralPath $suite).Hash -cne 'E64F62B91FDF2B86A412B1C789AA23868219FE5C6677DFE91013614E6DB44CF0') {
            throw 'Pinned Unicode suite source changed.'
        }
        $suiteExpression = '{ok,unicode_SUITE}=compile:file("' + $suite + '",[{outdir,"' + $relative + '"}]),' +
            '{ok,unicode_suite_capture}=compile:file("tools/Validation/Erlang.UnicodeCompatibility/unicode_suite_capture.erl",[{outdir,"' + $relative + '"}]),' +
            'true=code:add_patha("' + $relative + '"),ok=unicode_suite_capture:write("' + $relative + '"),halt().'
        & (Join-Path $profile.InstalledOtpRoot 'bin/erl.exe') -noshell -eval $suiteExpression 2>&1 |
            Tee-Object -FilePath (Join-Path $directory 'upstream-capture.log')
        if ($LASTEXITCODE -ne 0) { throw 'Unicode upstream suite capture failed.' }
    }
    Test-Closure
    $provenance = [ordered]@{
        Baseline = $profile.ReleaseTag
        Commit = $profile.SourceCommit
        ExecutionManifestSha256 = $profile.ExecutionManifestSha256
        Inputs = $closure
        ObserverSourceSha256 = (Get-FileHash tools/Validation/Erlang.UnicodeCompatibility/unicode_port_cases.erl).Hash
        ObserverBeamSha256 = (Get-FileHash (Join-Path $directory 'unicode_port_cases.beam')).Hash
        CorpusSha256 = (Get-FileHash (Join-Path $directory 'original.etf')).Hash
    }
    $provenance | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $directory 'provenance.json') -Encoding utf8
    if ($CaptureOnly) { return }
    if (-not $RunnerDll) {
        dotnet build tools/Validation/Erlang.UnicodeCompatibility -m:1
        if ($LASTEXITCODE -ne 0) { throw 'Unicode validator build failed.' }
        $RunnerDll = 'tools/Validation/Erlang.UnicodeCompatibility/bin/Debug/net10.0/Erlang.UnicodeCompatibility.dll'
    }
    foreach ($backend in @('Managed', 'Beam')) {
        dotnet $RunnerDll $repository $directory $backend 2>&1 | Tee-Object -FilePath (Join-Path $directory "$backend.log")
        if ($LASTEXITCODE -ne 0) { throw "Unicode $backend comparison failed." }
        foreach ($corpus in @('extra.etf', 'upstream-suite-original.etf')) {
            if (Test-Path -LiteralPath (Join-Path $directory $corpus)) {
                dotnet $RunnerDll $repository $directory $backend $corpus 2>&1 | Tee-Object -FilePath (Join-Path $directory "$backend-$corpus.log")
                if ($LASTEXITCODE -ne 0) { throw "Unicode $backend $corpus comparison failed." }
            }
        }
    }
}
finally { Pop-Location }
