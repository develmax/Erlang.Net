param(
    [string]$Admission,
    [string]$Output
)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Validation/SupervisorValidationContract.ps1')
if (-not $Admission) { $Admission = $SupervisorOracle.DefaultAdmission }
if (-not $Output) { $Output = $SupervisorOracle.DefaultOutput }
$repository = Split-Path $PSScriptRoot -Parent
Push-Location $repository
try {
    $profile = Get-Content -LiteralPath $Admission -Raw | ConvertFrom-Json
    if ($profile.ReleaseVersion -cne $SupervisorOracle.Release -or
        $profile.SourceCommit -cne $SupervisorOracle.Commit -or
        -not $profile.EveryExtractedFileMatchesArchive -or
        (Get-FileHash -LiteralPath $profile.ExecutionManifestPath).Hash -cne $profile.ExecutionManifestSha256) {
        throw $SupervisorOracle.AdmissionFailure
    }
    $inventory = Get-Content -LiteralPath $profile.ExecutionManifestPath -Raw | ConvertFrom-Json
    $closure = @($inventory | Where-Object {
        $_.Path -eq $SupervisorOracle.Launcher -or $_.Path -match $SupervisorOracle.RuntimeClosurePattern -or
        $_.Path -match $SupervisorOracle.LibraryClosurePattern
    })
    if ($closure.Count -lt $SupervisorOracle.MinimumClosureFiles) { throw $SupervisorOracle.IncompleteClosure }
    function Test-Closure {
        foreach ($entry in $closure) {
            if ((Get-FileHash -LiteralPath (Join-Path $profile.InstalledOtpRoot $entry.Path)).Hash -cne $entry.Sha256) {
                throw $SupervisorOracle.InputChanged
            }
        }
    }
    Test-Closure
    $directory = [IO.Path]::GetFullPath($Output)
    if (Test-Path -LiteralPath $directory) { throw $SupervisorOracle.ExistingOutput }
    New-Item -ItemType Directory -Path $directory | Out-Null
    $relative = [IO.Path]::GetRelativePath($repository,$directory).Replace('\','/')
    if ($relative.Contains('"') -or $relative.Contains("`n")) { throw $SupervisorOracle.UnsupportedPath }
    $expression = $SupervisorOracle.CaptureExpression.Replace('{0}', $SupervisorOracle.CompanionSource).Replace('{1}', $SupervisorOracle.ObserverSource).Replace('{2}', $relative)
    & (Join-Path $profile.InstalledOtpRoot $SupervisorOracle.Launcher) $SupervisorOracle.NoShellOption $SupervisorOracle.EvalOption $expression 2>&1 | Tee-Object -FilePath (Join-Path $directory $SupervisorOracle.Log)
    if ($LASTEXITCODE -ne 0) { throw $SupervisorOracle.CaptureFailure }
    Test-Closure
    [ordered]@{
        Baseline=$profile.ReleaseTag; Commit=$profile.SourceCommit
        ExecutionManifestSha256=$profile.ExecutionManifestSha256; Inputs=$closure
        ObserverSourceSha256=(Get-FileHash -LiteralPath $SupervisorOracle.ObserverSource).Hash
        ObserverBeamSha256=(Get-FileHash (Join-Path $directory $SupervisorOracle.ObserverBeam)).Hash
        CompanionSourceSha256=(Get-FileHash -LiteralPath $SupervisorOracle.CompanionSource).Hash
        CompanionBeamSha256=(Get-FileHash (Join-Path $directory $SupervisorOracle.CompanionBeam)).Hash
        ExportsSha256=(Get-FileHash (Join-Path $directory $SupervisorOracle.Exports)).Hash
        CorpusSha256=(Get-FileHash (Join-Path $directory $SupervisorOracle.Corpus)).Hash
        EffectsSha256=(Get-FileHash (Join-Path $directory $SupervisorOracle.Effects)).Hash
        FeatureBootstrapSha256=(Get-FileHash (Join-Path $directory $SupervisorOracle.FeatureBootstrap)).Hash
        ReferenceSources=@(Get-ChildItem tools/Validation/Erlang.SupervisorCompatibility/Reference -Filter '*.erl' | ForEach-Object {
            [ordered]@{Path=[IO.Path]::GetRelativePath($repository,$_.FullName).Replace('\','/');Sha256=(Get-FileHash $_.FullName).Hash}
        })
        ReferenceBeams=@(Get-ChildItem tools/Validation/Erlang.SupervisorCompatibility/Reference -Filter '*.erl' | ForEach-Object {
            $beam=Join-Path $directory ($_.BaseName + '.beam')
            [ordered]@{Path=[IO.Path]::GetRelativePath($directory,$beam).Replace('\','/');Sha256=(Get-FileHash $beam).Hash}
        })
    } | ConvertTo-Json -Depth $SupervisorOracle.ProvenanceJsonDepth | Set-Content -LiteralPath (Join-Path $directory $SupervisorOracle.Provenance) -Encoding utf8
}
finally { Pop-Location }
