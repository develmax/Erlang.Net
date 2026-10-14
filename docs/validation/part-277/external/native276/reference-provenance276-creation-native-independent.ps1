$ErrorActionPreference='Stop'
$head='54c9ff90ee2a924792467289a28629e5e02be661'
$credentialLines="protocol=https`nhost=github.com`n`n"|git credential fill
if($LASTEXITCODE){throw 'Credential provider failed'}
$secret=($credentialLines|Where-Object {$_ -like 'password=*'}|Select-Object -First 1).Substring(9)
$headers=@{'User-Agent'='Erlang.Net-reference-provenance';Authorization="Bearer $secret";Accept='application/vnd.github+json'}
try{
 foreach($item in @(,@(38647831713,116139005760,'creation-native'))){
  $run=Invoke-RestMethod -Uri "https://api.github.com/repos/develmax/Erlang.Net/actions/runs/$($item[0])" -Headers $headers
  if($run.head_sha -cne $head){throw 'Run head differs from frozen implementation'}
  $job=Invoke-RestMethod -Uri "https://api.github.com/repos/develmax/Erlang.Net/actions/jobs/$($item[1])" -Headers $headers
  [pscustomobject]@{Run=$run;Job=$job}|ConvertTo-Json -Depth 20|Set-Content "artifacts/part-276-$($item[2])-latest-provenance.json"
  [pscustomobject]@{Run=$run.id;Status=$run.status;Conclusion=$run.conclusion;Job=$job.id;RunnerLabels=$job.labels;StepsAvailable=@($job.steps).Count}|ConvertTo-Json -Depth 5
  if($run.status -eq 'completed'){
   $path="artifacts/part-276-$($item[2])-job.log"
   if(!(Test-Path -LiteralPath $path)){
    Invoke-WebRequest -Uri "https://api.github.com/repos/develmax/Erlang.Net/actions/jobs/$($item[1])/logs" -Headers $headers -OutFile $path
    [pscustomobject]@{Path=$path;SHA256=(Get-FileHash -LiteralPath $path).Hash}|ConvertTo-Json|Set-Content "$path.digest.json"
   }
  }
 }
}finally{$secret=$null;$credentialLines=$null;$headers=$null}
