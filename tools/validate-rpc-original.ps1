param(
    [string]$Admission = 'artifacts/library376-official-compiler/official-admission.json',
    [string]$Output = 'artifacts/rpc-compatibility',
    [string]$RunnerDll,
    [switch]$CaptureOnly,
    [ValidateRange(0,64)][int]$BacktraceDepth = 8
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
        throw 'RPC oracle provider is not admitted as pinned OTP-29.1.1'
    }
    $inventory = Get-Content -LiteralPath $profile.ExecutionManifestPath -Raw | ConvertFrom-Json
    $closure = @($inventory | Where-Object {
        $_.Path -eq 'bin/erl.exe' -or
        $_.Path -match '^erts-[^/]+/bin/(erl.exe|beam.smp.dll)$' -or
        $_.Path -match '^lib/[^/]+/(ebin|src)/(rpc|erpc|gen_server|gen|proc_lib|sys|supervisor|lists|maps|ets|net_kernel|persistent_term|unicode|erlang|logger|logger_config|logger_filters|logger_backend|logger_server|logger_proxy|proplists|queue)\.(beam|erl)$' -or
        $_.Path -match '^lib/common_test-[^/]+/include/ct.hrl$'
    })
    if ($closure.Count -lt 40) { throw 'RPC oracle execution closure is incomplete' }
    function Test-Closure {
        foreach ($entry in $closure) {
            if ((Get-FileHash -LiteralPath (Join-Path $profile.InstalledOtpRoot $entry.Path)).Hash -cne $entry.Sha256) {
                throw ('RPC provider input changed: ' + $entry.Path)
            }
        }
    }
    Test-Closure
    $directory = [IO.Path]::GetFullPath($Output)
    if (Test-Path -LiteralPath $directory) { throw 'RPC output must be a new immutable directory' }
    New-Item -ItemType Directory -Path $directory | Out-Null
    $relative = [IO.Path]::GetRelativePath($repository, $directory).Replace('\','/')
    if ($relative.Contains('"') -or $relative.Contains("`n")) { throw 'Invalid RPC output path' }
    $suite = 'tools/Validation/Erlang.RpcCompatibility/Reference/rpc_SUITE.erl'
    $source = 'tools/Validation/Erlang.RpcCompatibility/rpc_port_cases.erl'
    $expectedSuite = git -C ../otp rev-parse OTP-29.1.1:lib/kernel/test/rpc_SUITE.erl
    if ($LASTEXITCODE -ne 0) { throw 'Cannot identify the pinned upstream RPC suite' }
    $actualSuite = git hash-object --no-filters $suite
    if ($LASTEXITCODE -ne 0 -or $actualSuite -cne $expectedSuite) { throw 'Upstream RPC suite bytes changed' }
    $retainedSource = 'src/Otp/Original/Foundation/Erlang.Otp.Kernel.Beam/Otp/src/rpc.erl'
    $expectedSource = git -C ../otp rev-parse OTP-29.1.1:lib/kernel/src/rpc.erl
    if ($LASTEXITCODE -ne 0) { throw 'Cannot identify the pinned rpc source' }
    $actualSource = git hash-object --no-filters $retainedSource
    if ($LASTEXITCODE -ne 0 -or $actualSource -cne $expectedSource) { throw 'Retained rpc source differs from the baseline' }
    $retainedBeam = 'src/Otp/Original/Foundation/Erlang.Otp.Kernel.Beam/Otp/ebin/rpc.beam'
    $beamIdentity = @($closure | Where-Object { $_.Path -match '^lib/kernel-[^/]+/ebin/rpc.beam$' })
    if ($beamIdentity.Count -ne 1 -or (Get-FileHash -LiteralPath $retainedBeam).Hash -cne $beamIdentity[0].Sha256) {
        throw 'Retained rpc BEAM differs from the admitted original release'
    }
    $expression = '{ok,rpc_SUITE}=compile:file("' + $suite + '",[{outdir,"' + $relative + '"}]),' +
        '{ok,rpc_port_cases}=compile:file("' + $source + '",[{outdir,"' + $relative + '"}]),' +
        'true=code:add_patha("' + $relative + '"),erlang:system_flag(backtrace_depth,' + $BacktraceDepth + '),' +
        'ok=rpc_port_cases:write("' + $relative + '"),halt().'
    & (Join-Path $profile.InstalledOtpRoot 'bin/erl.exe') +S 2:2 -noshell -eval $expression 2>&1 |
        Tee-Object -FilePath (Join-Path $directory 'oracle.log')
    if ($LASTEXITCODE -ne 0) { throw 'Original OTP RPC capture failed' }
    Test-Closure
    @{
        Baseline = $profile.ReleaseTag
        Commit = $profile.SourceCommit
        ArchiveSha256 = $profile.ArchiveSha256
        ExecutionManifestSha256 = $profile.ExecutionManifestSha256
        Inputs = $closure
        ObserverSha256 = (Get-FileHash -LiteralPath $source).Hash
        UpstreamSuiteSha256 = (Get-FileHash -LiteralPath $suite).Hash
        BacktraceDepth = $BacktraceDepth
    } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $directory 'oracle-provenance.json')
    if ($CaptureOnly) { return }
    if (-not $RunnerDll) {
        dotnet build tools/Validation/Erlang.RpcCompatibility/Erlang.RpcCompatibility.csproj -m:1
        if ($LASTEXITCODE -ne 0) { throw 'RPC comparison runner build failed' }
        $RunnerDll = 'tools/Validation/Erlang.RpcCompatibility/bin/Debug/net10.0/Erlang.RpcCompatibility.dll'
    }
    foreach ($backend in @('Managed','Beam')) {
        dotnet $RunnerDll $repository $directory $backend $BacktraceDepth 2>&1 |
            Tee-Object -FilePath (Join-Path $directory ($backend + '.log'))
        if ($LASTEXITCODE -ne 0) { throw ('RPC focused comparison failed: ' + $backend) }
        if ($BacktraceDepth -eq 8) {
            dotnet $RunnerDll $repository $directory $backend Connected 2>&1 |
                Tee-Object -FilePath (Join-Path $directory ($backend + '-connected.log'))
            if ($LASTEXITCODE -ne 0) { throw ('RPC connected checks failed: ' + $backend) }
        }
    }
} finally { Pop-Location }
