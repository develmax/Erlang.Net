param(
    [string]$Admission = 'artifacts/library376-official-compiler/official-admission.json',
    [string]$Output = 'artifacts/timer-compatibility',
    [string]$RunnerDll,
    [switch]$CaptureOnly,
    [switch]$IncludeUpstreamSuite,
    [ValidateRange(0, 64)][int]$BacktraceDepth = 8
)
$ErrorActionPreference = 'Stop'
$repository = Split-Path $PSScriptRoot -Parent
$timerReference = Import-PowerShellDataFile (Join-Path $PSScriptRoot 'Validation/Erlang.TimerCompatibility/reference-contract.psd1')
Push-Location $repository
try {
    $profile = Get-Content -LiteralPath $Admission -Raw | ConvertFrom-Json
    if ($profile.ReleaseVersion -cne $timerReference.ReleaseVersion -or
        $profile.SourceCommit -cne $timerReference.SourceCommit -or
        -not $profile.EveryExtractedFileMatchesArchive -or
        (Get-FileHash -LiteralPath $profile.ExecutionManifestPath).Hash -cne $profile.ExecutionManifestSha256) {
        throw $timerReference.ProviderNotAdmitted
    }
    $inventory = Get-Content -LiteralPath $profile.ExecutionManifestPath -Raw | ConvertFrom-Json
    $closure = @($inventory | Where-Object {
        $_.Path -eq 'bin/erl.exe' -or
        $_.Path -match '^erts-[^/]+/bin/(erl.exe|beam.smp.dll)$' -or
        $_.Path -match '^lib/(stdlib-8.1|kernel-[^/]+|erts-17.1)/(ebin|src)/(timer|gen_server|gen|supervisor|proc_lib|sys|ets|lists|maps|erlang)\.(beam|erl)$'
    })
    if ($closure.Count -lt $timerReference.MinimumClosureFiles) { throw $timerReference.IncompleteProviderClosure }
    function Test-Closure {
        foreach ($entry in $closure) {
            if ((Get-FileHash -LiteralPath (Join-Path $profile.InstalledOtpRoot $entry.Path)).Hash -cne $entry.Sha256) {
                throw $timerReference.ProviderInputChanged
            }
        }
    }
    Test-Closure
    $directory = [IO.Path]::GetFullPath($Output)
    if (Test-Path -LiteralPath $directory) { throw $timerReference.OutputAlreadyExists }
    New-Item -ItemType Directory -Path $directory | Out-Null
    $relative = [IO.Path]::GetRelativePath($repository, $directory).Replace('\', '/')
    if ($relative.Contains('"') -or $relative.Contains("`n")) { throw $timerReference.InvalidOutputPath }
    $expression = '{ok,timer_port_cases}=compile:file("' + $timerReference.PrimarySource + '",[{outdir,"' + $relative + '"}]),' +
        'true=code:add_patha("' + $relative + '"),erlang:system_flag(backtrace_depth,' + $BacktraceDepth + '),' +
        'ok=timer_port_cases:write("' + $relative + '"),halt().'
    & (Join-Path $profile.InstalledOtpRoot 'bin/erl.exe') -noshell -eval $expression 2>&1 |
        Tee-Object -FilePath (Join-Path $directory 'oracle.log')
    if ($LASTEXITCODE -ne 0) { throw $timerReference.PrimaryCaptureFailed }
    if ($IncludeUpstreamSuite) {
        $suiteExpression = '{ok,timer_simple_SUITE}=compile:file("' + $timerReference.SuiteSource + '",[{outdir,"' + $relative + '"}]),' +
            '{ok,timer_suite_capture}=compile:file("' + $timerReference.SuiteObserverSource + '",[{outdir,"' + $relative + '"}]),' +
            'true=code:add_patha("' + $relative + '"),erlang:system_flag(backtrace_depth,' + $BacktraceDepth + '),' +
            'ok=timer_suite_capture:write("' + $relative + '"),halt().'
        & (Join-Path $profile.InstalledOtpRoot 'bin/erl.exe') -noshell -eval $suiteExpression 2>&1 |
            Tee-Object -FilePath (Join-Path $directory 'upstream.log')
        if ($LASTEXITCODE -ne 0) { throw $timerReference.UpstreamCaptureFailed }
    }
    Test-Closure
    [ordered]@{
        Baseline = $profile.ReleaseTag
        SourceCommit = $profile.SourceCommit
        BacktraceDepth = $BacktraceDepth
        ProviderArchiveSha256 = $profile.ArchiveSha256
        ExecutionManifestSha256 = $profile.ExecutionManifestSha256
        Closure = $closure
        Files = @(Get-ChildItem -LiteralPath $directory -File | ForEach-Object {
            [ordered]@{ Name = $_.Name; Sha256 = (Get-FileHash -LiteralPath $_.FullName).Hash }
        })
    } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $directory 'provenance.json') -Encoding utf8
    if (-not $CaptureOnly) {
        if (-not $RunnerDll) { throw $timerReference.RunnerRequired }
        foreach ($backend in @('Managed', 'Beam')) {
            & dotnet $RunnerDll $repository $directory $backend "depth:$BacktraceDepth"
            if ($LASTEXITCODE -ne 0) { throw ($timerReference.ComparisonFailed -f $backend) }
            if ($IncludeUpstreamSuite) {
                & dotnet $RunnerDll $repository $directory $backend 'suite' "depth:$BacktraceDepth"
                if ($LASTEXITCODE -ne 0) { throw ($timerReference.ComparisonFailed -f $backend) }
            }
        }
    }
}
finally { Pop-Location }
