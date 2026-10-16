param(
    [Parameter(Mandatory=$true)][ValidateSet('ordinary','native','reallocation','distribution')][string]$Kind,
    [Parameter(Mandatory=$true)][ValidatePattern('^[0-9a-f]{40}$')][string]$ExpectedHead,
    [Parameter(Mandatory=$true)][string]$ReviewedAuthorityPath,
    [Parameter(Mandatory=$true)][ValidatePattern('^[0-9A-Fa-f]{64}$')][string]$ExpectedAuthoritySha256
)
$ErrorActionPreference='Stop'
$Kind=$Kind.ToLowerInvariant()
$repo=[IO.Path]::GetFullPath((Get-Location).Path)
function Read-BytesHash([string]$Path) { return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash }
function Resolve-RepositoryFile([string]$Name) {
    if ([IO.Path]::IsPathRooted($Name)) { throw 'Authority binding must be repository relative' }
    $path=[IO.Path]::GetFullPath((Join-Path $repo $Name))
    if (!$path.StartsWith($repo+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase) -or !(Test-Path -LiteralPath $path -PathType Leaf)) { throw 'Authority binding escapes repository or is missing' }
    return $path
}
function Write-ExclusiveJson([string]$Path,$Value) {
    $data=[Text.UTF8Encoding]::new($false).GetBytes(($Value | ConvertTo-Json -Depth 12)+[char]10)
    $stream=[IO.File]::Open($Path,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
    try { $stream.Write($data,0,$data.Length);$stream.Flush($true) } finally { $stream.Dispose() }
}
if ((Read-BytesHash $ReviewedAuthorityPath) -cne $ExpectedAuthoritySha256.ToUpperInvariant()) { throw 'Root-reviewed authority hash differs' }
$authority=Get-Content -LiteralPath $ReviewedAuthorityPath -Raw | ConvertFrom-Json
if ($authority.Status -cne 'ROOT_REVIEWED_PART332_POST_PUBLICATION_DISPATCH_AUTHORITY' -or $authority.RootExecutionApproved -isnot [bool] -or !$authority.RootExecutionApproved) { throw 'Unbound proposal: Root-approved final authority required' }
if ($authority.Part -ne 332 -or $authority.PublishedHead -cne $ExpectedHead) { throw 'Exact published332 head required' }
if ($authority.DispatcherSourceSha256 -cne (Read-BytesHash $PSCommandPath)) { throw 'Dispatcher source differs from independently reviewed source' }
foreach ($flag in @('CompositeFullValidationPassed','IntegrationAndPackagesPassed','FreshPostFullWindowsLegacyPassed','FreshPostFullWindowsReallocationPassed','FreshPostFullWindowsProviderFivePhasesPassed','ActualStagedCanonicalSourcePlansPassed','DefaultGitWhitespaceCheckPassed')) {
    if ($authority.$flag -isnot [bool] -or !$authority.$flag) { throw ('Actual final gate incomplete: '+$flag) }
}
$required=@('SourceFreeze','FullGuard','NativeGuard','StageGuard','EvidenceReceipt')
$roles=@($authority.RequiredBindings | ForEach-Object Role)
if ($roles.Count -ne @($roles | Select-Object -Unique).Count -or @($required | Where-Object {$_ -notin $roles}).Count -ne 0) { throw 'Required unique actual proof roles missing' }
foreach ($binding in $authority.RequiredBindings) {
    $path=Resolve-RepositoryFile $binding.Path
    if ($binding.Sha256 -cnotmatch '^[A-F0-9]{64}$' -or (Read-BytesHash $path) -cne $binding.Sha256) { throw ('Final authority binding differs: '+$binding.Role) }
}
$sourceBinding=@($authority.RequiredBindings | Where-Object Role -ceq SourceFreeze)[0]
$freeze=Get-Content -LiteralPath (Resolve-RepositoryFile $sourceBinding.Path) -Raw | ConvertFrom-Json
$legacyPlans=@($freeze.Plans | Where-Object Path -ceq 'tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json')
$ownPlans=@($freeze.Plans | Where-Object Path -ceq 'tools/Erlang.NifReallocationObservations/SOURCE-PLAN.json')
if ($freeze.OwnedPaths -ne 25 -or $freeze.OwnedCSharpFiles -ne 21 -or $freeze.SourceClosurePaths -ne 2431 -or $authority.ExpectedFullPassed -ne 6822 -or $authority.ExpectedRegistry -ne 521 -or $ExpectedHead -ceq '6e4019aacb479ee407a91c92b6b7d1b1fcd9bf6d' -or $legacyPlans.Count -ne 1 -or $ownPlans.Count -ne 1 -or $legacyPlans[0].Inputs -ne 1128 -or $ownPlans[0].Inputs -ne 1153) { throw 'Actual final332 source/count authority differs or publication has not advanced' }
if (@($authority.ReviewedPlans).Count -ne 2) { throw 'Two actual Main2 reviewed plan authorities required' }
foreach ($plan in @($legacyPlans[0],$ownPlans[0])) {
    $reviewed=@($authority.ReviewedPlans | Where-Object Path -ceq $plan.Path)
    if ($reviewed.Count -ne 1 -or $reviewed[0].Sha256 -cne $plan.Sha256 -or $reviewed[0].Inputs -ne $plan.Inputs -or $plan.Sha256 -cnotmatch '^[A-F0-9]{64}$' -or (Read-BytesHash (Resolve-RepositoryFile $plan.Path)) -cne $plan.Sha256) { throw 'Actual Main2 reviewed plan authority differs' }
}
$local=(git rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $local -cne $ExpectedHead) { throw 'Exact committed local head required' }
$prefix=Join-Path $repo ('artifacts/root-part332-'+$Kind+'-dispatch')
if ((Test-Path -LiteralPath ($prefix+'-attempt.json')) -or (Test-Path -LiteralPath ($prefix+'-result.json'))) { throw 'Prior attempt/result exists; map with GET and never repeat POST' }
$workflowNames=@{ordinary='differential.yml';native='nif-pending-native.yml';reallocation='nif-reallocation-native.yml';distribution='distribution-signal-observations.yml'}
$workflowName=$workflowNames[$Kind]
$bodyObject=@{ref='main'}
if ($Kind -ceq 'reallocation') { $bodyObject.inputs=@{expected_head=$ExpectedHead} }
$body=$bodyObject | ConvertTo-Json -Depth 4 -Compress
$credentialLines=$null;$passwordLine=$null;$secret=$null;$headers=$null
try {
    $credentialLines="protocol=https`nhost=github.com`n`n" | git credential fill
    if ($LASTEXITCODE -ne 0) { throw 'Credential provider failed; details suppressed' }
    $passwordLine=$credentialLines | Where-Object {$_ -like 'password=*'} | Select-Object -First 1
    if (!$passwordLine) { throw 'Stored credential unavailable' }
    $secret=$passwordLine.Substring(9)
    $headers=@{'User-Agent'='Erlang.Net-part332-root-reviewed-dispatch';Authorization="Bearer $secret";Accept='application/vnd.github+json'}
    $api='https://api.github.com/repos/develmax/Erlang.Net'
    $branch=Invoke-RestMethod -Method Get -Uri "$api/branches/main" -Headers $headers
    if ($branch.commit.sha -cne $ExpectedHead) { throw 'Remote main differs from exact approved head' }
    $workflow=Invoke-RestMethod -Method Get -Uri "$api/actions/workflows/$workflowName" -Headers $headers
    if ($workflow.state -cne 'active' -or $workflow.path -cne ('.github/workflows/'+$workflowName)) { throw 'Wrong or inactive workflow' }
    # Atomic create before the single irreversible request; ambiguous outcomes are never retried.
    Write-ExclusiveJson ($prefix+'-attempt.json') @{Stage='BEFORE_SINGLE_POST';Head=$ExpectedHead;Kind=$Kind;Workflow=$workflow.id;WorkflowPath=$workflow.path;AuthoritySha256=$ExpectedAuthoritySha256.ToUpperInvariant();DispatcherSha256=(Read-BytesHash $PSCommandPath);Body=$bodyObject;ObservedRemoteHead=$branch.commit.sha;Time=[DateTime]::UtcNow.ToString('O');AutomaticRetryAllowed=$false}
    Invoke-RestMethod -Method Post -Uri "$api/actions/workflows/$workflowName/dispatches" -Headers $headers -ContentType 'application/json' -Body $body | Out-Null
    Write-ExclusiveJson ($prefix+'-result.json') @{Stage='SINGLE_POST_ACCEPTED';Head=$ExpectedHead;Kind=$Kind;Workflow=$workflow.id;WorkflowPath=$workflow.path;AuthoritySha256=$ExpectedAuthoritySha256.ToUpperInvariant();Time=[DateTime]::UtcNow.ToString('O');TerminalEvidenceAvailable=$false;ExactRunMappingStillRequired=$true;OriginalResultVerified=$false}
} finally { $credentialLines=$null;$passwordLine=$null;$secret=$null;$headers=$null }
