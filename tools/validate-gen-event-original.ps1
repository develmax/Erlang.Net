param(
    [string]$Admission = 'artifacts/library376-official-compiler/official-admission.json',
    [string]$Output = 'artifacts/gen-event-compatibility',
    [string]$RunnerDll,
    [switch]$CaptureOnly,
    [Nullable[int]]$BacktraceDepth
)

$GenEventContract = @{
    Release = '29.1.1'
    Baseline = 'OTP-29.1.1'
    Commit = 'ad05823719d77c8faee87348ea39513d4e2f99c5'
    StandardLibraryVersion = '8.1'
    EmulatorVersion = '17.1'
    OracleModules = @('gen_event','gen','proc_lib','sys','io_lib','io_lib_format','io_lib_pretty','unicode','unicode_util','code','epp','erl_scan','erl_features','string','init','erl_prim_loader','prim_file','os','filename','gb_sets','gb_trees','file','code_server','io')
    MinimumOracleFiles = 30
    ExportCount = 44
    ObservationCount = 105
}

$ErrorActionPreference = 'Stop'
$repository = Split-Path $PSScriptRoot -Parent
Push-Location $repository
try {
    $profile = Get-Content -LiteralPath $Admission -Raw | ConvertFrom-Json
    if ($profile.ReleaseVersion -cne $GenEventContract.Release -or
        $profile.SourceCommit -cne $GenEventContract.Commit -or
        -not $profile.EveryExtractedFileMatchesArchive -or
        (Get-FileHash -LiteralPath $profile.ExecutionManifestPath).Hash -cne $profile.ExecutionManifestSha256) {
        throw 'gen_event requires the admitted official OTP 29.1.1 provider.'
    }
    $inventory = Get-Content -LiteralPath $profile.ExecutionManifestPath -Raw | ConvertFrom-Json
    $libraryPattern = '^lib/(stdlib-' + [regex]::Escape($GenEventContract.StandardLibraryVersion) + '|kernel-[^/]+|erts-' +
        [regex]::Escape($GenEventContract.EmulatorVersion) + ')/(ebin|src)/(' + ($GenEventContract.OracleModules -join '|') + ')\.(beam|erl)$'
    $closure = @($inventory | Where-Object {
        $_.Path -eq 'bin/erl.exe' -or
        $_.Path -match '^erts-[^/]+/bin/(erl.exe|beam.smp.dll)$' -or
        $_.Path -match $libraryPattern
    })
    if ($closure.Count -lt $GenEventContract.MinimumOracleFiles) { throw 'Incomplete gen_event oracle closure.' }
    function Test-Closure {
        foreach ($entry in $closure) {
            if ((Get-FileHash -LiteralPath (Join-Path $profile.InstalledOtpRoot $entry.Path)).Hash -cne $entry.Sha256) {
                throw 'gen_event oracle input changed.'
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
        'lists:foreach(fun(M)->{ok,M}=compile:file("tools/Validation/Erlang.GenEventCompatibility/Reference/"++atom_to_list(M)++".erl",[{outdir,D}]) end,[gen_event_SUITE,dummy_h,dummy1_h,dummy_via]),' +
        'lists:foreach(fun(M)->{ok,M}=compile:file("tools/Validation/Erlang.GenEventCompatibility/"++atom_to_list(M)++".erl",[{outdir,D}]) end,[gen_event_no_optional,gen_event_port_cases]),' +
        'true=code:add_patha(D),ok=gen_event_port_cases:write(D),' +
        '{ok,{gen_event,[{abstract_code,{raw_abstract_v1,Forms}}]}}=beam_lib:chunks("src/Otp/Original/Foundation/Erlang.Otp.Stdlib.Beam/Otp/gen_event.beam",[abstract_code]),' +
        'Functions=[#{name=>atom_to_binary(N),arity=>A,line=>erl_anno:line(L)}||{function,L,N,A,_}<-Forms],' +
        'Exports=[#{name=>atom_to_binary(N),arity=>A}||{N,A}<-gen_event:module_info(exports),N=/=module_info],' +
        'ok=file:write_file(filename:join(D,"source-inventory.json"),json:encode(#{module=><<"gen_event">>,functions=>Functions,exports=>Exports})),halt().'
    & (Join-Path $profile.InstalledOtpRoot 'bin/erl.exe') -noshell -eval $expression 2>&1 |
        Tee-Object -FilePath (Join-Path $directory 'oracle.log')
    if ($LASTEXITCODE -ne 0) { throw 'gen_event oracle failed.' }
    Test-Closure
    $sourceHashes = [ordered]@{}
    Get-ChildItem tools/Validation/Erlang.GenEventCompatibility -Recurse -Filter '*.erl' | ForEach-Object {
        $sourceHashes[[IO.Path]::GetRelativePath($repository, $_.FullName).Replace('\', '/')] = (Get-FileHash $_.FullName).Hash
    }
    [ordered]@{
        Baseline = $profile.ReleaseTag
        Commit = $profile.SourceCommit
        BacktraceDepth = $BacktraceDepth
        ExecutionManifestSha256 = $profile.ExecutionManifestSha256
        Inputs = $closure
        Sources = $sourceHashes
        ObserverBeamSha256 = (Get-FileHash (Join-Path $directory 'gen_event_port_cases.beam')).Hash
        CorpusSha256 = (Get-FileHash (Join-Path $directory 'original.etf')).Hash
    } | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $directory 'provenance.json') -Encoding utf8
    if ($CaptureOnly) { return }
    if (-not $RunnerDll) {
        dotnet build tools/Validation/Erlang.GenEventCompatibility -m:1
        if ($LASTEXITCODE -ne 0) { throw 'gen_event validator build failed.' }
        $RunnerDll = 'tools/Validation/Erlang.GenEventCompatibility/bin/Debug/net10.0/Erlang.GenEventCompatibility.dll'
    }
    foreach ($backend in @('Managed', 'Beam')) {
        $arguments = @($RunnerDll, $repository, $directory, $backend)
        $suffix = ''
        if ($null -ne $BacktraceDepth) { $arguments += "depth=$BacktraceDepth"; $suffix = "-depth=$BacktraceDepth" }
        dotnet @arguments 2>&1 | Tee-Object (Join-Path $directory "$backend.log")
        if ($LASTEXITCODE -ne 0) { throw "gen_event $backend comparison failed." }
        $report = Get-Content (Join-Path $directory "$backend$suffix-report.json") -Raw | ConvertFrom-Json
        if (-not $report.CompleteCorpus -or $report.Passed -ne $report.Cases -or $report.Failures.Count -ne 0) {
            throw "Incomplete gen_event $backend acceptance."
        }
    }
}
finally { Pop-Location }
