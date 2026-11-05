param(
    [string]$Admission = 'artifacts/library376-official-compiler/official-admission.json',
    [string]$Output = 'artifacts/sys/oracle-001',
    [ValidateRange(0,128)][int]$BacktraceDepth = 8
)
$ErrorActionPreference = 'Stop'
$repository = Split-Path $PSScriptRoot -Parent
Push-Location $repository
try {
    $profile = Get-Content -LiteralPath $Admission -Raw | ConvertFrom-Json
    if ($profile.ReleaseVersion -cne '29.1.1' -or $profile.SourceCommit -cne 'ad05823719d77c8faee87348ea39513d4e2f99c5' -or
        -not $profile.EveryExtractedFileMatchesArchive -or
        (Get-FileHash -LiteralPath $profile.ExecutionManifestPath).Hash -cne $profile.ExecutionManifestSha256) {
        throw 'sys requires the admitted official OTP 29.1.1 provider'
    }
    $inventory = Get-Content -LiteralPath $profile.ExecutionManifestPath -Raw | ConvertFrom-Json
    $closure = @($inventory | Where-Object { $_.Path -eq 'bin/erl.exe' -or $_.Path -match '^erts-[^/]+/bin/(erl.exe|beam.smp.dll)$' -or $_.Path -match '^lib/(stdlib-[^/]+|kernel-[^/]+|erts-[^/]+)/(ebin|src)/' })
    if ($closure.Count -lt 15) { throw 'Incomplete sys oracle closure' }
    function Test-Closure {
        foreach ($entry in $closure) {
            if ((Get-FileHash -LiteralPath (Join-Path $profile.InstalledOtpRoot $entry.Path)).Hash -cne $entry.Sha256) { throw 'sys oracle input changed' }
        }
    }
    Test-Closure
    $directory = [IO.Path]::GetFullPath($Output)
    if (Test-Path -LiteralPath $directory) { throw 'Choose a new output directory' }
    New-Item -ItemType Directory -Path $directory | Out-Null
    $relative = [IO.Path]::GetRelativePath($repository,$directory).Replace('\','/')
    if ($relative.Contains('"') -or $relative.Contains("`n")) { throw 'Unsupported output path' }
    $source = 'tools/Validation/Erlang.SysCompatibility/sys_port_cases.erl'
    $compileHelpers = 'lists:foreach(fun(F)->{ok,_}=compile:file(F,[{outdir,"' + $relative + '"}]) end,filelib:wildcard("tools/Validation/Erlang.SysCompatibility/sys_behaviour_cases.erl")),'
    $compile = $compileHelpers + 'lists:foreach(fun(F)->{ok,_}=compile:file(F,[{outdir,"' + $relative + '"}]) end,filelib:wildcard("tools/Validation/Erlang.SysCompatibility/Reference/*.erl")),'
    $expression = 'erlang:system_flag(backtrace_depth,' + $BacktraceDepth + '),' + $compile + '{ok,sys_port_cases}=compile:file("' + $source + '",[{outdir,"' + $relative + '"}]),true=code:add_patha("' + $relative + '"),ok=sys_port_cases:write("' + $relative + '"),halt().'
    & (Join-Path $profile.InstalledOtpRoot 'bin/erl.exe') -noshell -eval $expression 2>&1 | Tee-Object -FilePath (Join-Path $directory 'oracle.log')
    if ($LASTEXITCODE -ne 0) { throw 'sys oracle capture failed' }
    Test-Closure
    [ordered]@{
        Baseline=$profile.ReleaseTag; Commit=$profile.SourceCommit; BacktraceDepth=$BacktraceDepth
        Fixtures=@(Get-ChildItem -LiteralPath 'tools/Validation/Erlang.SysCompatibility' -Filter '*.erl' -Recurse | ForEach-Object {
            $fixtureBeam=$_.BaseName+'.beam'
            [ordered]@{Source=[IO.Path]::GetRelativePath($repository,$_.FullName).Replace('\','/');SourceSha256=(Get-FileHash -LiteralPath $_.FullName).Hash;Beam=$fixtureBeam;BeamSha256=(Get-FileHash -LiteralPath (Join-Path $directory $fixtureBeam)).Hash}
        })
        ExecutionManifestSha256=$profile.ExecutionManifestSha256; Inputs=$closure
        ObserverSourceSha256=(Get-FileHash -LiteralPath $source).Hash
        ObserverBeamSha256=(Get-FileHash (Join-Path $directory 'sys_port_cases.beam')).Hash
        ExportsSha256=(Get-FileHash (Join-Path $directory 'exports.etf')).Hash
        FeaturesSha256=(Get-FileHash (Join-Path $directory 'features.etf')).Hash
        CorpusSha256=(Get-FileHash (Join-Path $directory 'original.etf')).Hash
    } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $directory 'provenance.json') -Encoding utf8
}
finally { Pop-Location }
