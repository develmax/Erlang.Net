$ErrorActionPreference='Stop'
$head='d7f10238d7f35146268617128729052e1af53c44'
$credentialLines="protocol=https`nhost=github.com`n`n"|git credential fill
if($LASTEXITCODE){throw 'Credential provider failed'}
$secret=($credentialLines|Where-Object {$_ -like 'password=*'}|Select-Object -First 1).Substring(9)
$headers=@{'User-Agent'='Erlang.Net-reference-provenance';Authorization="Bearer $secret";Accept='application/vnd.github+json'}
try{
 foreach($item in @(,@(38654983784,116162502397,'native269-queue'))){
  $run=Invoke-RestMethod -Uri "https://api.github.com/repos/develmax/Erlang.Net/actions/runs/$($item[0])" -Headers $headers
  if($run.head_sha -cne $head){throw 'Run head differs from frozen implementation'}
  $job=Invoke-RestMethod -Uri "https://api.github.com/repos/develmax/Erlang.Net/actions/jobs/$($item[1])" -Headers $headers
  [pscustomobject]@{Run=$run;Job=$job}|ConvertTo-Json -Depth 20|Set-Content "artifacts/part-278-$($item[2])-latest-provenance.json"
  [pscustomobject]@{Run=$run.id;Status=$run.status;Conclusion=$run.conclusion;Job=$job.id;RunnerLabels=$job.labels;StepsAvailable=@($job.steps).Count}|ConvertTo-Json -Depth 5
  if($run.status -eq 'completed'){
   $path="artifacts/part-278-$($item[2])-job.log"
   if(!(Test-Path -LiteralPath $path)){
    Invoke-WebRequest -Uri "https://api.github.com/repos/develmax/Erlang.Net/actions/jobs/$($item[1])/logs" -Headers $headers -OutFile $path
    [pscustomobject]@{Path=$path;SHA256=(Get-FileHash -LiteralPath $path).Hash}|ConvertTo-Json|Set-Content "$path.digest.json"
   }
  }
 }
}finally{$secret=$null;$credentialLines=$null;$headers=$null}
