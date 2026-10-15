param([Parameter(Mandatory=$true)][string]$ExpectedHead)
$ErrorActionPreference='Stop'
$prefix='artifacts/root-part302-original9-dispatch'
if(Test-Path -LiteralPath "$prefix-attempt.json"){throw 'Dispatch attempt already recorded; inspect remote state, never repeat POST'}
$head=(git rev-parse HEAD).Trim()
if($ExpectedHead -cnotmatch '^[0-9a-f]{40}$'){throw 'ExpectedHead must be the exact published SHA'}
if($head -cne $ExpectedHead){throw 'Head differs'}
$planPath='tools/Erlang.RawOnLoadVisibilityObservations/reviewed-execution-plan.json'
$approved='97C447A8AD78D37EA4F092963B7D4E5A7FEF5879E55DF1E4E4739F1409D5FBA9'
if((Get-FileHash -LiteralPath $planPath).Hash -cne $approved){throw 'Execution seal differs'}
$plan=Get-Content -LiteralPath $planPath -Raw|ConvertFrom-Json
if($plan.Files.Count -ne 18){throw 'Execution closure differs'}
foreach($file in $plan.Files){if((Get-FileHash -LiteralPath $file.Path).Hash -cne $file.Sha256 -or (Get-Item -LiteralPath $file.Path).Length -ne $file.Bytes){throw "Source differs: $($file.Path)"}}
$local=Get-Content artifacts/root-part302-local-tooling/local-validation.json -Raw|ConvertFrom-Json
if($local.Status -ne 'LOCAL_REVIEWED_SOURCE_AND_SYNTHETIC_CONTROLS_PASS' -or $local.DecoderControlsPassed -ne 35 -or !$local.ReportBudgetControlsPassed){throw 'Actual local tooling gate incomplete'}
if(!(Test-Path -LiteralPath 'docs/validation/part-302/evidence-receipt.json')){throw 'Finalized checkpoint required'}
$runtimeChanges=git diff HEAD^ HEAD --name-only -- src tests
if($LASTEXITCODE -ne 0 -or $runtimeChanges){throw 'Tooling-only commit required'}
$credentialLines="protocol=https`nhost=github.com`n`n"|git credential fill
if($LASTEXITCODE){throw 'Credential provider failed'}
$passwordLine=$credentialLines|Where-Object {$_ -like 'password=*'}|Select-Object -First 1
if($null -eq $passwordLine){throw 'Credential provider did not return a token'}
$secret=$passwordLine.Substring(9)
$headers=@{'User-Agent'='Erlang.Net-reference-provenance';Authorization="Bearer $secret";Accept='application/vnd.github+json'}
try {
 $branch=Invoke-RestMethod -Uri 'https://api.github.com/repos/develmax/Erlang.Net/branches/main' -Headers $headers
 if($branch.commit.sha -cne $head){throw 'Published main differs'}
 $workflow=Invoke-RestMethod -Uri 'https://api.github.com/repos/develmax/Erlang.Net/actions/workflows/raw-onload-original-observations.yml' -Headers $headers
 if($workflow.state -ne 'active'){throw 'Workflow not active'}
 [pscustomobject]@{Stage='BEFORE_SINGLE_POST';Head=$head;Workflow=$workflow.id;ApprovedExecutionPlanSha256=$approved;Time=[DateTime]::UtcNow.ToString('O')}|ConvertTo-Json|Set-Content "$prefix-attempt.json"
 Invoke-RestMethod -Method Post -Uri 'https://api.github.com/repos/develmax/Erlang.Net/actions/workflows/raw-onload-original-observations.yml/dispatches' -Headers $headers -ContentType 'application/json' -Body (@{ref='main';inputs=@{approved_execution_plan_sha256=$approved}}|ConvertTo-Json -Compress)|Out-Null
 [pscustomobject]@{Stage='SINGLE_POST_ACCEPTED';Head=$head;Workflow=$workflow.id;ApprovedExecutionPlanSha256=$approved;NativeCompleted=$false;Time=[DateTime]::UtcNow.ToString('O')}|ConvertTo-Json|Set-Content "$prefix-result.json"
 Write-Output 'Single workflow dispatch accepted; original observations pending.'
}finally{$secret=$null;$passwordLine=$null;$credentialLines=$null;$headers=$null}





