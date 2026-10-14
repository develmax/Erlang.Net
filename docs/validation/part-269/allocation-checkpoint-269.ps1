param([ValidateSet('Dispatch','Status','Download')][string]$Mode='Status')
$ErrorActionPreference='Stop'
if ($Mode -eq 'Dispatch') {
  $candidateHead=(git rev-parse HEAD).Trim()
  if ($LASTEXITCODE -ne 0 -or $candidateHead -eq '3a7ab4a6ac3adeee2242545a25ad2ace27efa8e7') { throw 'Part269 must be committed and published before dispatch' }
  foreach($path in @('tools/Erlang.AllocationDiagnostic/Program.cs','tools/Erlang.AllocationDiagnostic/Run-DiagnosticProcess.ps1','.github/workflows/allocation-diagnostic.yml')) {
    git cat-file -e "$($candidateHead):$path"
    if($LASTEXITCODE -ne 0){throw 'Committed269 source/evidence is incomplete'}
  }
  if(Test-Path -LiteralPath 'artifacts/part-269-allocation-frozen-head.txt') {
    if([IO.File]::ReadAllText('artifacts/part-269-allocation-frozen-head.txt').Trim() -cne $candidateHead){throw 'Never replace the frozen269 head'}
  }
}
$head=if($Mode -eq 'Dispatch'){$candidateHead}else{[IO.File]::ReadAllText('artifacts/part-269-allocation-frozen-head.txt').Trim()}
if ($head -notmatch '^[0-9a-f]{40}$') { throw 'Unexpected implementation head' }
$credentialLines="protocol=https`nhost=github.com`n`n" | git credential fill
if($LASTEXITCODE -ne 0){throw 'Credential provider failed'}
$secret=($credentialLines | Where-Object {$_ -like 'password=*'} | Select-Object -First 1).Substring(9)
$headers=@{'User-Agent'='Erlang.Net-development-validation';Authorization="Bearer $secret";Accept='application/vnd.github+json'}
try {
  if ($Mode -eq 'Dispatch') {
    $branch=Invoke-RestMethod -Uri 'https://api.github.com/repos/develmax/Erlang.Net/branches/main' -Headers $headers
    if($branch.commit.sha -cne $head){throw 'Published main no longer matches frozen dispatch head'}
    if(!(Test-Path -LiteralPath 'artifacts/part-269-allocation-frozen-head.txt')){[IO.File]::WriteAllText('artifacts/part-269-allocation-frozen-head.txt',$head+[char]10)}
    Invoke-RestMethod -Method Post -Uri 'https://api.github.com/repos/develmax/Erlang.Net/actions/workflows/allocation-diagnostic.yml/dispatches' -Headers $headers -ContentType 'application/json' -Body '{"ref":"main"}' | Out-Null
    Write-Output "Dispatched separate Windows allocation diagnostic workflow for $head"
  }
  $runs=Invoke-RestMethod -Uri "https://api.github.com/repos/develmax/Erlang.Net/actions/runs?head_sha=$head&per_page=10" -Headers $headers
  foreach ($run in @($runs.workflow_runs | Where-Object name -eq 'Windows hash allocation diagnostics')) {
    if($run.head_sha -ne $head){throw 'Run head differs'}
    $jobs=Invoke-RestMethod -Uri "$($run.url)/jobs" -Headers $headers
    [pscustomobject]@{Run=$run.id;Name=$run.name;Status=$run.status;Conclusion=$run.conclusion;Head=$run.head_sha;Jobs=@($jobs.jobs | ForEach-Object {[pscustomobject]@{Id=$_.id;Status=$_.status;Conclusion=$_.conclusion;Active=@($_.steps | Where-Object status -eq 'in_progress' | ForEach-Object name)}})} | ConvertTo-Json -Depth 6
    if($Mode -eq 'Download' -and $run.status -eq 'completed') {
      $artifactName='windows-hash-allocation-diagnostics'
      $metadataPath="artifacts/part-269-allocation-$artifactName-metadata.json"
      if(Test-Path -LiteralPath $metadataPath){continue}
      $artifacts=Invoke-RestMethod -Uri "$($run.url)/artifacts" -Headers $headers
      $artifact=@($artifacts.artifacts | Where-Object name -eq $artifactName)
      if($artifact.Count -ne 1 -or $artifact[0].expired){throw 'Missing/expired artifact'}
      $zip="artifacts/part-269-allocation-$artifactName.zip"
      Invoke-WebRequest -Uri $artifact[0].archive_download_url -Headers $headers -OutFile $zip
      Expand-Archive -LiteralPath $zip -DestinationPath "artifacts/part-269-allocation-$artifactName" -Force
      [pscustomobject]@{Run=$run;Jobs=$jobs.jobs;Artifact=$artifact[0];ZipSHA256=(Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash} | ConvertTo-Json -Depth 16 | Set-Content $metadataPath
      Write-Output "Downloaded $artifactName artifact $($artifact[0].id)"
    }
  }
} finally { $secret=$null; $credentialLines=$null; $headers=$null }
