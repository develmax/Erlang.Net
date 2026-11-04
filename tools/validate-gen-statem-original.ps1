param(
    [string]$Admission,
    [string]$Output
)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Validation/GenStatemValidationContract.ps1')
if (-not $Admission) { $Admission = $GenStatemOracle.DefaultAdmission }
if (-not $Output) { $Output = $GenStatemOracle.DefaultOutput }
$repository = Split-Path $PSScriptRoot -Parent
Push-Location $repository
try {
    $profile = Get-Content -LiteralPath $Admission -Raw | ConvertFrom-Json
    if ($profile.ReleaseVersion -cne $GenStatemOracle.Release -or
        $profile.SourceCommit -cne $GenStatemOracle.Commit -or
        -not $profile.EveryExtractedFileMatchesArchive -or
        (Get-FileHash -LiteralPath $profile.ExecutionManifestPath).Hash -cne $profile.ExecutionManifestSha256) {
        throw $GenStatemOracle.AdmissionFailure
    }
    $inventory = Get-Content -LiteralPath $profile.ExecutionManifestPath -Raw | ConvertFrom-Json
    $closure = @($inventory | Where-Object {
        $_.Path -eq $GenStatemOracle.Launcher -or $_.Path -match $GenStatemOracle.RuntimeClosurePattern -or
        $_.Path -match $GenStatemOracle.LibraryClosurePattern
    })
    if ($closure.Count -lt $GenStatemOracle.MinimumClosureFiles) { throw $GenStatemOracle.IncompleteClosure }
    function Test-Closure {
        foreach ($entry in $closure) {
            if ((Get-FileHash -LiteralPath (Join-Path $profile.InstalledOtpRoot $entry.Path)).Hash -cne $entry.Sha256) {
                throw $GenStatemOracle.InputChanged
            }
        }
    }
    Test-Closure
    $directory = [IO.Path]::GetFullPath($Output)
    if (Test-Path -LiteralPath $directory) { throw $GenStatemOracle.ExistingOutput }
    New-Item -ItemType Directory -Path $directory | Out-Null
    $relative = [IO.Path]::GetRelativePath($repository,$directory).Replace('\','/')
    if ($relative.Contains('"') -or $relative.Contains("`n")) { throw $GenStatemOracle.UnsupportedPath }
    $expression = $GenStatemOracle.CaptureExpression.Replace('{0}', $GenStatemOracle.CompanionSource).Replace('{1}', $GenStatemOracle.ObserverSource).Replace('{2}', $relative)
    & (Join-Path $profile.InstalledOtpRoot $GenStatemOracle.Launcher) $GenStatemOracle.NoShellOption $GenStatemOracle.EvalOption $expression 2>&1 | Tee-Object -FilePath (Join-Path $directory $GenStatemOracle.Log)
    if ($LASTEXITCODE -ne 0) { throw $GenStatemOracle.CaptureFailure }
    Test-Closure
    [ordered]@{
        Baseline=$profile.ReleaseTag; Commit=$profile.SourceCommit
        ExecutionManifestSha256=$profile.ExecutionManifestSha256; Inputs=$closure
        ObserverSourceSha256=(Get-FileHash -LiteralPath $GenStatemOracle.ObserverSource).Hash
        ObserverBeamSha256=(Get-FileHash (Join-Path $directory $GenStatemOracle.ObserverBeam)).Hash
        CompanionSourceSha256=(Get-FileHash -LiteralPath $GenStatemOracle.CompanionSource).Hash
        CompanionBeamSha256=(Get-FileHash (Join-Path $directory $GenStatemOracle.CompanionBeam)).Hash
        ExportsSha256=(Get-FileHash (Join-Path $directory $GenStatemOracle.Exports)).Hash
        CorpusSha256=(Get-FileHash (Join-Path $directory $GenStatemOracle.Corpus)).Hash
        EffectsSha256=(Get-FileHash (Join-Path $directory $GenStatemOracle.Effects)).Hash
        FeatureBootstrapSha256=(Get-FileHash (Join-Path $directory $GenStatemOracle.FeatureBootstrap)).Hash
    } | ConvertTo-Json -Depth $GenStatemOracle.ProvenanceJsonDepth | Set-Content -LiteralPath (Join-Path $directory $GenStatemOracle.Provenance) -Encoding utf8
}
finally { Pop-Location }
