param([Parameter(Mandatory=$true)][string]$ExpectedHead)
$ErrorActionPreference='Stop'
$attemptPath='artifacts/root-part340-ordinary-dispatch-attempt.json'
$resultPath='artifacts/root-part340-ordinary-dispatch-result.json'
if(Test-Path -LiteralPath $attemptPath){throw 'Prior dispatch attempt exists; inspect rather than repeat POST'}
if($ExpectedHead -notmatch '^[0-9a-f]{40}$' -or (git rev-parse HEAD) -cne $ExpectedHead -or (git rev-parse HEAD^) -cne '36e6da6dfc2696adce6bff8808ca96bbd46596b6'){throw 'Publication identity differs'}
$ready=Get-Content docs/module-readiness.json -Raw | ConvertFrom-Json
$stage=Get-Content artifacts/root-part340-stage-guard.json -Raw | ConvertFrom-Json
$full=Get-Content artifacts/connected-remote-completion-full-actual4/completion-guard.json -Raw | ConvertFrom-Json
$proc=@($ready.Modules | Where-Object Name -eq 'proc_lib')
if($stage.Status -cne 'EXACT340_CACHED_STAGE_PASS' -or $full.Status -cne 'PASS_ACTUAL_ISOLATED340_FULL' -or !$full.SingleUninterruptedFullPass -or $full.Failed -ne 0 -or $full.Registry -ne 587 -or $full.ProcLibMfas -ne 39 -or $ready.RegisteredMfas -ne 587 -or $ready.LocalPassedTests -ne $full.Passed -or $proc.Count -ne 1 -or $proc[0].RegisteredMfas -ne 39){throw 'Actual validation differs'}
$credentialLines=$null;$passwordLine=$null;$secret=$null;$headers=$null
try {
 $credentialLines="protocol=https`nhost=github.com`n`n" | git credential fill
 if($LASTEXITCODE -ne 0){throw 'Credential lookup failed; details suppressed'}
 $passwordLine=$credentialLines | Where-Object {$_ -like 'password=*'} | Select-Object -First 1
 if(!$passwordLine){throw 'Stored credential unavailable'}
 $secret=$passwordLine.Substring(9)
 $headers=@{Authorization="Bearer $secret";'User-Agent'='Erlang.Net-part340-original-comparison';Accept='application/vnd.github+json'}
 $api='https://api.github.com/repos/develmax/Erlang.Net'
 $branch=Invoke-RestMethod -Method Get -Uri "$api/branches/main" -Headers $headers
 $workflow=Invoke-RestMethod -Method Get -Uri "$api/actions/workflows/379611753" -Headers $headers
 if($branch.commit.sha -cne $ExpectedHead -or $workflow.path -cne '.github/workflows/differential.yml' -or $workflow.state -cne 'active'){throw 'Remote head/workflow differs; not dispatched'}
 $attempt=@{Head=$ExpectedHead;Workflow=379611753;Path=$workflow.path;Time=[DateTime]::UtcNow.ToString('O');Body=@{ref='main'};AutomaticRetryAllowed=$false;ActualLocalPassed=$full.Passed;PlannedOriginal=6260;FdPlanned=8}
 $bytes=[Text.Encoding]::UTF8.GetBytes(($attempt | ConvertTo-Json -Depth 8)+[char]10)
 $stream=[IO.File]::Open($attemptPath,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
 try{$stream.Write($bytes);$stream.Flush($true)}finally{$stream.Dispose()}
 $response=Invoke-WebRequest -Method Post -Uri "$api/actions/workflows/379611753/dispatches" -Headers $headers -ContentType 'application/json' -Body '{"ref":"main"}'
 if($response.StatusCode -ne 204){throw 'Unexpected dispatch response; do not retry'}
 @{Head=$ExpectedHead;Workflow=379611753;Status='ACCEPTED_ONCE';HttpStatus=204;Time=[DateTime]::UtcNow.ToString('O')} | ConvertTo-Json | Set-Content -LiteralPath $resultPath
 Write-Output 'Part340 original comparison accepted once; CI is automatic.'
}finally{$credentialLines=$null;$passwordLine=$null;$secret=$null;$headers=$null}
