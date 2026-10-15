$ErrorActionPreference='Stop'
$previousProgress=Get-ChildItem -LiteralPath artifacts/part312-readonly-monitor -Filter 'ordinary-jobs-*.json' | Sort-Object Name | Select-Object -Last 1
if($previousProgress -and ([DateTime]::UtcNow-$previousProgress.LastWriteTimeUtc).TotalSeconds -lt 120){throw 'Ordinary job GET spacing requires at least120seconds'}
$credentialLines=$null;$passwordLine=$null;$secret=$null;$headers=$null
try {
 $credentialLines="protocol=https`nhost=github.com`n`n" | git credential fill
 if($LASTEXITCODE -ne 0){throw 'Credential helper failed; details suppressed'}
 $passwordLine=$credentialLines | Where-Object {$_ -like 'password=*'} | Select-Object -First 1
 if(!$passwordLine){throw 'Stored credential unavailable'}
 $secret=$passwordLine.Substring(9);$headers=@{'User-Agent'='Erlang.Net312-passive-job-progress';Authorization="Bearer $secret";Accept='application/vnd.github+json'}
 try {$jobs=Invoke-RestMethod -Method Get -Uri 'https://api.github.com/repos/develmax/Erlang.Net/actions/runs/38790457315/jobs?per_page=100' -Headers $headers} catch {throw 'GET progress failed; credential/response details suppressed'}
 if($jobs.total_count -ne 1 -or $jobs.jobs.Count -ne 1 -or $jobs.jobs[0].head_sha -ne '710996472fdb4a68b6b59103ce351247cf78e7a3' -or $jobs.jobs[0].run_attempt -ne 1){throw 'Progress job binding mismatch'}
 $stamp=[DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffffffZ');$jobs | ConvertTo-Json -Depth 30 | Set-Content "artifacts/part312-readonly-monitor/ordinary-jobs-$stamp.json"
 $job=$jobs.jobs[0];@{RunId=38790457315;Job=$job.id;Status=$job.status;Conclusion=$job.conclusion;ActiveSteps=@($job.steps | Where-Object status -eq 'in_progress' | ForEach-Object name);CompletedSteps=@($job.steps | Where-Object status -eq 'completed' | ForEach-Object name)} | ConvertTo-Json -Depth 8
} finally {$credentialLines=$null;$passwordLine=$null;$secret=$null;$headers=$null}
