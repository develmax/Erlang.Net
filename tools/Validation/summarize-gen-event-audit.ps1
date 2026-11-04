param(
    [Parameter(Mandatory)][string]$Directory,
    [Parameter(Mandatory)][string]$RunnerDirectory
)
$GenEventContract = @{
    Release = '29.1.1'
    Baseline = 'OTP-29.1.1'
    Commit = 'ad05823719d77c8faee87348ea39513d4e2f99c5'
    MinimumOracleFiles = 30
    ExportCount = 44
    ObservationCount = 105
    OriginalImage = 'src/Otp/Original/Foundation/Erlang.Otp.Stdlib.Beam/Otp/gen_event.beam'
}

$ErrorActionPreference = 'Stop'
$repository = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$directory = [IO.Path]::GetFullPath($Directory)
$provenance = Get-Content (Join-Path $directory 'provenance.json') -Raw | ConvertFrom-Json
$corpus = (Get-FileHash (Join-Path $directory 'original.etf')).Hash
$observer = (Get-FileHash (Join-Path $directory 'gen_event_port_cases.beam')).Hash
if ($provenance.Baseline -cne $GenEventContract.Baseline -or
    $provenance.Commit -cne $GenEventContract.Commit -or
    $provenance.CorpusSha256 -cne $corpus -or $provenance.ObserverBeamSha256 -cne $observer) {
    throw 'gen_event corpus/provider binding mismatch.'
}
foreach ($source in $provenance.Sources.PSObject.Properties) {
    if ((Get-FileHash (Join-Path $repository $source.Name)).Hash -cne $source.Value) {
        throw 'gen_event observer/upstream source changed.'
    }
}
$reports = foreach ($backend in @('Managed', 'Beam')) {
    $report = Get-Content (Join-Path $directory "$backend-report.json") -Raw | ConvertFrom-Json
    if ($report.Backend -cne $backend -or -not $report.CompleteCorpus -or
        $report.Exports -ne $GenEventContract.ExportCount -or $report.Cases -ne $GenEventContract.ObservationCount -or
        $report.Attempted -ne $report.Cases -or $report.Passed -ne $report.Cases -or
        $report.Failures.Count -ne 0 -or $report.CorpusSha256 -cne $corpus -or
        $report.ObserverSha256 -cne $observer) {
        throw "Incomplete or stale gen_event $backend report."
    }
    if ($backend -ceq 'Beam' -and
        $report.InputSha256 -cne (Get-FileHash -LiteralPath (Join-Path $repository $GenEventContract.OriginalImage)).Hash) {
        throw 'gen_event tested BEAM image changed.'
    }
    foreach ($source in @($report.Sources.PSObject.Properties) + @($report.Inputs.PSObject.Properties)) {
        if ((Get-FileHash (Join-Path $repository $source.Name)).Hash -cne $source.Value) {
            throw 'gen_event implementation/dependency source changed.'
        }
    }
    foreach ($assembly in $report.Assemblies.PSObject.Properties) {
        if ((Get-FileHash (Join-Path $RunnerDirectory $assembly.Name)).Hash -cne $assembly.Value) {
            throw 'gen_event execution assembly changed.'
        }
    }
    [ordered]@{ Backend = $backend; Exports = $report.Exports; Cases = $report.Cases; Passed = $report.Passed }
}
[ordered]@{
    Baseline = $provenance.Baseline
    Commit = $provenance.Commit
    CorpusSha256 = $corpus
    Reports = @($reports)
    Scope = 'Focused same-platform gen_event contracts; finite evidence, no global readiness or universal compatibility certification.'
} | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $directory 'audit-summary.json') -Encoding utf8
Write-Output 'gen_event: both complete 105-case reports admitted against current source and execution assemblies.'
