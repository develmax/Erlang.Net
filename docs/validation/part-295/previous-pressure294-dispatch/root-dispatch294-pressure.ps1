param([Parameter(Mandatory=$true)][string]$ExpectedHead)
$ErrorActionPreference='Stop'
$prefix='artifacts/root-part294-pressure-dispatch'
if(Test-Path -LiteralPath "$prefix-attempt.json"){throw 'Dispatch attempt already recorded; inspect remote state, never repeat POST'}
$head=(git rev-parse HEAD).Trim()
if($ExpectedHead -cnotmatch '^[0-9a-f]{40}$'){throw 'ExpectedHead must be the exact published SHA'}
if($head -cne $ExpectedHead){throw 'Head differs'}
$freeze=Get-Content artifacts/root-part294-final-source-freeze.json -Raw|ConvertFrom-Json
if($freeze.SourcePaths -ne 8 -or $freeze.Files.Count -ne 8 -or !$freeze.TokensExact){throw 'Final source/token gate incomplete'}
foreach($file in $freeze.Files){if((Get-FileHash -LiteralPath $file.Path).Hash -cne $file.Sha256){throw "Source differs: $($file.Path)"}}
$receipt=Get-Content docs/validation/part-294/evidence-receipt.json -Raw|ConvertFrom-Json
if($receipt.SourcePaths -ne 8 -or $receipt.CliRefusalsPassed -ne 14 -or $receipt.RawControlsPassed -ne 2 -or $receipt.NewMainFullRepeated -or $receipt.NativePressureThreeExecuted -or $receipt.CI293Passed -ne 6603){throw 'Actual standalone tool gate incomplete'}
$localGate=Get-Content artifacts/root-part294-local-gates.json -Raw|ConvertFrom-Json
if($localGate.TargetExitCode -ne 0 -or !$localGate.TokensExact -or $localGate.CliPrewriteRefusalsPassed -ne 14 -or $localGate.RawProcessControlsPassed -ne 2 -or $localGate.OriginalPressureExecuted){throw 'Final local tool gate differs'}
if($receipt.ExecutedToolDllSha256 -cne $localGate.ActualDllSha256){throw 'Actual collector DLL binding differs'}
$credentialLines="protocol=https`nhost=github.com`n`n"|git credential fill
if($LASTEXITCODE){throw 'Credential provider failed'}
$passwordLine=$credentialLines|Where-Object {$_ -like 'password=*'}|Select-Object -First 1
if($null -eq $passwordLine){throw 'Credential provider did not return a token'}
$secret=$passwordLine.Substring(9)
$headers=@{'User-Agent'='Erlang.Net-reference-provenance';Authorization="Bearer $secret";Accept='application/vnd.github+json'}
try {
 $branch=Invoke-RestMethod -Uri 'https://api.github.com/repos/develmax/Erlang.Net/branches/main' -Headers $headers
 if($branch.commit.sha -cne $head){throw 'Published main differs'}
 $workflow=Invoke-RestMethod -Uri 'https://api.github.com/repos/develmax/Erlang.Net/actions/workflows/distribution-pressure-observations.yml' -Headers $headers
 if($workflow.state -ne 'active'){throw 'Workflow not active'}
 [pscustomobject]@{Stage='BEFORE_SINGLE_POST';Head=$head;Workflow=$workflow.id;Time=[DateTime]::UtcNow.ToString('O')}|ConvertTo-Json|Set-Content "$prefix-attempt.json"
 Invoke-RestMethod -Method Post -Uri 'https://api.github.com/repos/develmax/Erlang.Net/actions/workflows/distribution-pressure-observations.yml/dispatches' -Headers $headers -ContentType 'application/json' -Body '{"ref":"main"}'|Out-Null
 [pscustomobject]@{Stage='SINGLE_POST_ACCEPTED';Head=$head;Workflow=$workflow.id;NativeCompleted=$false;Time=[DateTime]::UtcNow.ToString('O')}|ConvertTo-Json|Set-Content "$prefix-result.json"
 Write-Output 'Single pressure workflow dispatch accepted; pinned probe compilation and three original observations pending.'
}finally{$secret=$null;$passwordLine=$null;$credentialLines=$null;$headers=$null}
