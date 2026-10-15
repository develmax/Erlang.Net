param(
    [Parameter(Mandatory)][string] $Repository,
    [Parameter(Mandatory)][ValidatePattern('^[0-9a-fA-F]{40}$')][string] $ExpectedHead
)

$ErrorActionPreference = 'Stop'
$taskRepository = (Resolve-Path -LiteralPath $Repository).Path
$taskProposal = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../raw-onload-combined303-execution-proposal2')).Path
$taskFreezePath = Join-Path $taskProposal 'source-freeze.json'
$taskFreezeSha = 'A5D7FBD058265DCDFF948E04485612D35FE68F838F1FC7FDB9D5085D6FEFA3E2'
$taskPlanSha = '97C447A8AD78D37EA4F092963B7D4E5A7FEF5879E55DF1E4E4739F1409D5FBA9'
$taskActualHead = (& git -C $taskRepository rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $taskActualHead -cne $ExpectedHead.ToLowerInvariant()) {
    throw 'The explicitly supplied current checkout head does not match.'
}
if ((Get-FileHash -LiteralPath $taskFreezePath -Algorithm SHA256).Hash -cne $taskFreezeSha) {
    throw 'Frozen proposal2 manifest differs.'
}
$taskFreeze = Get-Content -LiteralPath $taskFreezePath -Raw | ConvertFrom-Json
$taskOverlay = Join-Path $taskProposal 'overlay'
$taskEntries = @($taskFreeze.Files | Where-Object { $_.Path.StartsWith('overlay/') })
if ($taskEntries.Count -ne 19 -or $taskFreeze.ExecutionPlanSha256 -cne $taskPlanSha) {
    throw 'Expected coherent19-file overlay and execution seal.'
}
$taskSourceMembers = @(Get-ChildItem -LiteralPath $taskOverlay -File -Recurse)
if ($taskSourceMembers.Count -ne 19) {
    throw 'Additional or missing overlay files.'
}
$taskCopies = @()
foreach ($taskEntry in $taskFreeze.Files) {
    $taskSource = Join-Path $taskProposal $taskEntry.Path
    if (-not (Test-Path -LiteralPath $taskSource -PathType Leaf) -or
        (Get-Item -LiteralPath $taskSource).Length -ne $taskEntry.Bytes -or
        (Get-FileHash -LiteralPath $taskSource -Algorithm SHA256).Hash -cne $taskEntry.Sha256) {
        throw ('Frozen proposal member differs: ' + $taskEntry.Path)
    }
}
foreach ($taskEntry in $taskEntries) {
    $taskRelative = $taskEntry.Path.Substring('overlay/'.Length)
    $taskSource = Join-Path $taskOverlay $taskRelative
    $taskTarget = Join-Path $taskRepository $taskRelative
    if (Test-Path -LiteralPath $taskTarget) {
        if (-not (Test-Path -LiteralPath $taskTarget -PathType Leaf) -or
            (Get-FileHash -LiteralPath $taskTarget -Algorithm SHA256).Hash -cne $taskEntry.Sha256) {
            throw ('Existing target differs; review before promotion: ' + $taskRelative)
        }
    } elseif ($taskRelative -ceq 'tools/Erlang.NifPendingVarargsObservations/verify-original13.py') {
        throw 'The reviewed existing helper must already exist unchanged.'
    } else {
        $taskCopies += [pscustomobject]@{ Source = $taskSource; Target = $taskTarget }
    }
}
$taskAttributes = Join-Path $taskRepository '.gitattributes'
if (-not (Test-Path -LiteralPath $taskAttributes -PathType Leaf)) {
    throw 'Existing .gitattributes required.'
}
$taskAttributeBytes = [IO.File]::ReadAllBytes($taskAttributes)
$taskAttributeText = [Text.Encoding]::UTF8.GetString($taskAttributeBytes)
$taskRequiredLines = @(
    'tools/Erlang.RawOnLoadVisibilityObservations/** -text',
    '.github/workflows/raw-onload-original-observations.yml -text',
    'tools/Erlang.NifPendingVarargsObservations/verify-original13.py -text'
)
$taskMissingLines = @($taskRequiredLines | Where-Object { $_ -cnotin ($taskAttributeText -split '\r?\n') })

# All source and existing-target checks precede the first workspace write.
foreach ($taskCopy in $taskCopies) {
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($taskCopy.Target)) | Out-Null
    [IO.File]::Copy($taskCopy.Source, $taskCopy.Target, $false)
}
if ($taskMissingLines.Count -gt 0) {
    $taskCurrentAttributes = [IO.File]::ReadAllBytes($taskAttributes)
    if ([Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($taskCurrentAttributes)) -cne
        [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($taskAttributeBytes))) {
        throw '.gitattributes changed concurrently; stop and review the reversible copy.'
    }
    $taskSeparator = if ($taskAttributeText.EndsWith("`n")) { '' } else { "`n" }
    $taskAppend = [Text.Encoding]::UTF8.GetBytes($taskSeparator + ($taskMissingLines -join "`n") + "`n")
    $taskStream = [IO.File]::Open($taskAttributes, [IO.FileMode]::Append, [IO.FileAccess]::Write, [IO.FileShare]::None)
    try { $taskStream.Write($taskAppend, 0, $taskAppend.Length) } finally { $taskStream.Dispose() }
}
foreach ($taskEntry in $taskEntries) {
    $taskTarget = Join-Path $taskRepository $taskEntry.Path.Substring('overlay/'.Length)
    if ((Get-FileHash -LiteralPath $taskTarget -Algorithm SHA256).Hash -cne $taskEntry.Sha256) {
        throw ('Promoted bytes differ: ' + $taskEntry.Path)
    }
}
Write-Output 'Reviewed tooling overlay copied byte-exact; no build, stage, commit, push or workflow dispatch performed.'
