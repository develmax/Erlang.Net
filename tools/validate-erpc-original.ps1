param(
    [string]$Admission = 'artifacts/library376-official-compiler/official-admission.json',
    [string]$Output = 'artifacts/erpc-compatibility-capture',
    [string]$RunnerDll,
    [switch]$CaptureOnly,
    [ValidateRange(0,64)][int]$BacktraceDepth = 8
)
$ErrorActionPreference = 'Stop'
$repository = Split-Path $PSScriptRoot -Parent
Push-Location $repository
try {
    $contract = Get-Content tools/Validation/Erlang.ErpcCompatibility/reference-contract.json -Raw | ConvertFrom-Json
    $profile = Get-Content -LiteralPath $Admission -Raw | ConvertFrom-Json
    if ($profile.ReleaseTag -cne $contract.ReleaseTag -or $profile.SourceCommit -cne $contract.SourceCommit -or
        -not $profile.EveryExtractedFileMatchesArchive -or
        (Get-FileHash -LiteralPath $profile.ExecutionManifestPath).Hash -cne $profile.ExecutionManifestSha256) {
        throw 'OTP provider is not admitted against the pinned erpc baseline'
    }
    $source = 'src/Otp/Original/Foundation/Erlang.Otp.Kernel.Beam/Otp/src/erpc.erl'
    $beam = 'src/Otp/Original/Foundation/Erlang.Otp.Kernel.Beam/Otp/ebin/erpc.beam'
    $suite = 'tools/Validation/Erlang.ErpcCompatibility/Reference/erpc_SUITE.erl'
    if ((Get-FileHash $source).Hash -cne $contract.ErpcSourceSha256 -or
        (Get-FileHash $beam).Hash -cne $contract.ErpcBeamSha256 -or
        (Get-FileHash $suite).Hash -cne $contract.UpstreamSuiteSha256) {
        throw 'Pinned erpc or unchanged upstream suite identity differs'
    }
    $inventory = Get-Content -LiteralPath $profile.ExecutionManifestPath -Raw | ConvertFrom-Json
    $closure = @($inventory | Where-Object {
        $_.Path -eq 'bin/erl.exe' -or $_.Path -match '^erts-[^/]+/bin/(erl.exe|beam.smp.dll)$' -or
        $_.Path -match '^lib/[^/]+/ebin/[^/]+\.beam$'
    })
    function Test-ErpcProvider {
        foreach ($entry in $closure) {
            if ((Get-FileHash -LiteralPath (Join-Path $profile.InstalledOtpRoot $entry.Path)).Hash -cne $entry.Sha256) {
                throw ('OTP provider input differs: ' + $entry.Path)
            }
        }
    }
    Test-ErpcProvider
    $directory = [IO.Path]::GetFullPath($Output)
    if (Test-Path -LiteralPath $directory) { throw 'Capture output already exists; use a new immutable directory' }
    New-Item -ItemType Directory -Path $directory | Out-Null
    $relative = [IO.Path]::GetRelativePath($repository, $directory).Replace('\','/')
    if ($relative.Contains('"') -or $relative.Contains("`n")) { throw 'Invalid capture path' }
    $expression = '{ok,erpc_port_cases}=compile:file("tools/Validation/Erlang.ErpcCompatibility/erpc_port_cases.erl",[{outdir,"' + $relative + '"}]),' +
        '{ok,erpc_SUITE}=compile:file("' + $suite + '",[{outdir,"' + $relative + '"}]),' +
        'true=code:add_patha("' + $relative + '"),erlang:system_flag(backtrace_depth,' + $BacktraceDepth + '),' +
        'ok=erpc_port_cases:write("' + $relative + '"),' +
        'L=[{T,apply(erpc_SUITE,T,[[]])}||T<-[call_always_spawn,multicall_always_spawn,timeout_limit]],' +
        'ok=file:write_file("' + $relative + '/upstream.etf",term_to_binary(L)),halt().'
    & (Join-Path $profile.InstalledOtpRoot 'bin/erl.exe') -noshell -eval $expression 2>&1 |
        Tee-Object -FilePath (Join-Path $directory 'oracle.log')
    if ($LASTEXITCODE -ne 0) { throw 'Original OTP erpc capture failed' }
    Test-ErpcProvider
    $inputs = @($source,$beam,$suite,'src/Otp/Managed/Erlang.Otp.Kernel/Registration/KernelModules.cs','tools/Validation/Erlang.ErpcCompatibility/erpc_port_cases.erl',
        'tools/Validation/Erlang.ErpcCompatibility/reference-contract.json','tools/validate-erpc-original.ps1') +
        @(Get-ChildItem tools/Validation/Erlang.ErpcCompatibility/*.cs | ForEach-Object { [IO.Path]::GetRelativePath($repository,$_.FullName).Replace('\','/') }) +
        @(Get-ChildItem src/Otp/Managed/Erlang.Otp.Kernel/Rpc/Erpc*.cs | ForEach-Object { [IO.Path]::GetRelativePath($repository,$_.FullName).Replace('\','/') })
    $sourceFiles = @($inputs | ForEach-Object { [ordered]@{ Path = $_; Sha256 = (Get-FileHash -LiteralPath $_).Hash } })
    [ordered]@{
        Baseline = $contract.ReleaseTag
        SourceCommit = $contract.SourceCommit
        BacktraceDepth = $BacktraceDepth
        ExecutionManifestSha256 = $profile.ExecutionManifestSha256
        ProviderClosure = $closure
        SourceFiles = $sourceFiles
        CaptureFiles = @(Get-ChildItem -LiteralPath $directory -File | ForEach-Object {
            [ordered]@{ Name = $_.Name; Sha256 = (Get-FileHash -LiteralPath $_.FullName).Hash }
        })
    } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $directory 'provenance.json') -Encoding utf8
    if (-not $CaptureOnly) {
        if (-not $RunnerDll) { throw 'Supply a built erpc validator DLL or use CaptureOnly' }
        foreach ($backend in @('Managed','Beam')) {
            & dotnet $RunnerDll $repository $directory $backend
            if ($LASTEXITCODE -ne 0) { throw ($backend + ' erpc comparison failed') }
        }
    }
}
finally { Pop-Location }
