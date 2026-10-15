param(
 [Parameter(Mandatory=$true)][ValidatePattern('^[0-9a-f]{40}$')][string]$ExpectedHead,
 [string]$PublishedRef='main'
)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$repo=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$seal='C73AFABAF43F98F81BD94C52788189429F85931C3D7228C8C8346C38FCEDD0F5'
$workflowName='allocator-memory-original-observations.yml'
$attempt=Join-Path $PSScriptRoot 'dispatch-attempt.json'
$result=Join-Path $PSScriptRoot 'dispatch-result.json'
if(Test-Path -LiteralPath $attempt){throw 'An attempt already exists. Inspect remote runs with watch-get-only.ps1; never repeat POST.'}
if(Test-Path -LiteralPath $result){throw 'Prior dispatch result exists'}
$head=(& git -C $repo rev-parse HEAD).Trim()
if($LASTEXITCODE -or $head -cne $ExpectedHead){throw 'Local HEAD must equal the supplied published SHA'}
& py -3.12 -B (Join-Path $PSScriptRoot 'validate-frozen-source.py') --repository $repo
if($LASTEXITCODE){throw 'Frozen installed source validation failed'}
$credentialLines=$null; $passwordLine=$null; $secret=$null; $headers=$null
try {
 $credentialLines="protocol=https`nhost=github.com`n`n"|git -C $repo credential fill
 if($LASTEXITCODE){throw 'Credential provider failed'}
 $passwordLine=$credentialLines|Where-Object {$_ -like 'password=*'}|Select-Object -First 1
 if($null -eq $passwordLine){throw 'Credential provider did not return a token'}
 $secret=$passwordLine.Substring(9)
 $headers=@{'User-Agent'='Erlang.Net-allocator-original-provenance';Authorization="Bearer $secret";Accept='application/vnd.github+json'}
 function Get-GitHub([string]$Url) {
  try { Invoke-RestMethod -Uri $Url -Headers $headers -Method Get }
  catch { throw 'GitHub preflight GET failed; credential and response details suppressed' }
 }
 $api='https://api.github.com/repos/develmax/Erlang.Net'
 $ref=Get-GitHub "$api/commits/$([Uri]::EscapeDataString($PublishedRef))"
 if($ref.sha -cne $ExpectedHead){throw 'Published dispatch ref differs from supplied SHA'}
 $workflow=Get-GitHub "$api/actions/workflows/$workflowName"
 if($workflow.state -ne 'active' -or $workflow.path -cne ".github/workflows/$workflowName"){throw 'Workflow path/state mismatch'}
 foreach($path in @("tools/Erlang.AllocatorMemoryObservations/reviewed-execution-plan.json", ".github/workflows/$workflowName")) {
  $remote=Get-GitHub "$api/contents/$($path)?ref=$ExpectedHead"
  if($remote.type -ne 'file' -or $remote.encoding -ne 'base64'){throw 'Published file was not returned as base64'}
  $bytes=[Convert]::FromBase64String($remote.content)
  $hash=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes))
  if($hash -cne (Get-FileHash -LiteralPath (Join-Path $repo $path)).Hash){throw "Published source mismatch: $path"}
  if($path.EndsWith('reviewed-execution-plan.json') -and $hash -cne $seal){throw 'Approved execution seal mismatch'}
 }
 $record=[ordered]@{Stage='BEFORE_SINGLE_POST';ExpectedHead=$ExpectedHead;PublishedRef=$PublishedRef;WorkflowId=$workflow.id;WorkflowPath=$workflow.path;ExecutionPlanSha256=$seal;StartedAt=[DateTime]::UtcNow.ToString('O');OriginalCompleted=$false;SdkCompared=$false}
 # CreateNew is the once-only boundary, including concurrent launches and ambiguous responses.
 $stream=[IO.File]::Open($attempt,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
 try {$bytes=[Text.Encoding]::UTF8.GetBytes(($record|ConvertTo-Json)); $stream.Write($bytes,0,$bytes.Length); $stream.Flush($true)} finally {$stream.Dispose()}
 $body=@{ref=$PublishedRef;inputs=@{approved_execution_plan_sha256=$seal}}|ConvertTo-Json -Compress
 try {Invoke-RestMethod -Method Post -Uri "$api/actions/workflows/$workflowName/dispatches" -Headers $headers -ContentType 'application/json' -Body $body|Out-Null}
 catch {throw 'Dispatch response failed or was ambiguous. Attempt retained; use GET-only watcher and never repeat POST.'}
 [ordered]@{Stage='SINGLE_POST_ACCEPTED';ExpectedHead=$ExpectedHead;ExecutionPlanSha256=$seal;Time=[DateTime]::UtcNow.ToString('O');OriginalCompleted=$false;SdkCompared=$false}|ConvertTo-Json|Set-Content -LiteralPath $result
 Write-Output 'One dispatch accepted. Original19 execution and terminal verification remain pending.'
} finally {$secret=$null;$passwordLine=$null;$credentialLines=$null;$headers=$null}
