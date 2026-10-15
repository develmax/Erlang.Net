param(
    [Parameter(Mandatory)][ValidatePattern('^[0-9a-f]{40}$')][string] $ExactPublishedHead,
    [Parameter(Mandatory)][ValidatePattern('^[^\s/]+/[^\s/]+$')][string] $RepositorySlug,
    [Parameter(Mandatory)][string] $OutputDirectory
)

$ErrorActionPreference = 'Stop'
$DispatchRef = 'main'
$taskOutput = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $taskOutput) { throw 'Fresh dispatch preparation directory required.' }
[IO.Directory]::CreateDirectory($taskOutput) | Out-Null
$taskSeal = '97C447A8AD78D37EA4F092963B7D4E5A7FEF5879E55DF1E4E4739F1409D5FBA9'
$taskBody = [ordered]@{ ref = $DispatchRef; inputs = [ordered]@{ approved_execution_plan_sha256 = $taskSeal } }
$taskContext = [ordered]@{
    Status = 'DRAFT_NOT_DISPATCHED'
    Repository = $RepositorySlug
    Workflow = 'raw-onload-original-observations.yml'
    ExpectedPublishedHead = $ExactPublishedHead
    DispatchRef = $DispatchRef
    ApprovedExecutionPlanSha256 = $taskSeal
    RemoteRefVerifiedByThisScript = $false
    WorkflowDispatched = $false
    Requirement = 'Operator must verify branches/main.commit.sha equals ExpectedPublishedHead, save the attempt record before exactly one POST, then verify actual returned run head before accepting evidence.'
}
[IO.File]::WriteAllText((Join-Path $taskOutput 'dispatch.json'), ($taskBody | ConvertTo-Json -Depth 4) + "`n", [Text.UTF8Encoding]::new($false))
[IO.File]::WriteAllText((Join-Path $taskOutput 'dispatch-context.json'), ($taskContext | ConvertTo-Json -Depth 4) + "`n", [Text.UTF8Encoding]::new($false))
Write-Output 'Draft payload created. No network request, ref creation, Git change or workflow dispatch performed.'
