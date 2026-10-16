param([Parameter(Mandatory=$true)][ValidateSet('ci','native','ordinary','reallocation','distribution')][string]$Kind)

$ErrorActionPreference='Stop'

$mapping=Get-Content artifacts/part332-remote-new-review/monitor/mapped-runs.json -Raw | ConvertFrom-Json

$target=@($mapping.Runs | Where-Object Kind -eq $Kind);if($target.Count -ne 1){throw 'Unique mapped run required'}

$target=$target[0];$rid=$target.RunId;$head=$mapping.PublishedHead

$out="artifacts/part332-remote-new-review/$Kind";if(Test-Path $out){throw 'Terminal folder exists; inspect rather than overwrite'}

$credentialLines=$null;$passwordLine=$null;$secret=$null;$headers=$null

try {

 $credentialLines="protocol=https`nhost=github.com`n`n" | git credential fill

 if($LASTEXITCODE -ne 0){throw 'Credential helper failed; details suppressed'}

 $passwordLine=$credentialLines | Where-Object {$_ -like 'password=*'} | Select-Object -First 1

 if(!$passwordLine){throw 'Stored credential unavailable'}

 $secret=$passwordLine.Substring(9);$headers=@{'User-Agent'='Erlang.Net-part332-terminal-readonly';Authorization="Bearer $secret";Accept='application/vnd.github+json'}

 $api='https://api.github.com/repos/develmax/Erlang.Net'

 try {

  $run=Invoke-RestMethod -Method Get -Uri "$api/actions/runs/$rid" -Headers $headers

  if($run.status -ne 'completed' -or $run.id -ne $rid -or $run.head_sha -ne $head -or $run.run_attempt -ne $target.RunAttempt -or $run.path -ne $target.Path){throw 'Exact mapped run not terminal'}

  New-Item -ItemType Directory -Path $out | Out-Null

  $run | ConvertTo-Json -Depth 40 | Set-Content "$out/run.json"

  $jobs=Invoke-RestMethod -Method Get -Uri "$api/actions/runs/$rid/jobs?per_page=100" -Headers $headers

  $items=Invoke-RestMethod -Method Get -Uri "$api/actions/runs/$rid/artifacts?per_page=100" -Headers $headers

  $jobs | ConvertTo-Json -Depth 40 | Set-Content "$out/jobs.json"

  $items | ConvertTo-Json -Depth 40 | Set-Content "$out/artifacts.json"

  if($jobs.total_count -ne $jobs.jobs.Count -or $items.total_count -ne $items.artifacts.Count){throw 'Pagination incomplete'}

  foreach($job in $jobs.jobs){

   if($job.head_sha -ne $head -or $job.run_id -ne $rid -or $job.run_attempt -ne $target.RunAttempt -or $job.status -ne 'completed'){throw 'Job binding mismatch'}

   Invoke-WebRequest -Method Get -Uri "$api/actions/jobs/$($job.id)/logs" -Headers $headers -OutFile "$out/job-$($job.id).log"

  }

  foreach($item in $items.artifacts){

   if($item.expired -or $item.workflow_run.head_sha -ne $head -or $item.workflow_run.id -ne $rid){throw 'Artifact binding mismatch'}

   $zip="$out/artifact-$($item.id).zip"

   Invoke-WebRequest -Method Get -Uri $item.archive_download_url -Headers $headers -OutFile $zip

   if((Get-Item -LiteralPath $zip).Length -ne $item.size_in_bytes -or (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash -ne $item.digest.Substring(7).ToUpperInvariant()){throw 'Artifact byte/digest mismatch'}

  }

 } catch {throw 'Terminal GET/capture failed; partial artifacts retained; credential/response details suppressed'}

 @{Status='TERMINAL_RAW_CAPTURED_DIGESTS_VERIFIED_NOT_SEMANTICALLY_AUDITED';Kind=$Kind;RunId=$rid;Head=$head;RunAttempt=$target.RunAttempt;WorkflowConclusion=$run.conclusion;ObservedUtc=[DateTime]::UtcNow.ToString('O');Jobs=$jobs.jobs.Count;Artifacts=$items.artifacts.Count;PostExecuted=$false;MainEdited=$false;NativeExecuted=$false} | ConvertTo-Json | Set-Content "$out/raw-capture.json"

 Write-Output ("Terminal captured: "+$Kind+" run="+$rid+" conclusion="+$run.conclusion+" jobs="+$jobs.jobs.Count+" artifacts="+$items.artifacts.Count)

} finally {$credentialLines=$null;$passwordLine=$null;$secret=$null;$headers=$null}

