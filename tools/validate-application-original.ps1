param(
    [string]$Admission,
    [string]$Output,
    [int]$BacktraceDepth = 8
)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Validation/ApplicationValidationContract.ps1')
if (-not $Admission) { $Admission = $ApplicationOracle.DefaultAdmission }
if (-not $Output) { $Output = $ApplicationOracle.DefaultOutput }
$repository = Split-Path $PSScriptRoot -Parent
Push-Location $repository
try {
    $profile = Get-Content -LiteralPath $Admission -Raw | ConvertFrom-Json
    if ($profile.ReleaseVersion -cne $ApplicationOracle.Release -or
        $profile.SourceCommit -cne $ApplicationOracle.Commit -or
        -not $profile.EveryExtractedFileMatchesArchive -or
        (Get-FileHash -LiteralPath $profile.ExecutionManifestPath).Hash -cne $profile.ExecutionManifestSha256) {
        throw $ApplicationOracle.AdmissionFailure
    }
    $inventory = Get-Content -LiteralPath $profile.ExecutionManifestPath -Raw | ConvertFrom-Json
    $closure = @($inventory | Where-Object {
        $_.Path -eq $ApplicationOracle.Launcher -or $_.Path -match $ApplicationOracle.RuntimeClosurePattern -or
        $_.Path -match $ApplicationOracle.LibraryClosurePattern
    })
    if ($closure.Count -lt $ApplicationOracle.MinimumClosureFiles) { throw $ApplicationOracle.IncompleteClosure }
    function Test-Closure {
        foreach ($entry in $closure) {
            if ((Get-FileHash -LiteralPath (Join-Path $profile.InstalledOtpRoot $entry.Path)).Hash -cne $entry.Sha256) {
                throw $ApplicationOracle.InputChanged
            }
        }
    }
    Test-Closure
    foreach ($name in @('application_SUITE.erl','appinc1.app','appinc2.app')) {
        $expectedBlob = git -C ../otp rev-parse "OTP-29.1.1:lib/kernel/test/$name"
        $actualBlob = git hash-object --no-filters "tools/Validation/Erlang.ApplicationCompatibility/Upstream/$name"
        if ($LASTEXITCODE -ne 0 -or $expectedBlob -cne $actualBlob) { throw 'Changed pinned application test fixture.' }
    }
    $directory = [IO.Path]::GetFullPath($Output)
    if (Test-Path -LiteralPath $directory) { throw $ApplicationOracle.ExistingOutput }
    New-Item -ItemType Directory -Path $directory | Out-Null
    $relative = [IO.Path]::GetRelativePath($repository,$directory).Replace('\','/')
    if ($relative.Contains('"') -or $relative.Contains("`n")) { throw $ApplicationOracle.UnsupportedPath }
    $expression = "erlang:system_flag(backtrace_depth,$BacktraceDepth)," + $ApplicationOracle.CaptureExpression.Replace('{0}', $ApplicationOracle.CompanionSource).Replace('{1}', $ApplicationOracle.ObserverSource).Replace('{2}', $relative).Replace('{3}', $ApplicationOracle.UpstreamSource)
    & (Join-Path $profile.InstalledOtpRoot $ApplicationOracle.Launcher) $ApplicationOracle.NoShellOption $ApplicationOracle.EvalOption $expression 2>&1 | Tee-Object -FilePath (Join-Path $directory $ApplicationOracle.Log)
    if ($LASTEXITCODE -ne 0) { throw $ApplicationOracle.CaptureFailure }
    Test-Closure
    [ordered]@{
        Baseline=$profile.ReleaseTag; Commit=$profile.SourceCommit
        BacktraceDepth=$BacktraceDepth
        ExecutionManifestSha256=$profile.ExecutionManifestSha256; Inputs=$closure
        ObserverSourceSha256=(Get-FileHash -LiteralPath $ApplicationOracle.ObserverSource).Hash
        ObserverBeamSha256=(Get-FileHash (Join-Path $directory $ApplicationOracle.ObserverBeam)).Hash
        CompanionSourceSha256=(Get-FileHash -LiteralPath $ApplicationOracle.CompanionSource).Hash
        CompanionBeamSha256=(Get-FileHash (Join-Path $directory $ApplicationOracle.CompanionBeam)).Hash
        UpstreamSourceSha256=(Get-FileHash -LiteralPath $ApplicationOracle.UpstreamSource).Hash
        UpstreamBeamSha256=(Get-FileHash (Join-Path $directory 'application_SUITE.beam')).Hash
        UpstreamAppFiles=@{
            'appinc1.app'=(Get-FileHash 'tools/Validation/Erlang.ApplicationCompatibility/Upstream/appinc1.app').Hash
            'appinc2.app'=(Get-FileHash 'tools/Validation/Erlang.ApplicationCompatibility/Upstream/appinc2.app').Hash
        }
        ExportsSha256=(Get-FileHash (Join-Path $directory $ApplicationOracle.Exports)).Hash
        CorpusSha256=(Get-FileHash (Join-Path $directory $ApplicationOracle.Corpus)).Hash
        EffectsSha256=(Get-FileHash (Join-Path $directory $ApplicationOracle.Effects)).Hash
        FeatureBootstrapSha256=(Get-FileHash (Join-Path $directory $ApplicationOracle.FeatureBootstrap)).Hash
        PrerequisitesSha256=(Get-FileHash (Join-Path $directory 'prerequisites.etf')).Hash
    } | ConvertTo-Json -Depth $ApplicationOracle.ProvenanceJsonDepth | Set-Content -LiteralPath (Join-Path $directory $ApplicationOracle.Provenance) -Encoding utf8
}
finally { Pop-Location }
