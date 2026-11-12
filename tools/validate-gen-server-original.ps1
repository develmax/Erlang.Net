param(
    [string]$Admission,
    [string]$Output
)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Validation/GenServerValidationContract.ps1')
if (-not $Admission) { $Admission = $GenServerOracle.DefaultAdmission }
if (-not $Output) { $Output = $GenServerOracle.DefaultOutput }
$repository = Split-Path $PSScriptRoot -Parent
Push-Location $repository
try {
    $profile = Get-Content -LiteralPath $Admission -Raw | ConvertFrom-Json
    if ($profile.ReleaseVersion -cne $GenServerOracle.Release -or
        $profile.SourceCommit -cne $GenServerOracle.Commit -or
        -not $profile.EveryExtractedFileMatchesArchive -or
        (Get-FileHash -LiteralPath $profile.ExecutionManifestPath).Hash -cne $profile.ExecutionManifestSha256) {
        throw $GenServerOracle.AdmissionFailure
    }
    $inventory = Get-Content -LiteralPath $profile.ExecutionManifestPath -Raw | ConvertFrom-Json
    $closure = @($inventory | Where-Object {
        $_.Path -eq $GenServerOracle.Launcher -or $_.Path -match $GenServerOracle.RuntimeClosurePattern -or
        $_.Path -match $GenServerOracle.LibraryClosurePattern
    })
    if ($closure.Count -lt $GenServerOracle.MinimumClosureFiles) { throw $GenServerOracle.IncompleteClosure }
    function Test-Closure {
        foreach ($entry in $closure) {
            if ((Get-FileHash -LiteralPath (Join-Path $profile.InstalledOtpRoot $entry.Path)).Hash -cne $entry.Sha256) {
                throw $GenServerOracle.InputChanged
            }
        }
    }
    Test-Closure
    $directory = [IO.Path]::GetFullPath($Output)
    if (Test-Path -LiteralPath $directory) { throw $GenServerOracle.ExistingOutput }
    New-Item -ItemType Directory -Path $directory | Out-Null
    $relative = [IO.Path]::GetRelativePath($repository,$directory).Replace('\','/')
    if ($relative.Contains('"') -or $relative.Contains("`n")) { throw $GenServerOracle.UnsupportedPath }
    $expression = $GenServerOracle.CaptureExpression.Replace('{0}', $GenServerOracle.CompanionSource).Replace('{1}', $GenServerOracle.ObserverSource).Replace('{2}', $relative)
    & (Join-Path $profile.InstalledOtpRoot $GenServerOracle.Launcher) $GenServerOracle.NoShellOption $GenServerOracle.EvalOption $expression 2>&1 | Tee-Object -FilePath (Join-Path $directory $GenServerOracle.Log)
    if ($LASTEXITCODE -ne 0) { throw $GenServerOracle.CaptureFailure }
    Test-Closure
    [ordered]@{
        SuiteSourceSha256=(Get-FileHash -LiteralPath 'tools/Validation/Erlang.GenServerCompatibility/Reference/gen_server_SUITE.erl').Hash
        SuiteBeamSha256=(Get-FileHash -LiteralPath (Join-Path $directory 'gen_server_SUITE.beam')).Hash
        Baseline=$profile.ReleaseTag; Commit=$profile.SourceCommit
        ExecutionManifestSha256=$profile.ExecutionManifestSha256; Inputs=$closure
        ObserverSourceSha256=(Get-FileHash -LiteralPath $GenServerOracle.ObserverSource).Hash
        ObserverBeamSha256=(Get-FileHash (Join-Path $directory $GenServerOracle.ObserverBeam)).Hash
        CompanionSourceSha256=(Get-FileHash -LiteralPath $GenServerOracle.CompanionSource).Hash
        CompanionBeamSha256=(Get-FileHash (Join-Path $directory $GenServerOracle.CompanionBeam)).Hash
        ExportsSha256=(Get-FileHash (Join-Path $directory $GenServerOracle.Exports)).Hash
        CorpusSha256=(Get-FileHash (Join-Path $directory $GenServerOracle.Corpus)).Hash
        EffectsSha256=(Get-FileHash (Join-Path $directory $GenServerOracle.Effects)).Hash
        FeatureBootstrapSha256=(Get-FileHash (Join-Path $directory $GenServerOracle.FeatureBootstrap)).Hash
    } | ConvertTo-Json -Depth $GenServerOracle.ProvenanceJsonDepth | Set-Content -LiteralPath (Join-Path $directory $GenServerOracle.Provenance) -Encoding utf8
}
finally { Pop-Location }
