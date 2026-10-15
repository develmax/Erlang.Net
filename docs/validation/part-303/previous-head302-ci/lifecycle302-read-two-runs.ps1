param([Parameter(Mandatory=$true)][ValidatePattern('^[0-9a-f]{40}$')][string]$PublishedHead)
$ErrorActionPreference='Stop'
if($PublishedHead -cne 'ce3636a9d17a4d1e1c979c57b4df3d0800b72134'){throw 'Only published302 authority is accepted'}
$publishedReceiptText=git cat-file blob ($PublishedHead+':docs/validation/part-302/evidence-receipt.json')
if($LASTEXITCODE){throw 'Part302 exact-head published receipt absent; refuse before GET'}
$publishedReceipt=$publishedReceiptText|ConvertFrom-Json
if($publishedReceipt.Reused301FullPassed -ne 6621 -or $publishedReceipt.RegisteredMfas -ne 503 -or $publishedReceipt.ErlangMfas -ne 345 -or $publishedReceipt.ExactExecutionInputs -ne 18 -or $publishedReceipt.ReadinessRegistryQueryVerified -ne $true -or $publishedReceipt.ExecutionPlanSha256 -cne '97C447A8AD78D37EA4F092963B7D4E5A7FEF5879E55DF1E4E4739F1409D5FBA9'){throw 'Published302 tooling authority differs; refuse before GET'}
$lastPath='artifacts/lifecycle302-last-read.json'
if(Test-Path -LiteralPath $lastPath){$previous=Get-Content -LiteralPath $lastPath -Raw|ConvertFrom-Json;if(([DateTime]::UtcNow-[DateTime]$previous.ReadUtc).TotalSeconds -lt 60){throw 'Read-only backoff requires at least 60 seconds'}}
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
 $targets=@($runs.workflow_runs|Where-Object {$_.name -cin @('Validate CLR implementation','Pinned raw on-load visibility original observations')})
 if(@($targets.name|Select-Object -Unique).Count -ne 2 -or $targets.Count -ne 2){throw 'Expected exactly two exact-head named runs; inspect snapshot rather than pick a run'}
 $stamp=[DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffffffZ')
 foreach($run in $targets){
  if($run.head_sha -cne $PublishedHead -or $run.run_attempt -ne 1){throw 'CI head or attempt differs'}
  $jobs=Invoke-RestMethod -Uri "$($run.jobs_url)?per_page=100" -Headers $headers
  [pscustomobject]@{Run=$run;Jobs=$jobs;ReadOnly=$true;FetchedUtc=$stamp}|ConvertTo-Json -Depth 35|Set-Content -LiteralPath "artifacts/lifecycle302-watch-$($run.id)-$stamp.json"
  [pscustomobject]@{Run=$run.id;Head=$run.head_sha;Status=$run.status;Conclusion=$run.conclusion;Jobs=@($jobs.jobs|ForEach-Object {[pscustomobject]@{Id=$_.id;Status=$_.status;Conclusion=$_.conclusion;Active=@($_.steps|Where-Object status -eq 'in_progress'|ForEach-Object name)}})}|ConvertTo-Json -Depth 7
  if($run.status -ne 'completed'){continue}
  $dest="artifacts/lifecycle302-watch-$($run.id)-terminal"
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
   & py -3 artifacts/lifecycle302-safe-extract.py $zip "$dest/artifact-$($item.id)"
   if($LASTEXITCODE){throw 'Bounded safe extraction failed; raw ZIP retained'}
  }
  Get-ChildItem -LiteralPath $dest -Recurse -File|ForEach-Object {[pscustomobject]@{Path=[IO.Path]::GetRelativePath((Resolve-Path -LiteralPath $dest).Path,$_.FullName);Bytes=$_.Length;Sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash}}|ConvertTo-Json -Depth 5|Set-Content -LiteralPath "$dest/download-manifest.json"
  Write-Output "Read-only downloaded terminal302 run $($run.id)"
 }
}finally{$secret=$null;$passwordLine=$null;$credentialLines=$null;$headers=$null}