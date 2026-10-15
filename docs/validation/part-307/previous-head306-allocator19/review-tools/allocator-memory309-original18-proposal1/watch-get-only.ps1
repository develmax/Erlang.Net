param(
 [Parameter(Mandatory=$true)][ValidatePattern('^[0-9a-f]{40}$')][string]$ExpectedHead,
 [ValidatePattern('^[0-9]*$')][string]$RunId='',
 [switch]$DownloadTerminalArtifact
)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
function ConvertTo-UtcTimestamp([object]$Value) {
 if($Value -is [DateTimeOffset]){return $Value.ToUniversalTime()}
 if($Value -is [DateTime]){
  if($Value.Kind -eq [DateTimeKind]::Unspecified){$Value=[DateTime]::SpecifyKind($Value,[DateTimeKind]::Utc)}
  return [DateTimeOffset]::new($Value.ToUniversalTime())
 }
 return [DateTimeOffset]::Parse([string]$Value,[Globalization.CultureInfo]::InvariantCulture,([Globalization.DateTimeStyles]::AssumeUniversal -bor [Globalization.DateTimeStyles]::AdjustToUniversal))
}
$repo=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$attempt=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'dispatch-attempt.json') -Raw|ConvertFrom-Json
if($attempt.ExpectedHead -cne $ExpectedHead -or $attempt.ExecutionPlanSha256 -cne 'C73AFABAF43F98F81BD94C52788189429F85931C3D7228C8C8346C38FCEDD0F5'){throw 'Attempt head/seal mismatch'}
$credentialLines=$null;$passwordLine=$null;$secret=$null;$headers=$null
try {
 $credentialLines="protocol=https`nhost=github.com`n`n"|git -C $repo credential fill
 if($LASTEXITCODE){throw 'Credential provider failed'}
 $passwordLine=$credentialLines|Where-Object {$_ -like 'password=*'}|Select-Object -First 1
 if($null -eq $passwordLine){throw 'Credential provider did not return a token'}
 $secret=$passwordLine.Substring(9)
 $headers=@{'User-Agent'='Erlang.Net-allocator-original-provenance';Authorization="Bearer $secret";Accept='application/vnd.github+json'}
 function Get-GitHub([string]$Url) {
  try {Invoke-RestMethod -Uri $Url -Headers $headers -Method Get}
  catch {throw 'GitHub watcher GET failed; credential and response details suppressed'}
 }
 $api='https://api.github.com/repos/develmax/Erlang.Net'
 if(!$RunId) {
  $runs=Get-GitHub "$api/actions/workflows/$($attempt.WorkflowId)/runs?event=workflow_dispatch&head_sha=$ExpectedHead&per_page=100"
  $found=@($runs.workflow_runs|Where-Object {$_.head_sha -ceq $ExpectedHead -and (ConvertTo-UtcTimestamp $_.created_at) -ge (ConvertTo-UtcTimestamp $attempt.StartedAt).AddSeconds(-5)})
  if($found.Count -eq 0){Write-Output 'No matching run visible yet; GET-only inspection may be repeated, POST must not be repeated.';return}
  if($found.Count -ne 1){throw 'Multiple matching runs; supply and independently inspect the exact RunId'}
  $RunId=[string]$found[0].id
 }
 $run=Get-GitHub "$api/actions/runs/$RunId"
 if($run.head_sha -cne $ExpectedHead -or $run.event -cne 'workflow_dispatch' -or $run.workflow_id -ne $attempt.WorkflowId -or $run.path -cne $attempt.WorkflowPath -or (ConvertTo-UtcTimestamp $run.created_at) -lt (ConvertTo-UtcTimestamp $attempt.StartedAt).AddSeconds(-5)){throw 'Run head/workflow/event/time differs from the recorded attempt'}
 if($run.run_attempt -ne 1){throw 'Automatic adoption of reruns is forbidden; this run is not attempt1'}
 $jobs=Get-GitHub "$api/actions/runs/$RunId/attempts/1/jobs?per_page=100"
 if($jobs.total_count -gt 100){throw 'Unexpected job pagination'}
 $artifacts=Get-GitHub "$api/actions/runs/$RunId/artifacts?per_page=100"
 if($artifacts.total_count -gt 100){throw 'Unexpected artifact pagination'}
 $out=Join-Path $PSScriptRoot ('watch-'+$RunId+'-'+[DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffffffZ'))
 New-Item -ItemType Directory -Path $out|Out-Null
 $run|ConvertTo-Json -Depth 100|Set-Content -LiteralPath (Join-Path $out 'run.json')
 $jobs|ConvertTo-Json -Depth 100|Set-Content -LiteralPath (Join-Path $out 'jobs.json')
 $artifacts|ConvertTo-Json -Depth 100|Set-Content -LiteralPath (Join-Path $out 'artifacts.json')
 [ordered]@{Status='GET_ONLY_RUN_SNAPSHOT';ExpectedHead=$ExpectedHead;RunId=$RunId;RunAttempt=1;ExecutionPlanSha256=$attempt.ExecutionPlanSha256;RunStatus=$run.status;Conclusion=$run.conclusion;ArtifactTypedReviewCompleted=$false;SdkCompared=$false;SnapshotAt=[DateTime]::UtcNow.ToString('O')}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $out 'snapshot.json')
 if($DownloadTerminalArtifact) {
  if($run.status -ne 'completed'){throw 'Download requested before terminal state; snapshot retained'}
  $matching=@($artifacts.artifacts|Where-Object {$_.name -ceq 'allocator-memory-original-nineteen-observations' -and !$_.expired})
  if($matching.Count -ne 1){throw 'Exactly one nonexpired original19 artifact required; snapshot retained'}
  try {
   Invoke-WebRequest -Method Get -Uri "$api/actions/artifacts/$($matching[0].id)/zip" -Headers $headers -OutFile (Join-Path $out 'artifact.zip') -MaximumRedirection 5|Out-Null
   Invoke-WebRequest -Method Get -Uri "$api/actions/runs/$RunId/attempts/1/logs" -Headers $headers -OutFile (Join-Path $out 'terminal-logs.zip') -MaximumRedirection 5|Out-Null
  } catch {throw 'Terminal GET download failed; snapshot/partial evidence retained; credential details suppressed'}
  [ordered]@{ArtifactZipSha256=(Get-FileHash -LiteralPath (Join-Path $out 'artifact.zip')).Hash;TerminalLogsZipSha256=(Get-FileHash -LiteralPath (Join-Path $out 'terminal-logs.zip')).Hash;ArtifactId=$matching[0].id;RunId=$RunId;RunAttempt=1;ExpectedHead=$ExpectedHead}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $out 'download-hashes.json')
 }
 Write-Output "Run $RunId status=$($run.status) conclusion=$($run.conclusion); snapshot=$out; typed artifact review pending."
} finally {$secret=$null;$passwordLine=$null;$credentialLines=$null;$headers=$null}
