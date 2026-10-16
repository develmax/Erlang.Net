param([switch]$Initial)

$ErrorActionPreference='Stop'

$head='ed2785a000ae8b1c520707df3224c2807c959bc3'

$out='artifacts/part330-remote-cleanup-review/monitor'

$stamp=[DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffffffZ')

if (!$Initial) {

 $prior=@(Get-ChildItem -LiteralPath $out -Filter 'status-*.json' | Sort-Object Name) | Select-Object -Last 1

 if($prior){$seen=Get-Content -LiteralPath $prior.FullName -Raw | ConvertFrom-Json;if(([DateTimeOffset]::UtcNow-[DateTimeOffset]$seen.ObservedUtc).TotalSeconds -lt 120){throw 'Read-only poll spacing requires at least120seconds'}}

}

$credentialLines=$null;$passwordLine=$null;$secret=$null;$headers=$null

try {

 $credentialLines="protocol=https`nhost=github.com`n`n" | & 'C:/Users/Maksim/.cache/codex-runtimes/codex-primary-runtime/dependencies/native/git/cmd/git.exe' credential fill

 if ($LASTEXITCODE -ne 0) {throw 'Credential helper failed; details suppressed'}

 $passwordLine=$credentialLines | Where-Object {$_ -like 'password=*'} | Select-Object -First 1

 if (!$passwordLine) {throw 'No stored credential returned'}

 $secret=$passwordLine.Substring(9)

 $headers=@{'User-Agent'='Erlang.Net-part330-readonly-provenance';Authorization="Bearer $secret";Accept='application/vnd.github+json'}

 $api='https://api.github.com/repos/develmax/Erlang.Net'

 try {

  $branch=Invoke-RestMethod -Method Get -Uri "$api/branches/main" -Headers $headers

  $response=Invoke-RestMethod -Method Get -Uri "$api/actions/runs?head_sha=$head&per_page=100" -Headers $headers

 } catch {throw 'Authenticated GitHub GET failed; credentials/response details suppressed'}

 $response | ConvertTo-Json -Depth 40 | Set-Content "$out/runs-$stamp.json"

 $branch | ConvertTo-Json -Depth 25 | Set-Content "$out/branch-$stamp.json"

 $runs=@($response.workflow_runs | Where-Object {$_.head_sha -eq $head})

 $mapped=@()

 foreach ($role in @(@{Kind='ci';Path='.github/workflows/validate.yml';Event='push'},@{Kind='cleanup';Path='.github/workflows/recursive-cleanup.yml';Event='workflow_dispatch'})) {

  $match=@($runs | Where-Object {$_.path -eq $role.Path -and $_.event -eq $role.Event})

  if ($match.Count -ne 1) {throw ('Expected exactly one matching run for '+$role.Kind+'; snapshot retained')}

  $expectedRuns=@{}; if (!$Initial) { $initialMap=Get-Content "$out/mapped-runs.json" -Raw | ConvertFrom-Json; foreach($entry in $initialMap.Runs){$expectedRuns[$entry.Kind]=$entry.RunId} }
  $run=$match[0];$attempt=$null;$result=$null
  if(!$Initial -and $run.id -ne $expectedRuns[$role.Kind]){throw 'Exact initial-mapped330 run ID differs'}

  if($run.run_attempt -ne 1 -or $run.head_branch -cne 'main'){throw 'Exact first-attempt main run required'}

  if ($role.Kind -ne 'ci') {

   $attempt=Get-Content "artifacts/part330-recursive-cleanup-dispatch-attempt.json" -Raw | ConvertFrom-Json

   $result=Get-Content "artifacts/part330-recursive-cleanup-dispatch-result.json" -Raw | ConvertFrom-Json

   if ($attempt.Subject -ne 'd83ed875d0c77939e20049afcb46fa297fee82dd' -or $attempt.Body.inputs.subject_ref -ne $attempt.Subject -or $result.Subject -ne $attempt.Subject -or $attempt.Head -ne $head -or $result.Head -ne $head -or $run.workflow_id -ne $attempt.Workflow -or [DateTimeOffset]$run.created_at -lt [DateTimeOffset]$attempt.Time -or [DateTimeOffset]$run.created_at -gt ([DateTimeOffset]$result.Time).AddMinutes(2)) {throw 'Run/dispatch temporal binding mismatch'}

  }

  $jobSnapshot=Invoke-RestMethod -Method Get -Uri "$api/actions/runs/$($run.id)/attempts/1/jobs?per_page=100" -Headers $headers

  $jobSnapshot | ConvertTo-Json -Depth 40 | Set-Content "$out/$($role.Kind)-jobs-$stamp.json"

  $mapped+=@{Kind=$role.Kind;RunId=$run.id;WorkflowId=$run.workflow_id;Path=$run.path;Event=$run.event;Head=$run.head_sha;HeadBranch=$run.head_branch;RunAttempt=$run.run_attempt;CreatedAt=$run.created_at;UpdatedAt=$run.updated_at;Status=$run.status;Conclusion=$run.conclusion;HtmlUrl=$run.html_url;DispatchAttemptTime=$(if($attempt){$attempt.Time}else{$null});DispatchAcceptedTime=$(if($result){$result.Time}else{$null})}

 }

 $report=@{Status='EXACT_HEAD_TWO_RUNS_MAPPED_TERMINAL_PROOF_NOT_YET_AUDITED';PublishedHead=$head;ObservedMain=$branch.commit.sha;CurrentMainExact=($branch.commit.sha -eq $head);ObservedUtc=[DateTime]::UtcNow.ToString('O');Runs=$mapped;PostExecuted=$false;MainEdited=$false;GitMutation=$false;OriginalResultsAudited=$false;ArtifactsAudited=$false}

 $report | ConvertTo-Json -Depth 30 | Set-Content "$out/status-$stamp.json"

 if ($Initial) {

  if(Test-Path "$out/mapped-runs.json"){throw 'Initial mapping already exists'}

  $report | ConvertTo-Json -Depth 30 | Set-Content "$out/mapped-runs.json"

 }

 $mapped | Select-Object Kind,RunId,Status,Conclusion,RunAttempt | ConvertTo-Json

 Write-Output ('CurrentMainExact='+$report.CurrentMainExact)

} finally {$secret=$null;$headers=$null;$passwordLine=$null;$credentialLines=$null}

