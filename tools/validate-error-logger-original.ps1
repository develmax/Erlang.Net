param(
    [string]$Admission = 'artifacts/library376-official-compiler/official-admission.json',
    [string]$Output = 'artifacts/error-logger-compatibility',
    [string]$RunnerDll,
    [switch]$CaptureOnly
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
        throw 'error_logger requires the admitted official OTP 29.1.1 provider.'
    }
    $inventory = Get-Content -LiteralPath $profile.ExecutionManifestPath -Raw | ConvertFrom-Json
    $closure = @($inventory | Where-Object {
        $_.Path -eq 'bin/erl.exe' -or $_.Path -match '^erts-[^/]+/bin/(erl.exe|beam.smp.dll)$' -or
        $_.Path -match '^lib/(stdlib-|kernel-|common_test-)[^/]+/(ebin|src)/[^/]+[.](beam|erl)$'
    })
    function Test-Closure {
        foreach ($entry in $closure) {
            if ((Get-FileHash -LiteralPath (Join-Path $profile.InstalledOtpRoot $entry.Path)).Hash -cne $entry.Sha256) {
                throw ('Oracle input changed: ' + $entry.Path)
            }
        }
    }
    Test-Closure
    $directory = [IO.Path]::GetFullPath($Output)
    if (Test-Path -LiteralPath $directory) { throw 'Choose a new output directory.' }
    New-Item -ItemType Directory -Path $directory | Out-Null
    $relative = [IO.Path]::GetRelativePath($repository, $directory).Replace('\','/')
    $expression = 'D="' + $relative + '",' +
        'lists:foreach(fun(M)->{ok,M}=compile:file("tools/Validation/Erlang.ErrorLoggerCompatibility/Reference/"++atom_to_list(M)++".erl",[debug_info,{outdir,D}]) end,[error_logger_SUITE]),' +
        'lists:foreach(fun(M)->{ok,M}=compile:file("tools/Validation/Erlang.ErrorLoggerCompatibility/"++atom_to_list(M)++".erl",[debug_info,{outdir,D}]) end,[error_logger_port_cases,error_logger_suite_cases]),' +
        'true=code:add_patha(D),ok=error_logger_port_cases:write(D),ok=error_logger_suite_cases:write(D),halt().'
    & (Join-Path $profile.InstalledOtpRoot 'bin/erl.exe') -noshell -eval $expression 2>&1 |
        Tee-Object -FilePath (Join-Path $directory 'oracle.log')
    if ($LASTEXITCODE -ne 0) { throw 'error_logger oracle failed.' }
    Test-Closure
    $hashes = [ordered]@{}
    Get-ChildItem tools/Validation/Erlang.ErrorLoggerCompatibility -Recurse -Filter '*.erl' | ForEach-Object {
        $hashes[[IO.Path]::GetRelativePath($repository, $_.FullName).Replace('\','/')] = (Get-FileHash $_.FullName).Hash
    }
    [ordered]@{ Baseline = $profile.ReleaseTag; Commit = $profile.SourceCommit;
        ExecutionManifestSha256 = $profile.ExecutionManifestSha256; Inputs = $closure; Sources = $hashes;
        CorpusSha256 = (Get-FileHash (Join-Path $directory 'original.etf')).Hash;
        UpstreamSha256 = (Get-FileHash (Join-Path $directory 'upstream.etf')).Hash
    } | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $directory 'provenance.json') -Encoding utf8
    if ($CaptureOnly) { return }
    if (-not $RunnerDll) {
        dotnet build tools/Validation/Erlang.ErrorLoggerCompatibility -m:1
        if ($LASTEXITCODE -ne 0) { throw 'error_logger validator build failed.' }
        $RunnerDll = 'tools/Validation/Erlang.ErrorLoggerCompatibility/bin/Debug/net10.0/Erlang.ErrorLoggerCompatibility.dll'
    }
    foreach ($backend in @('Managed','Beam')) {
        foreach ($mode in @('primary','suite')) {
            $arguments = @($RunnerDll,$repository,$directory,$backend)
            if ($mode -eq 'suite') { $arguments += 'suite' }
            dotnet @arguments 2>&1 | Tee-Object (Join-Path $directory "$backend-$mode.log")
            if ($LASTEXITCODE -ne 0) { throw "error_logger $backend $mode failed." }
        }
    }
}
finally { Pop-Location }
