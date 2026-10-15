param([Parameter(Mandatory=$true)][string]$ExpectedHead)
$ErrorActionPreference='Stop'
$prefix='artifacts/root-part300-native-dispatch'
if(Test-Path -LiteralPath "$prefix-attempt.json"){throw 'Dispatch attempt already recorded; inspect remote state, never repeat POST'}
$head=(git rev-parse HEAD).Trim()
if($ExpectedHead -cnotmatch '^[0-9a-f]{40}$'){throw 'ExpectedHead must be the exact published SHA'}
if($head -cne $ExpectedHead){throw 'Head differs'}
$freeze=Get-Content artifacts/root-part300-final-source-freeze.json -Raw|ConvertFrom-Json
if($freeze.SourcePaths -ne 30 -or $freeze.Files.Count -ne 30 -or !$freeze.TokensExactForCSharp){throw 'Final source/token gate incomplete'}
foreach($file in $freeze.Files){if((Get-FileHash -LiteralPath $file.Path).Hash -cne $file.Sha256){throw "Source differs: $($file.Path)"}}
$receipt=Get-Content docs/validation/part-300/evidence-receipt.json -Raw|ConvertFrom-Json
if($receipt.Passed -ne 6611 -or $receipt.Failed -ne 0 -or !$receipt.IntegrationAndPackagesPassed){throw 'Actual Main gate incomplete'}
$native=Get-Content artifacts/root-part300-main-native-guard.json -Raw|ConvertFrom-Json
if($native.Status -ne 'PASS_ACTUAL_MAIN_NATIVE_TRUSTED_WINDOWS_AND_COMPILED_SOURCE' -or !$native.NativeTrustedWindowsProfilePassed -or !$native.ActualGeneratedCSharpRoutePassed){throw 'Native Main gate incomplete'}
if((Get-FileHash tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json).Hash -ne '7D5BBD71767A5C202A87C930D308A7D2AFE45C490EE33B236D918900122ACBFA'){throw 'Reviewed native plan differs'}
$credentialLines="protocol=https`nhost=github.com`n`n"|git credential fill
if($LASTEXITCODE){throw 'Credential provider failed'}
$passwordLine=$credentialLines|Where-Object {$_ -like 'password=*'}|Select-Object -First 1
if($null -eq $passwordLine){throw 'Credential provider did not return a token'}
$secret=$passwordLine.Substring(9)
$headers=@{'User-Agent'='Erlang.Net-reference-provenance';Authorization="Bearer $secret";Accept='application/vnd.github+json'}
try {
 $branch=Invoke-RestMethod -Uri 'https://api.github.com/repos/develmax/Erlang.Net/branches/main' -Headers $headers
 if($branch.commit.sha -cne $head){throw 'Published main differs'}
 $workflow=Invoke-RestMethod -Uri 'https://api.github.com/repos/develmax/Erlang.Net/actions/workflows/nif-pending-native.yml' -Headers $headers
 if($workflow.state -ne 'active'){throw 'Workflow not active'}
 [pscustomobject]@{Stage='BEFORE_SINGLE_POST';Head=$head;Workflow=$workflow.id;Time=[DateTime]::UtcNow.ToString('O')}|ConvertTo-Json|Set-Content "$prefix-attempt.json"
 Invoke-RestMethod -Method Post -Uri 'https://api.github.com/repos/develmax/Erlang.Net/actions/workflows/nif-pending-native.yml/dispatches' -Headers $headers -ContentType 'application/json' -Body '{"ref":"main"}'|Out-Null
 [pscustomobject]@{Stage='SINGLE_POST_ACCEPTED';Head=$head;Workflow=$workflow.id;NativeCompleted=$false;Time=[DateTime]::UtcNow.ToString('O')}|ConvertTo-Json|Set-Content "$prefix-result.json"
 Write-Output 'Single workflow dispatch accepted; original observations pending.'
}finally{$secret=$null;$passwordLine=$null;$credentialLines=$null;$headers=$null}






