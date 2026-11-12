param(
    [string]$Admission = 'artifacts/library376-official-compiler/official-admission.json',
    [string]$Output = 'artifacts/string-compatibility',
    [string]$RunnerDll,
    [switch]$IncludeUpstreamSuite,
    [switch]$DepthProfiles
)

$ErrorActionPreference = 'Stop'
$repository = Split-Path $PSScriptRoot -Parent
Push-Location $repository
try {
    $outputPath = [IO.Path]::GetFullPath($Output)
    if (Test-Path -LiteralPath $outputPath) {
        throw 'Choose a new output directory to preserve previous comparison evidence.'
    }
    $profile = Get-Content -LiteralPath $Admission -Raw | ConvertFrom-Json
    if ($profile.ReleaseVersion -cne '29.1.1' -or
        $profile.SourceCommit -cne 'ad05823719d77c8faee87348ea39513d4e2f99c5' -or
        -not $profile.EveryExtractedFileMatchesArchive) {
        throw 'The string oracle requires the admitted official OTP 29.1.1 provider.'
    }
    if ((Get-FileHash -LiteralPath $profile.ExecutionManifestPath -Algorithm SHA256).Hash -cne $profile.ExecutionManifestSha256) {
        throw 'The oracle execution manifest changed.'
    }
    $inventory = Get-Content -LiteralPath $profile.ExecutionManifestPath -Raw | ConvertFrom-Json
    $selected = @($inventory | Where-Object {
        $_.Path -eq 'bin/erl.exe' -or
        $_.Path -match '^erts-[^/]+/bin/(erl.exe|beam.smp.dll)$' -or
        $_.Path -match '^lib/(stdlib-8.1|erts-17.1)/(ebin|src)/(string|unicode|unicode_util|lists|maps|erts_internal)\.(beam|erl)$'
    })
    if ($selected.Count -lt 12) {
        throw 'The original string execution closure is incomplete.'
    }
    function Test-OriginalClosure {
        foreach ($entry in $selected) {
            $path = Join-Path $profile.InstalledOtpRoot $entry.Path
            if ((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -cne $entry.Sha256) {
                throw "Original execution input changed: $($entry.Path)"
            }
        }
    }
    Test-OriginalClosure
    New-Item -ItemType Directory -Path $outputPath | Out-Null
    $fixture = 'tools/Validation/Erlang.StringCompatibility/string_port_cases.erl'
    $relativeOutput = [IO.Path]::GetRelativePath($repository, $outputPath).Replace('\', '/')
    if ($relativeOutput.Contains('"') -or $relativeOutput.Contains("`n")) {
        throw 'Unsupported output path.'
    }
    $expression = '{ok,string_port_cases}=compile:file("' + $fixture + '",[{outdir,"' + $relativeOutput + '"}]),' +
        'true=code:add_patha("' + $relativeOutput + '"),' +
        'ok=string_port_cases:write("' + $relativeOutput + '"),halt().'
    & (Join-Path $profile.InstalledOtpRoot 'bin/erl.exe') -noshell -eval $expression 2>&1 |
        Tee-Object -FilePath (Join-Path $outputPath 'oracle.log')
    if ($LASTEXITCODE -ne 0) {
        throw 'Original OTP string acquisition failed.'
    }
    if ($IncludeUpstreamSuite) {
        $suite = 'tools/Validation/Erlang.StringCompatibility/Reference/string_SUITE.erl'
        $capture = 'tools/Validation/Erlang.StringCompatibility/string_suite_capture.erl'
        if ((Get-FileHash -LiteralPath $suite -Algorithm SHA256).Hash -cne '001F4E3EA8D228B9EC3ED6D07F104F43C8DD85A7176D90E3E8C8BFF5EF3BAAB1') {
            throw 'Pinned upstream string_SUITE source changed.'
        }
        $suiteExpression = '{ok,string_SUITE}=compile:file("' + $suite + '",[{outdir,"' + $relativeOutput + '"}]),' +
            '{ok,string_suite_capture}=compile:file("' + $capture + '",[{outdir,"' + $relativeOutput + '"}]),' +
            'true=code:add_patha("' + $relativeOutput + '"),' +
            'ok=string_suite_capture:write("' + $relativeOutput + '"),' +
            'D="' + $relativeOutput + '",{ok,A}=file:read_file(filename:join(D,"original.etf")),' +
            '{ok,B}=file:read_file(filename:join(D,"upstream-suite-original.etf")),' +
            'ok=file:write_file(filename:join(D,"original.etf"),term_to_binary(binary_to_term(A)++binary_to_term(B))),halt().'
        & (Join-Path $profile.InstalledOtpRoot 'bin/erl.exe') -noshell -eval $suiteExpression 2>&1 |
            Tee-Object -FilePath (Join-Path $outputPath 'upstream-oracle.log')
        if ($LASTEXITCODE -ne 0) {
            throw 'Pinned upstream string suite capture failed.'
        }
    }
    if ($RunnerDll) {
        $RunnerDll = [IO.Path]::GetFullPath($RunnerDll)
        if (-not (Test-Path -LiteralPath $RunnerDll -PathType Leaf)) {
            throw 'The explicitly selected string runner does not exist.'
        }
    }
    else {
        dotnet build tools/Validation/Erlang.StringCompatibility -m:1 -v:q 2>&1 |
            Tee-Object -FilePath (Join-Path $outputPath 'build.log')
        if ($LASTEXITCODE -ne 0) {
            throw 'String comparison runner build failed.'
        }
        $RunnerDll = [IO.Path]::GetFullPath('tools/Validation/Erlang.StringCompatibility/bin/Debug/net10.0/Erlang.StringCompatibility.dll')
    }
    $failed = $false
    foreach ($backend in @('managed', 'beam')) {
        dotnet $RunnerDll $repository $outputPath $backend 2>&1 |
            Tee-Object -FilePath (Join-Path $outputPath ($backend + '.log'))
        $failed = $failed -or $LASTEXITCODE -ne 0
    }
    if ($DepthProfiles) {
        $depthExpression = '{ok,string_depth_capture}=compile:file("tools/Validation/Erlang.StringCompatibility/string_depth_capture.erl",[{outdir,"' + $relativeOutput + '"}]),' +
            'true=code:add_patha("' + $relativeOutput + '"),ok=string_depth_capture:write("' + $relativeOutput + '"),halt().'
        & (Join-Path $profile.InstalledOtpRoot 'bin/erl.exe') -noshell -eval $depthExpression 2>&1 |
            Tee-Object -FilePath (Join-Path $outputPath 'depth-oracle.log')
        if ($LASTEXITCODE -ne 0) { throw 'Original string backtrace-depth capture failed.' }
        foreach ($depth in @(0,1,2,3,8,32)) {
            $depthOutput = Join-Path $outputPath ('depth-' + $depth)
            foreach ($backend in @('managed', 'beam')) {
                dotnet $RunnerDll $repository $depthOutput $backend $depth 2>&1 |
                    Tee-Object -FilePath (Join-Path $depthOutput ($backend + '.log'))
                $failed = $failed -or $LASTEXITCODE -ne 0
            }
        }
    }
    Test-OriginalClosure
    [ordered]@{
        Baseline = $profile.SourceCommit
        ReleaseVersion = $profile.ReleaseVersion
        FixtureSha256 = (Get-FileHash -LiteralPath $fixture -Algorithm SHA256).Hash
        UpstreamSuiteIncluded = [bool]$IncludeUpstreamSuite
        BacktraceDepthProfilesIncluded = [bool]$DepthProfiles
        UpstreamSuiteSha256 = $(if ($IncludeUpstreamSuite) { (Get-FileHash -LiteralPath $suite -Algorithm SHA256).Hash } else { $null })
        RunnerDll = $RunnerDll
        RunnerSha256 = (Get-FileHash -LiteralPath $RunnerDll -Algorithm SHA256).Hash
        OriginalInputs = $selected
        ProviderUnchangedAfterExecution = $true
    } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $outputPath 'provenance.json') -Encoding utf8
    if ($failed) {
        throw 'A string backend has compatibility failures; inspect both retained reports.'
    }
}
finally {
    Pop-Location
}
