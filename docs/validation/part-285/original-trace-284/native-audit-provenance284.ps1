$ErrorActionPreference='Stop'
$head='d45e62732d1cd101fc2ec7dfc1c5014942a6e2d0'
$credentialLines="protocol=https`nhost=github.com`n`n"|git credential fill
if($LASTEXITCODE){throw 'Credential provider failed'}
$secret=($credentialLines|Where-Object {$_ -like 'password=*'}|Select-Object -First 1).Substring(9)
$headers=@{'User-Agent'='Erlang.Net-reference-provenance';Authorization="Bearer $secret";Accept='application/vnd.github+json'}
try{
 foreach($item in @(@(38669679953,116209593073,'native269-trace'),@(38669631998,116209429742,'native269-ci'))){
  $run=Invoke-RestMethod -Uri "https://api.github.com/repos/develmax/Erlang.Net/actions/runs/$($item[0])" -Headers $headers
  if($run.head_sha -cne $head){throw 'Run head differs from frozen implementation'}
  $job=Invoke-RestMethod -Uri "https://api.github.com/repos/develmax/Erlang.Net/actions/jobs/$($item[1])" -Headers $headers
  [pscustomobject]@{Run=$run;Job=$job}|ConvertTo-Json -Depth 20|Set-Content "artifacts/part-284-$($item[2])-latest-provenance.json"
  [pscustomobject]@{Run=$run.id;Status=$run.status;Conclusion=$run.conclusion;Job=$job.id;RunnerLabels=$job.labels;StepsAvailable=@($job.steps).Count}|ConvertTo-Json -Depth 5
  if($run.status -eq 'completed'){
   $path="artifacts/part-284-$($item[2])-job.log"
   if(!(Test-Path -LiteralPath $path)){
    Invoke-WebRequest -Uri "https://api.github.com/repos/develmax/Erlang.Net/actions/jobs/$($item[1])/logs" -Headers $headers -OutFile $path
    [pscustomobject]@{Path=$path;SHA256=(Get-FileHash -LiteralPath $path).Hash}|ConvertTo-Json|Set-Content "$path.digest.json"
   }
  }
 }
}finally{$secret=$null;$credentialLines=$null;$headers=$null}
