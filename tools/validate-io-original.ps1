param(
    [string]$Admission = 'artifacts/library376-official-compiler/official-admission.json',
    [string]$Output = 'artifacts/io-compatibility',
    [string]$RunnerDll,
    [switch]$CaptureOnly,
    [ValidateRange(0, 65535)][int]$BacktraceDepth = 8
)
$ErrorActionPreference = 'Stop'
$repository = Split-Path $PSScriptRoot -Parent
Push-Location $repository
try {
    $profile = Get-Content -LiteralPath $Admission -Raw | ConvertFrom-Json
    if ($profile.ReleaseVersion -cne '29.1.1' -or
        $profile.SourceCommit -cne 'ad05823719d77c8faee87348ea39513d4e2f99c5' -or
        -not $profile.EveryExtractedFileMatchesArchive -or
        (Get-FileHash -LiteralPath $profile.ExecutionManifestPath).Hash -cne $profile.ExecutionManifestSha256) {
        throw 'io requires the admitted official OTP 29.1.1 oracle.'
    }
    $inventory = Get-Content -LiteralPath $profile.ExecutionManifestPath -Raw | ConvertFrom-Json
    $closure = @($inventory | Where-Object {
        $_.Path -eq 'bin/erl.exe' -or $_.Path -match '^erts-[^/]+/bin/(erl.exe|beam.smp.dll)$' -or
        $_.Path -match '^lib/(stdlib-8.1|kernel-[^/]+|erts-17.1)/ebin/.*\.beam$'
    })
    if ($closure.Count -lt 100) { throw 'Incomplete I/O oracle closure.' }
    function Test-OracleClosure {
        foreach ($entry in $closure) {
            if ((Get-FileHash -LiteralPath (Join-Path $profile.InstalledOtpRoot $entry.Path)).Hash -cne $entry.Sha256) {
                throw ('I/O oracle input changed: ' + $entry.Path)
            }
        }
    }
    Test-OracleClosure
    $directory = [IO.Path]::GetFullPath($Output)
    if (Test-Path -LiteralPath $directory) { throw 'Choose a new immutable output directory.' }
    New-Item -ItemType Directory -Path $directory | Out-Null
    $relative = [IO.Path]::GetRelativePath($repository, $directory).Replace('\', '/')
    if ($relative.Contains('"') -or $relative.Contains("`n")) { throw 'Unsupported output path.' }
    $source = 'tools/Validation/Erlang.IoCompatibility/io_port_cases.erl'
    $suite = 'tools/Validation/Erlang.IoCompatibility/Reference/io_SUITE.erl'
    if ((Get-FileHash -LiteralPath $suite).Hash -cne '1FF7ABD04A15017D356B2E75A6CE99065D0630F1B1C82C7C7EDFAA29E66F7784') {
        throw 'Pinned upstream io_SUITE source changed.'
    }
    $expression = 'erlang:system_flag(backtrace_depth,' + $BacktraceDepth + '),{ok,io_SUITE}=compile:file("' + $suite + '",[{outdir,"' + $relative + '"}]),' +
        '{ok,io_port_cases}=compile:file("' + $source + '",[{outdir,"' + $relative + '"}]),' +
        'true=code:add_patha("' + $relative + '"),ok=io_port_cases:write("' + $relative + '"),halt().'
    & (Join-Path $profile.InstalledOtpRoot 'bin/erl.exe') -noshell -eval $expression 2>&1 |
        Tee-Object -FilePath (Join-Path $directory 'oracle.log')
    if ($LASTEXITCODE -ne 0) { throw 'I/O oracle acquisition failed.' }
    Test-OracleClosure
    [ordered]@{
        Baseline = $profile.ReleaseTag
        SourceCommit = $profile.SourceCommit
        BacktraceDepth = $BacktraceDepth
        AdmissionSha256 = (Get-FileHash -LiteralPath $Admission).Hash
        ObserverSha256 = (Get-FileHash -LiteralPath $source).Hash
        ObserverBeamSha256 = (Get-FileHash -LiteralPath (Join-Path $directory 'io_port_cases.beam')).Hash
        CorpusSha256 = (Get-FileHash -LiteralPath (Join-Path $directory 'cases.etf')).Hash
        ExportsSha256 = (Get-FileHash -LiteralPath (Join-Path $directory 'exports.etf')).Hash
        FeatureStateSha256 = (Get-FileHash -LiteralPath (Join-Path $directory 'features.etf')).Hash
        SuiteSourceSha256 = (Get-FileHash -LiteralPath $suite).Hash
        SuiteBeamSha256 = (Get-FileHash -LiteralPath (Join-Path $directory 'io_SUITE.beam')).Hash
        Closure = $closure
    } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $directory 'provenance.json')
    if ($CaptureOnly) { return }
    if (-not $RunnerDll) {
        dotnet build tools/Validation/Erlang.IoCompatibility/Erlang.IoCompatibility.csproj -m:1
        if ($LASTEXITCODE -ne 0) { throw 'I/O focused build failed.' }
        $RunnerDll = 'tools/Validation/Erlang.IoCompatibility/bin/Debug/net10.0/Erlang.IoCompatibility.dll'
    }
    foreach ($backend in @('Managed', 'Beam')) {
        dotnet $RunnerDll $repository $directory $backend
        if ($LASTEXITCODE -ne 0) { throw ($backend + ' I/O comparison failed; preserve the report.') }
    }
} finally {
    Pop-Location
}
