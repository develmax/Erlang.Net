param([Parameter(Mandatory=$true)][ValidatePattern('^[0-9a-f]{40}$')][string]$PublishedHead)
$ErrorActionPreference='Stop'
 $lastPath='artifacts/spawn-protocol-ci290-last-read.json'
 if(Test-Path -LiteralPath $lastPath){$previous=Get-Content -LiteralPath $lastPath -Raw|ConvertFrom-Json;if(([DateTime]::UtcNow-[DateTime]$previous.ReadUtc).TotalSeconds -lt 60){throw 'Read-only polling backoff requires at least60seconds'}}
 [pscustomobject]@{ReadUtc=[DateTime]::UtcNow.ToString('o');Head=$PublishedHead}|ConvertTo-Json|Set-Content -LiteralPath $lastPath
$credentialLines="protocol=https`nhost=github.com`n`n"|git credential fill
if($LASTEXITCODE){throw 'Credential provider failed'}
$passwordLine=$credentialLines|Where-Object {$_ -like 'password=*'}|Select-Object -First 1
if($null -eq $passwordLine){throw 'Credential provider did not return a token'}
$secret=$passwordLine.Substring(9)
$headers=@{'User-Agent'='Erlang.Net-read-only-independent-audit';Authorization="Bearer $secret";Accept='application/vnd.github+json'}
try {
 $repo='https://api.github.com/repos/develmax/Erlang.Net'
 $runs=Invoke-RestMethod -Uri "$repo/actions/runs?head_sha=$PublishedHead&per_page=100" -Headers $headers
 $targets=@($runs.workflow_runs|Where-Object {$_.name -ceq 'Validate CLR implementation' -or $_.name -ceq 'Pinned OTP NIF fixture observations'})
 if($targets.Count -ne 2){throw 'Expected exactly two exact-head CI/NIF runs; preserve selection failure without dispatch'}
 $stamp=[DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffffffZ')
 foreach($run in $targets){
  $kind=if($run.name -ceq 'Validate CLR implementation'){'ci'}else{'nif'}
  if($run.head_sha -cne $PublishedHead -or $run.run_attempt -ne 1){throw 'CI head or attempt differs'}
  $jobs=Invoke-RestMethod -Uri "$($run.jobs_url)?per_page=100" -Headers $headers
  [pscustomobject]@{Run=$run;Jobs=$jobs;ReadOnly=$true;FetchedUtc=$stamp}|ConvertTo-Json -Depth 35|Set-Content -LiteralPath "artifacts/spawn-protocol-$kind-290-$($run.id)-$stamp.json"
  [pscustomobject]@{Run=$run.id;Head=$run.head_sha;Status=$run.status;Conclusion=$run.conclusion;Jobs=@($jobs.jobs|ForEach-Object {[pscustomobject]@{Id=$_.id;Status=$_.status;Conclusion=$_.conclusion;Active=@($_.steps|Where-Object status -eq 'in_progress'|ForEach-Object name)}})}|ConvertTo-Json -Depth 7
  if($kind -ceq 'nif' -and $run.status -ne 'completed'){
   $partial="artifacts/spawn-protocol-nif-290-$($run.id)-partial-$stamp"
   $terminalJobs=@($jobs.jobs|Where-Object status -eq 'completed')
   $havePartial=@(Get-ChildItem -LiteralPath artifacts -Directory -Filter "spawn-protocol-nif-290-$($run.id)-partial-*"|Where-Object {Test-Path -LiteralPath (Join-Path $_.FullName 'download-manifest.json')}).Count -gt 0
   if($terminalJobs.Count -gt 0 -and !$havePartial){
    New-Item -ItemType Directory -Path $partial|Out-Null
    $run|ConvertTo-Json -Depth 35|Set-Content -LiteralPath "$partial/run.json"
    $jobs|ConvertTo-Json -Depth 35|Set-Content -LiteralPath "$partial/jobs.json"
    $partialArtifacts=Invoke-RestMethod -Uri $run.artifacts_url -Headers $headers
    $partialArtifacts|ConvertTo-Json -Depth 35|Set-Content -LiteralPath "$partial/artifacts.json"
    foreach($job in $terminalJobs){Invoke-WebRequest -Uri "$repo/actions/jobs/$($job.id)/logs" -Headers $headers -OutFile "$partial/job-$($job.id).log"}
    foreach($item in $partialArtifacts.artifacts){
     if($item.expired -or $item.workflow_run.head_sha -cne $PublishedHead){throw 'Partial artifact source mismatch'}
     $zip="$partial/artifact-$($item.id).zip";Invoke-WebRequest -Uri $item.archive_download_url -Headers $headers -OutFile $zip
     $digest=(Get-FileHash -LiteralPath $zip).Hash
     if((Get-Item -LiteralPath $zip).Length -ne $item.size_in_bytes -or $item.digest -cne "sha256:$($digest.ToLowerInvariant())"){throw 'Partial ZIP digest mismatch'}
     Expand-Archive -LiteralPath $zip -DestinationPath "$partial/artifact-$($item.id)"
    }
    Get-ChildItem -LiteralPath $partial -Recurse -File|ForEach-Object {[pscustomobject]@{Path=[IO.Path]::GetRelativePath((Resolve-Path -LiteralPath $partial).Path,$_.FullName);Bytes=$_.Length;Sha256=(Get-FileHash -LiteralPath $_.FullName).Hash}}|ConvertTo-Json -Depth 5|Set-Content -LiteralPath "$partial/download-manifest.json"
   }
  }
  if($run.status -ne 'completed'){continue}
  $dest="artifacts/spawn-protocol-$kind-290-$($run.id)-terminal"
  if(Test-Path -LiteralPath $dest){if(!(Test-Path -LiteralPath "$dest/download-manifest.json")){throw 'Partial terminal download exists; preserve and inspect'};continue}
  New-Item -ItemType Directory -Path $dest|Out-Null
  $run|ConvertTo-Json -Depth 35|Set-Content -LiteralPath "$dest/run.json"
  $jobs|ConvertTo-Json -Depth 35|Set-Content -LiteralPath "$dest/jobs.json"
  $artifacts=Invoke-RestMethod -Uri $run.artifacts_url -Headers $headers
  $artifacts|ConvertTo-Json -Depth 35|Set-Content -LiteralPath "$dest/artifacts.json"
  foreach($job in $jobs.jobs){Invoke-WebRequest -Uri "$repo/actions/jobs/$($job.id)/logs" -Headers $headers -OutFile "$dest/job-$($job.id).log"}
  foreach($item in $artifacts.artifacts){
   if($item.expired -or $item.workflow_run.head_sha -cne $PublishedHead){throw 'Artifact expired or source head differs'}
   $zip="$dest/artifact-$($item.id).zip"
   Invoke-WebRequest -Uri $item.archive_download_url -Headers $headers -OutFile $zip
   $zipHash=(Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash
   if((Get-Item -LiteralPath $zip).Length -ne $item.size_in_bytes -or $item.digest -cne "sha256:$($zipHash.ToLowerInvariant())"){throw 'ZIP digest/length mismatch; preserve raw bytes'}
   Expand-Archive -LiteralPath $zip -DestinationPath "$dest/artifact-$($item.id)"
  }
  Get-ChildItem -LiteralPath $dest -Recurse -File|ForEach-Object {[pscustomobject]@{Path=[IO.Path]::GetRelativePath((Resolve-Path -LiteralPath $dest).Path,$_.FullName);Bytes=$_.Length;Sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash}}|ConvertTo-Json -Depth 5|Set-Content -LiteralPath "$dest/download-manifest.json"
  Write-Output "Read-only downloaded terminal exact-head run $($run.id)"
 }
}finally{$secret=$null;$passwordLine=$null;$credentialLines=$null;$headers=$null}