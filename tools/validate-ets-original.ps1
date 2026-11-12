param(
    [string]$Admission = 'artifacts/library376-official-compiler/official-admission.json',
    [string]$Output = 'artifacts/ets-original',
    [string]$RunnerDll,
    [switch]$CaptureOnly,
    [switch]$IncludeFileSystem,
    [switch]$IncludeTerminal
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
        throw 'ETS requires the admitted official OTP 29.1.1 provider.'
    }
    $inventory = Get-Content -LiteralPath $profile.ExecutionManifestPath -Raw | ConvertFrom-Json
    $closure = @($inventory | Where-Object {
        $_.Path -eq 'bin/erl.exe' -or
        $_.Path -match '^erts-[^/]+/bin/(erl.exe|beam.smp.dll)$' -or
        $_.Path -match '^lib/[^/]+/(ebin|src)/(ets|lists|erts_internal|persistent_term|qlc|gb_trees|gb_sets|epp|erl_anno|erl_parse|erl_lint|erl_internal|erl_scan|eval_bits|erl_features|sets|maps|orddict|ordsets|sofs|erl_eval|ms_transform|dets|dets_server|dets_sup|dets_utils|dets_v9|init|timer|file_sorter|file|raw_file_io|file_server|file_io_server|os|ram_file|raw_file_io_list|raw_file_io_delayed|raw_file_io_compressed|raw_file_io_deflate|raw_file_io_inflate|prim_file|prim_buffer|gen_server|gen|proc_lib|sys|supervisor|disk_log|disk_log_server|disk_log_sup|disk_log_1|logger|logger_config|logger_backend|logger_filters|logger_server|logger_proxy|io_lib|io_lib_format|io_lib_pretty|io|filename|unicode|unicode_util|binary|proplists|erl_posix_msg|string|re|net_kernel)\.(beam|erl)$'
    })
    if ($closure.Count -lt 8) { throw 'Incomplete ETS oracle closure.' }
    function Test-Closure {
        foreach ($entry in $closure) {
            if ((Get-FileHash -LiteralPath (Join-Path $profile.InstalledOtpRoot $entry.Path)).Hash -cne $entry.Sha256) {
                throw 'ETS oracle input changed.'
            }
        }
    }
    Test-Closure
    $directory = [IO.Path]::GetFullPath($Output)
    if (Test-Path -LiteralPath $directory) { throw 'Choose a new output directory.' }
    New-Item -ItemType Directory -Path $directory | Out-Null
    $previousCrashDump = $env:ERL_CRASH_DUMP
    $env:ERL_CRASH_DUMP = Join-Path $directory 'erl_crash.dump'
    $relative = [IO.Path]::GetRelativePath($repository, $directory).Replace('\', '/')
    if ($relative.Contains('"') -or $relative.Contains("`n")) { throw 'Unsupported output path.' }
    $expression = '{ok,ets_port_support}=compile:file("tools/Validation/Erlang.EtsCompatibility/ets_port_support.erl",[{outdir,"' + $relative + '"}]),' +
        '{ok,ets_port_device}=compile:file("tools/Validation/Erlang.EtsCompatibility/ets_port_device.erl",[{outdir,"' + $relative + '"}]),' +
        '{ok,ets_port_cases}=compile:file("tools/Validation/Erlang.EtsCompatibility/ets_port_cases.erl",[{outdir,"' + $relative + '"}]),' +
        'true=code:add_patha("' + $relative + '"),ok=ets_port_cases:write("' + $relative + '"),halt().'
    & (Join-Path $profile.InstalledOtpRoot 'bin/erl.exe') -noshell -eval $expression 2>&1 |
        Tee-Object -FilePath (Join-Path $directory 'oracle.log')
    if ($LASTEXITCODE -ne 0) { throw 'ETS oracle failed.' }
    Test-Closure
    [ordered]@{
        Baseline = $profile.ReleaseTag
        Commit = $profile.SourceCommit
        ExecutionManifestSha256 = $profile.ExecutionManifestSha256
        Inputs = $closure
        ObserverSourceSha256 = (Get-FileHash tools/Validation/Erlang.EtsCompatibility/ets_port_cases.erl).Hash
        ObserverBeamSha256 = (Get-FileHash (Join-Path $directory 'ets_port_cases.beam')).Hash
        DeviceSourceSha256 = (Get-FileHash tools/Validation/Erlang.EtsCompatibility/ets_port_device.erl).Hash
        DeviceBeamSha256 = (Get-FileHash (Join-Path $directory 'ets_port_device.beam')).Hash
        SupportSourceSha256 = (Get-FileHash tools/Validation/Erlang.EtsCompatibility/ets_port_support.erl).Hash
        SupportBeamSha256 = (Get-FileHash (Join-Path $directory 'ets_port_support.beam')).Hash
        OriginalDumpSha256 = (Get-FileHash (Join-Path $directory 'original.dump')).Hash
        CorpusSha256 = (Get-FileHash (Join-Path $directory 'original.etf')).Hash
    } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $directory 'provenance.json') -Encoding utf8
    if ($CaptureOnly) { return }
    if (-not $RunnerDll) {
        dotnet build tools/Validation/Erlang.EtsCompatibility -m:1 --artifacts-path artifacts/ets-compatibility-build
        if ($LASTEXITCODE -ne 0) { throw 'ETS validator build failed.' }
        $RunnerDll = 'artifacts/ets-compatibility-build/bin/Erlang.EtsCompatibility/debug/Erlang.EtsCompatibility.dll'
    }
    foreach ($backend in @('Managed', 'Beam')) {
        dotnet $RunnerDll $repository $directory $backend 2>&1 | Tee-Object -FilePath (Join-Path $directory "$backend.log")
        if ($LASTEXITCODE -ne 0) { throw "ETS $backend comparison failed." }
        if ($IncludeTerminal) {
            dotnet $RunnerDll $repository $directory $backend browse 2>&1 | Tee-Object -FilePath (Join-Path $directory "$backend-browse.log")
            if ($LASTEXITCODE -ne 0) { throw "ETS $backend terminal comparison failed." }
        }
        if ($IncludeFileSystem) {
            dotnet $RunnerDll $repository $directory $backend files 2>&1 | Tee-Object -FilePath (Join-Path $directory "$backend-files.log")
            if ($LASTEXITCODE -ne 0) { throw "ETS $backend filesystem comparison failed." }
        }
    }
    if ($IncludeFileSystem) {
        $verify = 'true=code:add_patha("' + $relative + '"),' +
            'ok=ets_port_cases:verify_dump("' + $relative + '/Managed.dump"),' +
            'ok=ets_port_cases:verify_dump("' + $relative + '/Beam.dump"),' +
            'ok=file:write_file("' + $relative + '/export-otp.etf",term_to_binary(ok)),halt().'
        Test-Closure
        & (Join-Path $profile.InstalledOtpRoot 'bin/erl.exe') -noshell -eval $verify 2>&1 |
            Tee-Object -FilePath (Join-Path $directory 'export-otp.log')
        if ($LASTEXITCODE -ne 0) { throw 'Official OTP refused an exported ETS dump.' }
        Test-Closure
        [ordered]@{
            Baseline = $profile.ReleaseTag
            Commit = $profile.SourceCommit
            ExecutionManifestSha256 = $profile.ExecutionManifestSha256
            ObserverBeamSha256 = (Get-FileHash (Join-Path $directory 'ets_port_cases.beam')).Hash
            ManagedDumpSha256 = (Get-FileHash (Join-Path $directory 'Managed.dump')).Hash
            BeamDumpSha256 = (Get-FileHash (Join-Path $directory 'Beam.dump')).Hash
            OutcomeSha256 = (Get-FileHash (Join-Path $directory 'export-otp.etf')).Hash
            Passed = $true
        } | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $directory 'interoperability.json') -Encoding utf8
    }
}
finally {
    if (Get-Variable previousCrashDump -ErrorAction SilentlyContinue) {
        $env:ERL_CRASH_DUMP = $previousCrashDump
    }
    Pop-Location
}
