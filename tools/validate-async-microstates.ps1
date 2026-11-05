param(
    [string]$Admission = 'artifacts/library376-official-compiler/official-admission.json',
    [Parameter(Mandatory=$true)][string]$PackageManifest,
    [ValidateSet('Debug','Release')][string]$Configuration = 'Release',
    [string]$Output = 'artifacts/async-microstates',
    [string]$Python = 'python',
    [string]$PlanSha256 = '711A00FEE7695A4DD4D1362933CA755EE7029271FF97B9AB49843DA8D155E644'
)
$ErrorActionPreference = 'Stop'
$repository = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$run = Join-Path ([IO.Path]::GetFullPath($Output)) ([Guid]::NewGuid().ToString('N'))
$arguments = @('-B', (Join-Path $PSScriptRoot 'Validation/Erlang.AsyncMicrostateValidation/collect.py'), '--repository', $repository, '--admission', ([IO.Path]::GetFullPath($Admission)), '--package', ([IO.Path]::GetFullPath($PackageManifest)), '--configuration', $Configuration, '--output', $run, '--plan-sha256', $PlanSha256)
if ([IO.Path]::GetFileNameWithoutExtension($Python) -eq 'py') { $arguments = @('-3') + $arguments }
& $Python @arguments
if ($LASTEXITCODE -ne 0) { throw "Strict actual async microstate comparison failed: $run" }
$completion = Get-Content -LiteralPath (Join-Path $run 'completion.json') -Raw | ConvertFrom-Json
if (-not $completion.Passed -or $completion.Errors.Count -ne 0 -or $completion.Configuration -cne $Configuration) { throw 'Async observation completion failed' }
Write-Host "Actual async original comparison and physical negative fences passed ($Configuration): $run"
