param([ValidateSet('Dispatch','Status','Download')][string]$Mode='Status')
$ErrorActionPreference='Stop'
$head=[IO.File]::ReadAllText('artifacts/part-260-frozen-head.txt').Trim()
if ($head -notmatch '^[0-9a-f]{40}$') { throw 'Unexpected implementation head' }
$credentialLines="protocol=https`nhost=github.com`n`n" | git credential fill
if($LASTEXITCODE -ne 0){throw 'Credential provider failed'}
$secret=($credentialLines | Where-Object {$_ -like 'password=*'} | Select-Object -First 1).Substring(9)
$headers=@{'User-Agent'='Erlang.Net-development-validation';Authorization="Bearer $secret";Accept='application/vnd.github+json'}
try {
  if ($Mode -eq 'Dispatch') {
    $branch=Invoke-RestMethod -Uri 'https://api.github.com/repos/develmax/Erlang.Net/branches/main' -Headers $headers
    if($branch.commit.sha -cne $head){throw 'Published main no longer matches frozen dispatch head'}
    Invoke-RestMethod -Method Post -Uri 'https://api.github.com/repos/develmax/Erlang.Net/actions/workflows/loader-contrasts.yml/dispatches' -Headers $headers -ContentType 'application/json' -Body '{"ref":"main"}' | Out-Null
    Write-Output "Dispatched pinned workflow for $head"
  }
  $runs=Invoke-RestMethod -Uri "https://api.github.com/repos/develmax/Erlang.Net/actions/runs?head_sha=$head&per_page=10" -Headers $headers
  foreach ($run in $runs.workflow_runs) {
    if($run.head_sha -ne $head){throw 'Run head differs'}
    $jobs=Invoke-RestMethod -Uri "$($run.url)/jobs" -Headers $headers
    [pscustomobject]@{Run=$run.id;Name=$run.name;Status=$run.status;Conclusion=$run.conclusion;Head=$run.head_sha;Jobs=@($jobs.jobs | ForEach-Object {[pscustomobject]@{Id=$_.id;Status=$_.status;Conclusion=$_.conclusion;Active=@($_.steps | Where-Object status -eq 'in_progress' | ForEach-Object name)}})} | ConvertTo-Json -Depth 6
    if($Mode -eq 'Download' -and $run.status -eq 'completed') {
      $artifactName=if($run.name -eq 'Pinned OTP differential oracle'){'differential'}elseif($run.name -eq 'Validate CLR implementation'){'validation'}elseif($run.name -eq 'Pinned OTP BEAM fixtures'){'beam-fixtures'}elseif($run.name -eq 'Pinned OTP halt raw subprocess track'){'halt-native-raw'}elseif($run.name -eq 'Pinned OTP loader contrasts'){'loader-contrasts'}else{throw 'Unknown workflow'}
      $metadataPath="artifacts/part-260-$artifactName-metadata.json"
      if(Test-Path -LiteralPath $metadataPath){continue}
      $artifacts=Invoke-RestMethod -Uri "$($run.url)/artifacts" -Headers $headers
      $artifact=@($artifacts.artifacts | Where-Object name -eq $artifactName)
      if($artifact.Count -ne 1 -or $artifact[0].expired){throw 'Missing/expired artifact'}
      $zip="artifacts/part-260-$artifactName.zip"
      Invoke-WebRequest -Uri $artifact[0].archive_download_url -Headers $headers -OutFile $zip
      Expand-Archive -LiteralPath $zip -DestinationPath "artifacts/part-260-$artifactName" -Force
      [pscustomobject]@{Run=$run;Jobs=$jobs.jobs;Artifact=$artifact[0];ZipSHA256=(Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash} | ConvertTo-Json -Depth 16 | Set-Content $metadataPath
      Write-Output "Downloaded $artifactName artifact $($artifact[0].id)"
    }
  }
} finally { $secret=$null; $credentialLines=$null; $headers=$null }
