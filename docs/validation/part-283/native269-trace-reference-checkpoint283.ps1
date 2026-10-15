param([ValidateSet('Dispatch','Status','Download')][string]$Mode='Status', [Parameter(Mandatory=$true)][ValidatePattern('^[0-9a-f]{40}$')][string]$PublishedHead)
$ErrorActionPreference='Stop'
$repository='https://api.github.com/repos/develmax/Erlang.Net'
$frozenPath='artifacts/part-283-trace-frozen-head.txt'
$dispatchPath='artifacts/part-283-trace-reference-dispatch.json'
$head=$PublishedHead
if($Mode -eq 'Dispatch'){
 if((git rev-parse HEAD).Trim() -cne $head){throw 'Explicit published SHA differs from local committed HEAD'}
 py artifacts/native269-checkpoint283-trace-committed-source-guard.py --head $head
 if($LASTEXITCODE){throw 'Committed trace11/shared8/rawprobe/plan/historical6562/source11 guard failed'}
 git diff --quiet HEAD -- src tests tools .github/workflows
 if($LASTEXITCODE){throw 'Published source modified'}
 if(Test-Path -LiteralPath $dispatchPath){throw 'Dispatch receipt exists; inspect unknown outcome, never repeat POST'}
}
if((Test-Path -LiteralPath $frozenPath) -and [IO.File]::ReadAllText($frozenPath).Trim() -cne $head){throw 'Never replace frozen trace283 head'}
if($Mode -ne 'Dispatch' -and !(Test-Path -LiteralPath $frozenPath)){throw 'No authorized dispatch frozen head; inspect before status/download'}
$credentialLines = "protocol=https`nhost=github.com`n`n" | git credential fill
if ($LASTEXITCODE -ne 0) { throw 'Credential provider failed' }
$passwordLine = $credentialLines | Where-Object { $_ -like 'password=*' } | Select-Object -First 1
if ($null -eq $passwordLine) { throw 'Credential provider returned no token' }
$secret = $passwordLine.Substring(9)
$headers = @{ 'User-Agent'='Erlang.Net-development-validation'; Authorization="Bearer $secret"; Accept='application/vnd.github+json' }
try {
    if ($Mode -eq 'Dispatch') {
        $branch = Invoke-RestMethod -Uri "$repository/branches/main" -Headers $headers
        if ($branch.commit.sha -cne $head) { throw 'Published remote main differs from committed283-trace head' }
        if (!(Test-Path -LiteralPath $frozenPath)) { [IO.File]::WriteAllText($frozenPath,$head+[char]10) }
        $receipt = [ordered]@{ Head=$head; Pin='ad05823719d77c8faee87348ea39513d4e2f99c5'; FreshScenarioVMs=8; MetadataVMs=1; SemanticExpectations=$null; Workflows=@() }
        # Write before each mutation so an unknown dispatch outcome cannot silently repeat it.
        foreach ($workflow in @('distribution-trace-observations.yml')) {
            $row = [ordered]@{ Workflow=$workflow; Status='DispatchAttemptRecorded'; Utc=[DateTime]::UtcNow.ToString('o') }
            $receipt.Workflows += $row
            [IO.File]::WriteAllText($dispatchPath,($receipt | ConvertTo-Json -Depth 8)+[char]10)
            Invoke-RestMethod -Method Post -Uri "$repository/actions/workflows/$workflow/dispatches" -Headers $headers -ContentType 'application/json' -Body '{"ref":"main"}' | Out-Null
            $row.Status = 'DispatchAccepted'
            [IO.File]::WriteAllText($dispatchPath,($receipt | ConvertTo-Json -Depth 8)+[char]10)
            Write-Output "Dispatched $workflow at $head"
        }
    }
    $runs = Invoke-RestMethod -Uri "$repository/actions/runs?head_sha=$head&per_page=30" -Headers $headers
    foreach ($run in $runs.workflow_runs) {
        if ($run.head_sha -cne $head) { throw 'Run head differs from frozen283-trace' }
        $artifactNames = switch ($run.name) {
            'Pinned OTP distribution trace observations' { 'pinned-distribution-trace-observations' }
            'Validate CLR implementation' { 'validation' }
            default { $null }
        }
        if ($null -eq $artifactNames) { continue }
        $jobs = Invoke-RestMethod -Uri "$($run.url)/jobs" -Headers $headers
        [pscustomobject]@{ Run=$run.id; Name=$run.name; Status=$run.status; Conclusion=$run.conclusion; Head=$run.head_sha; Jobs=@($jobs.jobs | ForEach-Object { [pscustomobject]@{ Id=$_.id; Status=$_.status; Conclusion=$_.conclusion; Active=@($_.steps | Where-Object status -eq 'in_progress' | ForEach-Object name) } }) } | ConvertTo-Json -Depth 6
        if ($Mode -ne 'Download' -or $run.status -ne 'completed') { continue }
        foreach ($artifactName in @($artifactNames)) {
        $metadataPath = "artifacts/part-283-trace-$artifactName-metadata.json"
        if (Test-Path -LiteralPath $metadataPath) {
            $saved = Get-Content -LiteralPath $metadataPath -Raw | ConvertFrom-Json
            if ($saved.Run.id -ne $run.id -or $saved.Run.head_sha -cne $head) { throw 'Existing283-trace download is bound to a different run; never replace' }
            continue
        }
        $artifacts = Invoke-RestMethod -Uri "$($run.url)/artifacts" -Headers $headers
        $artifact = @($artifacts.artifacts | Where-Object name -eq $artifactName)
        if ($artifact.Count -ne 1 -or $artifact[0].expired) { throw 'Missing/expired283-trace artifact' }
        $zip = "artifacts/part-283-trace-$artifactName.zip"
        $destination = "artifacts/part-283-trace-$artifactName"
        if ((Test-Path -LiteralPath $zip) -or (Test-Path -LiteralPath $destination)) { throw 'Partial283-trace download exists; inspect rather than overwrite' }
        Invoke-WebRequest -Uri $artifact[0].archive_download_url -Headers $headers -OutFile $zip
        $zipHash=(Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash
        if((Get-Item -LiteralPath $zip).Length -ne $artifact[0].size_in_bytes -or $artifact[0].digest -cne "sha256:$($zipHash.ToLowerInvariant())"){throw 'Downloaded artifact size/digest mismatch; preserve partial raw ZIP'}
        Expand-Archive -LiteralPath $zip -DestinationPath $destination
        [pscustomobject]@{ Run=$run; Jobs=$jobs.jobs; Artifact=$artifact[0]; ZipSHA256=(Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash } | ConvertTo-Json -Depth 18 | Set-Content -LiteralPath $metadataPath
        Write-Output "Downloaded $artifactName artifact $($artifact[0].id)"
        }
    }
} finally {
    $secret=$null; $passwordLine=$null; $credentialLines=$null; $headers=$null
}
