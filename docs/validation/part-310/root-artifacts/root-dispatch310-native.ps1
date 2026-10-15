param([Parameter(Mandatory=$true)][string]$ExpectedHead)
$ErrorActionPreference = 'Stop'
$prefix = 'artifacts/root-part310-native-dispatch'
if (Test-Path -LiteralPath "$prefix-attempt.json") { throw 'Dispatch attempt already recorded; inspect remote state, never repeat POST' }
$head = (git rev-parse HEAD).Trim()
if ($ExpectedHead -cnotmatch '^[0-9a-f]{40}$' -or $head -cne $ExpectedHead) { throw 'Exact reviewed published head required' }
$freeze = Get-Content artifacts/root-part310-final-source-freeze.json -Raw | ConvertFrom-Json
if ($freeze.SourcePaths -ne 26 -or $freeze.CSharpPaths -ne 20 -or !$freeze.TokensExactForCSharp -or $freeze.ReviewedSourceInputs -ne 1028) { throw 'Source/token gate incomplete' }
foreach ($file in $freeze.Files) {
    if ((Get-FileHash -LiteralPath $file.Path).Hash -cne $file.Sha256) { throw "Source differs: $($file.Path)" }
}
$full = Get-Content artifacts/root-part310-full-guard.json -Raw | ConvertFrom-Json
if ($full.Passed -ne 6638 -or $full.Failed -ne 0 -or @($full.IntegrationResults.PSObject.Properties).Count -ne 11 -or !$full.SourceHashesExact) { throw 'Actual Main full gate incomplete' }
$native = Get-Content artifacts/root-part310-main-native/guard.json -Raw | ConvertFrom-Json
if ($native.Status -cne 'ACTUAL_MAIN_POST_FULL_WINDOWS_NATIVE_SEVEN_ORDERED_LINES_AND_THREE_ALLOCATION_DOMAIN_ROLES_PASS' -or !$native.Process.NativeExecuted -or !$native.Parsed.NativePayloadDomainRolesValidated -or !$native.Parsed.LoadedOwnerSnapshotMatchesRuntime) { throw 'Actual Windows native gate incomplete' }
if (!(Test-Path -LiteralPath docs/validation/part-310/evidence-receipt.json)) { throw 'Published evidence receipt required' }
if ((Get-FileHash tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json).Hash -cne 'BFC52C82190D6BE2CAC9A9396E0F9B87C662A06B85249C76F910450352494CCB') { throw 'Reviewed native plan differs' }
$credentialLines = "protocol=https`nhost=github.com`n`n" | git credential fill
if ($LASTEXITCODE) { throw 'Credential provider failed' }
$passwordLine = $credentialLines | Where-Object { $_ -like 'password=*' } | Select-Object -First 1
if ($null -eq $passwordLine) { throw 'Credential provider did not return a token' }
$secret = $passwordLine.Substring(9)
$headers = @{ 'User-Agent'='Erlang.Net-reference-provenance'; Authorization="Bearer $secret"; Accept='application/vnd.github+json' }
try {
    $branch = Invoke-RestMethod -Uri 'https://api.github.com/repos/develmax/Erlang.Net/branches/main' -Headers $headers
    if ($branch.commit.sha -cne $head) { throw 'Published main differs' }
    $workflow = Invoke-RestMethod -Uri 'https://api.github.com/repos/develmax/Erlang.Net/actions/workflows/nif-pending-native.yml' -Headers $headers
    if ($workflow.state -ne 'active') { throw 'Workflow not active' }
    [pscustomobject]@{ Stage='BEFORE_SINGLE_POST'; Head=$head; Workflow=$workflow.id; ReviewedInputs=1028; Time=[DateTime]::UtcNow.ToString('O') } | ConvertTo-Json | Set-Content "$prefix-attempt.json"
    Invoke-RestMethod -Method Post -Uri 'https://api.github.com/repos/develmax/Erlang.Net/actions/workflows/nif-pending-native.yml/dispatches' -Headers $headers -ContentType 'application/json' -Body '{"ref":"main"}' | Out-Null
    [pscustomobject]@{ Stage='SINGLE_POST_ACCEPTED'; Head=$head; Workflow=$workflow.id; NativeCompleted=$false; Time=[DateTime]::UtcNow.ToString('O') } | ConvertTo-Json | Set-Content "$prefix-result.json"
    Write-Output 'Single reviewed Linux native workflow dispatch accepted; actual eight-line SDK and original13 observations pending.'
}
finally { $secret=$null; $passwordLine=$null; $credentialLines=$null; $headers=$null }
