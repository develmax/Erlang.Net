$ErrorActionPreference='Stop'
$head='3cf595fe194e80830773f2ac80ac2a1174c944bd'
$credentialLines="protocol=https`nhost=github.com`n`n"|git credential fill
if($LASTEXITCODE){throw 'Credential provider failed'}
$secret=($credentialLines|Where-Object {$_ -like 'password=*'}|Select-Object -First 1).Substring(9)
$headers=@{'User-Agent'='Erlang.Net-native269-CI281-readonly';Authorization="Bearer $secret";Accept='application/vnd.github+json'}
try{
 $runs=Invoke-RestMethod -Uri 'https://api.github.com/repos/develmax/Erlang.Net/actions/workflows/validate.yml/runs?per_page=30' -Headers $headers
 $matches=@($runs.workflow_runs|Where-Object {$_.head_sha -ceq $head})
 if($matches.Count -ne 1){throw "Expected one sameheadCI281 run; actual $($matches.Count)"}
 $run=$matches[0]
 $jobs=Invoke-RestMethod -Uri "https://api.github.com/repos/develmax/Erlang.Net/actions/runs/$($run.id)/jobs" -Headers $headers
 [pscustomobject]@{Run=$run;Jobs=$jobs.jobs}|ConvertTo-Json -Depth 30|Set-Content artifacts/native269-part281-ci-status.json
 [pscustomobject]@{Head=$head;Run=$run.id;Status=$run.status;Conclusion=$run.conclusion;Jobs=@($jobs.jobs|Select-Object id,status,conclusion)}|ConvertTo-Json -Depth 5
 if($run.status -eq 'completed' -and $run.conclusion -eq 'success'){
  if(@($jobs.jobs).Count -ne 1){throw 'Unexpected CI job count'}
  $job=$jobs.jobs[0]
  $path='artifacts/part-281-native269-ci-job.log'
  if(!(Test-Path -LiteralPath $path)){Invoke-WebRequest -Uri "https://api.github.com/repos/develmax/Erlang.Net/actions/jobs/$($job.id)/logs" -Headers $headers -OutFile $path;[pscustomobject]@{Path=$path;SHA256=(Get-FileHash -LiteralPath $path).Hash}|ConvertTo-Json|Set-Content "$path.digest.json"}
  [pscustomobject]@{Run=$run;Job=$job}|ConvertTo-Json -Depth 30|Set-Content artifacts/part-281-native269-ci-latest-provenance.json
  $ars=Invoke-RestMethod -Uri "https://api.github.com/repos/develmax/Erlang.Net/actions/runs/$($run.id)/artifacts" -Headers $headers
  $artifacts=@($ars.artifacts|Where-Object {$_.name -eq 'validation'})
  if($artifacts.Count -ne 1){throw 'Unexpected validation artifact count'}
  $ar=$artifacts[0];$zip='artifacts/part-281-native269-validation.zip'
  if(!(Test-Path -LiteralPath $zip)){Invoke-WebRequest -Uri "https://api.github.com/repos/develmax/Erlang.Net/actions/artifacts/$($ar.id)/zip" -Headers $headers -OutFile $zip}
  $digest=(Get-FileHash -LiteralPath $zip).Hash
  if($ar.digest -cne "sha256:$($digest.ToLowerInvariant())"){throw 'Artifact SHA mismatch'}
  [pscustomobject]@{Run=$run;Artifact=$ar;ZipSHA256=$digest}|ConvertTo-Json -Depth 30|Set-Content artifacts/part-281-native269-validation-metadata.json
  if(!(Test-Path -LiteralPath artifacts/part-281-native269-validation)){Expand-Archive -LiteralPath $zip -DestinationPath artifacts/part-281-native269-validation}
  Write-Output "Downloaded CI281 artifact $($ar.id)"
 }
}finally{$secret=$null;$credentialLines=$null;$headers=$null}
