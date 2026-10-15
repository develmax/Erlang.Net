$ErrorActionPreference='Stop'
$credentialLines="protocol=https`nhost=github.com`n`n"|git credential fill
if($LASTEXITCODE){throw 'Credential provider failed'}
$passwordLine=$credentialLines|Where-Object {$_ -like 'password=*'}|Select-Object -First 1
if($null -eq $passwordLine){throw 'Credential provider did not return a token'}
$secret=$passwordLine.Substring(9)
$headers=@{'User-Agent'='Erlang.Net-read-only-independent-audit';Authorization="Bearer $secret";Accept='application/vnd.github+json'}
try {
 $repo='https://api.github.com/repos/develmax/Erlang.Net'
 $targets=@()
 $old=Invoke-RestMethod -Uri "$repo/actions/runs/38675406093" -Headers $headers
 if($old.head_sha -cne '0267a3cebb7a03e849d7d023dfbd8cda31e708a1'){throw 'Old286 original run head differs'}
 $targets+=$old
 $runs=Invoke-RestMethod -Uri "$repo/actions/runs?head_sha=58d227ecef29d117fa091ab30b1e0928f969d2fa&per_page=30" -Headers $headers
 $targets+=@($runs.workflow_runs|Where-Object {$_.name -in @('Validate CLR implementation','Pinned OTP deferred payload exit observations','Pinned OTP deferred payload monitor observations')})
 $stamp=[DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffffffZ')
 foreach($run in $targets){
  if($run.id -ne 38675406093 -and $run.head_sha -cne '58d227ecef29d117fa091ab30b1e0928f969d2fa'){throw '287 run head differs'}
  $jobs=Invoke-RestMethod -Uri "$($run.jobs_url)?per_page=100" -Headers $headers
  $snapshot="artifacts/native-audit-run-$($run.id)-$stamp.json"
  [pscustomobject]@{Run=$run;Jobs=$jobs;ReadOnly=$true;FetchedUtc=$stamp}|ConvertTo-Json -Depth 35|Set-Content -LiteralPath $snapshot
  [pscustomobject]@{Run=$run.id;Name=$run.name;Head=$run.head_sha;Status=$run.status;Conclusion=$run.conclusion;Jobs=@($jobs.jobs|ForEach-Object {[pscustomobject]@{Id=$_.id;Status=$_.status;Conclusion=$_.conclusion;Active=@($_.steps|Where-Object status -eq 'in_progress'|ForEach-Object name)}})}|ConvertTo-Json -Depth 7
  if($run.status -ne 'completed'){continue}
  $dest="artifacts/native-audit-run-$($run.id)-terminal"
  if(Test-Path -LiteralPath $dest){
   if(!(Test-Path -LiteralPath "$dest/download-manifest.json")){throw 'Partial terminal download exists; inspect rather than overwrite'}
   continue
  }
  New-Item -ItemType Directory -Path $dest|Out-Null
  $run|ConvertTo-Json -Depth 35|Set-Content -LiteralPath "$dest/run.json"
  $jobs|ConvertTo-Json -Depth 35|Set-Content -LiteralPath "$dest/jobs.json"
  $artifacts=Invoke-RestMethod -Uri $run.artifacts_url -Headers $headers
  $artifacts|ConvertTo-Json -Depth 35|Set-Content -LiteralPath "$dest/artifacts.json"
  foreach($job in $jobs.jobs){
   Invoke-WebRequest -Uri "$repo/actions/jobs/$($job.id)/logs" -Headers $headers -OutFile "$dest/job-$($job.id).log"
  }
  foreach($item in $artifacts.artifacts){
   if($item.expired -or $item.workflow_run.head_sha -cne $run.head_sha){throw 'Expired artifact or source head mismatch'}
   $zip="$dest/artifact-$($item.id).zip"
   Invoke-WebRequest -Uri $item.archive_download_url -Headers $headers -OutFile $zip
   $zipHash=(Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash
   if((Get-Item -LiteralPath $zip).Length -ne $item.size_in_bytes -or $item.digest -cne "sha256:$($zipHash.ToLowerInvariant())"){throw 'Downloaded ZIP size/digest mismatch; preserve raw bytes'}
   Expand-Archive -LiteralPath $zip -DestinationPath "$dest/artifact-$($item.id)"
  }
  Get-ChildItem -LiteralPath $dest -Recurse -File|ForEach-Object {[pscustomobject]@{Path=[IO.Path]::GetRelativePath((Resolve-Path -LiteralPath $dest).Path,$_.FullName);Bytes=$_.Length;Sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash}}|ConvertTo-Json -Depth 5|Set-Content -LiteralPath "$dest/download-manifest.json"
  Write-Output "Read-only downloaded terminal run $($run.id)"
 }
}finally{$secret=$null;$passwordLine=$null;$credentialLines=$null;$headers=$null}
