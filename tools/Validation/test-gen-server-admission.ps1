param(
    [string]$Repository = '.',
    [Parameter(Mandatory)][string]$Oracle,
    [Parameter(Mandatory)][string]$Validator,
    [Parameter(Mandatory)][string]$Output
)
$ErrorActionPreference = 'Stop'
$repositoryPath = [IO.Path]::GetFullPath($Repository)
$oraclePath = [IO.Path]::GetFullPath($Oracle)
$validatorPath = [IO.Path]::GetFullPath($Validator)
$outputPath = [IO.Path]::GetFullPath($Output)
if (Test-Path -LiteralPath $outputPath) { throw 'Choose a new negative-control directory.' }
New-Item -ItemType Directory -Path $outputPath | Out-Null
Get-ChildItem -LiteralPath $oraclePath -File | Where-Object {
    $_.Extension -in '.beam', '.etf', '.json' -and $_.Name -notlike '*-report.json'
} | Copy-Item -Destination $outputPath
$provenancePath = Join-Path $outputPath 'provenance.json'
$provenance = Get-Content -LiteralPath $provenancePath -Raw | ConvertFrom-Json
$provenance.CorpusSha256 = '0' * 64
$provenance | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $provenancePath -Encoding utf8
$logPath = Join-Path $outputPath 'rejection.log'
& dotnet $validatorPath $repositoryPath $outputPath Managed 2>&1 | Out-File -LiteralPath $logPath -Encoding utf8
$code = $LASTEXITCODE
$log = Get-Content -LiteralPath $logPath -Raw
$rejected = $code -ne 0 -and $log.Contains('gen_server immutable oracle admission failed') -and -not $log.Contains('Prepare: ')
[ordered]@{
    CorruptedCorpusIdentityRejected = $rejected
    ProductExecutionStarted = $log.Contains('Prepare: ')
    ProcessExitCode = $code
    Complete = $rejected
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $outputPath 'negative-controls.json') -Encoding utf8
if (-not $rejected) { throw 'The invalid gen_server oracle receipt was not rejected before execution.' }
