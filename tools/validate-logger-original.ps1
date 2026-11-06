param(
    [string]$Admission = 'artifacts/library376-official-compiler/official-admission.json',
    [string]$Output = 'artifacts/logger-compatibility',
    [string]$RunnerDll,
    [switch]$CaptureOnly,
    [Nullable[int]]$BacktraceDepth
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
        throw 'Logger requires the admitted official OTP 29.1.1 provider.'
    }
    $inventory = Get-Content -LiteralPath $profile.ExecutionManifestPath -Raw | ConvertFrom-Json
    $closure = @($inventory | Where-Object {
        $_.Path -eq 'bin/erl.exe' -or
        $_.Path -match '^erts-[^/]+/bin/(erl.exe|beam.smp.dll)$' -or
        $_.Path -match '^lib/(kernel|stdlib)-[^/]+/(src|ebin)/(logger[^/]*|application|io|io_lib|lists|maps|os)\.(erl|beam)$'
    })
    if ($closure.Count -lt 40) { throw 'Incomplete Logger oracle closure.' }
    function Test-Closure {
        foreach ($entry in $closure) {
            if ((Get-FileHash -LiteralPath (Join-Path $profile.InstalledOtpRoot $entry.Path)).Hash -cne $entry.Sha256) {
                throw "Logger oracle input changed: $($entry.Path)"
            }
        }
    }
    Test-Closure
    $directory = [IO.Path]::GetFullPath($Output)
    if (Test-Path -LiteralPath $directory) { throw 'Choose a new output directory.' }
    New-Item -ItemType Directory -Path $directory | Out-Null
    $relative = [IO.Path]::GetRelativePath($repository, $directory).Replace('\', '/')
    if ($relative.Contains('"') -or $relative.Contains("`n")) { throw 'Unsupported output path.' }
    $depthExpression = if ($null -eq $BacktraceDepth) { '' } else { 'erlang:system_flag(backtrace_depth,' + $BacktraceDepth + '),' }
    $expression = 'D="' + $relative + '",' + $depthExpression +
        '{ok,logger_SUITE}=compile:file("tools/Validation/Erlang.LoggerCompatibility/Reference/logger_SUITE.erl",[{outdir,D},debug_info]),' +
        '{ok,logger_port_cases}=compile:file("tools/Validation/Erlang.LoggerCompatibility/logger_port_cases.erl",[{outdir,D},debug_info]),' +
        'true=code:add_patha(D),ok=logger_port_cases:write(D),halt().'
    & (Join-Path $profile.InstalledOtpRoot 'bin/erl.exe') +S 2:2 -noshell -eval $expression 2>&1 |
        Tee-Object -FilePath (Join-Path $directory 'oracle.log')
    if ($LASTEXITCODE -ne 0) { throw 'Logger oracle capture failed.' }
    Test-Closure
    $closure | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $directory 'oracle-inputs.json') -Encoding utf8
    Copy-Item -LiteralPath $Admission -Destination (Join-Path $directory 'official-admission.json')
    $files = [ordered]@{}
    foreach ($name in @('cases.etf','exports.etf','logger_port_cases.beam','logger_SUITE.beam','oracle-inputs.json')) {
        $files[$name] = (Get-FileHash -LiteralPath (Join-Path $directory $name)).Hash
    }
    $sources = [ordered]@{}
    foreach ($name in @('tools/Validation/Erlang.LoggerCompatibility/logger_port_cases.erl','tools/Validation/Erlang.LoggerCompatibility/Reference/logger_SUITE.erl')) {
        $sources[$name] = (Get-FileHash -LiteralPath $name).Hash
    }
    [ordered]@{ Baseline='OTP-29.1.1'; Commit=$profile.SourceCommit; BacktraceDepth=$BacktraceDepth; Files=$files; Sources=$sources } |
        ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $directory 'corpus-manifest.json') -Encoding utf8
    if (-not $CaptureOnly) {
        if (-not $RunnerDll) {
            dotnet build tools/Validation/Erlang.LoggerCompatibility/Erlang.LoggerCompatibility.csproj -m:1
            if ($LASTEXITCODE -ne 0) { throw 'Logger validator build failed.' }
            $RunnerDll = 'tools/Validation/Erlang.LoggerCompatibility/bin/Debug/net10.0/Erlang.LoggerCompatibility.dll'
        }
        foreach ($backend in @('Managed','Beam')) {
            & dotnet $RunnerDll $repository $directory $backend 2>&1 |
                Tee-Object -FilePath (Join-Path $directory ($backend + '.log'))
            if ($LASTEXITCODE -ne 0) { throw "Logger $backend validation failed." }
        }
    }
}
finally {
    Pop-Location
}
