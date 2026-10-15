# Root only after source review. This is local packaging; no network dispatch or publication.
param([Parameter(Mandatory=$true)][string]$Config,[Parameter(Mandatory=$true)][ValidateSet('ReadinessScope','Finalize','Stage')][string]$Phase)
$ErrorActionPreference='Stop'
$python='C:/Users/Maksim/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$configData=Get-Content -LiteralPath $Config -Raw | ConvertFrom-Json
if (!$configData.RootExecutionApproved -or !$configData.BoundToActualComposite -or $configData.Part -ne 312) { throw 'Root-reviewed actual configuration required' }
$stamp=Join-Path $PSScriptRoot ('root-once-'+$Phase+'.json')
$stream=[System.IO.File]::Open($stamp,[System.IO.FileMode]::CreateNew,[System.IO.FileAccess]::Write,[System.IO.FileShare]::None)
try {
 $bytes=[Text.Encoding]::UTF8.GetBytes((@{Phase=$Phase;Config=$Config;StartedUtc=[DateTime]::UtcNow.ToString('O');Status='StartedOnlyNotPass'} | ConvertTo-Json));$stream.Write($bytes,0,$bytes.Length)
} finally { $stream.Dispose() }
$script=switch($Phase) {'ReadinessScope' {'readiness312.py'} 'Finalize' {'finalize312.py'} 'Stage' {'stage312.py'}}
if ($Phase -eq 'Stage') { & $python (Join-Path $PSScriptRoot $script) $Config --apply } else { & $python (Join-Path $PSScriptRoot $script) $Config }
if ($LASTEXITCODE -ne 0) { throw ('Packaging phase failed; preserve once stamp and inspect before any retry: '+$Phase) }
