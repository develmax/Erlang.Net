param([ValidateSet('Dispatch','Status','Download')][string]$Mode='Status')
$ErrorActionPreference = 'Stop'
$repository = 'https://api.github.com/repos/develmax/Erlang.Net'
$frozenPath = 'artifacts/part-271-frozen-head.txt'
$dispatchPath = 'artifacts/part-271-reference-dispatch.json'
$baseline = 'f26e1a15320553eb9d725764ec2260f3c262e1da'
$required = @('.github/workflows/distribution-packet-contrasts.yml', '.github/workflows/validate.yml', 'tools/Erlang.DistributionPacketContrasts/distribution_packet_probe.erl', 'tools/Erlang.MixedMapIdentityContrasts/mixed_map_identity_probe.erl')
if ($Mode -eq 'Dispatch') {
    $candidateHead = (git rev-parse HEAD).Trim()
    if ($LASTEXITCODE -ne 0 -or $candidateHead -eq $baseline) { throw 'Part271 must be committed and published before dispatch' }
    foreach ($path in $required) {
        git cat-file -e "$($candidateHead):$path"
        if ($LASTEXITCODE -ne 0) { throw "Committed271 source missing: $path" }
    }
    py -3 artifacts/reference-checkpoint271-source-guard.py $candidateHead
    if ($LASTEXITCODE -ne 0) { throw 'Committed271 source bindings failed' }
    if (Test-Path -LiteralPath $dispatchPath) { throw 'Part271 joint observation dispatch receipt already exists; never duplicate dispatch' }
    if ((Test-Path -LiteralPath $frozenPath) -and [IO.File]::ReadAllText($frozenPath).Trim() -cne $candidateHead) { throw 'Never replace frozen271 head' }
    $head = $candidateHead
} else {
    $head = [IO.File]::ReadAllText($frozenPath).Trim()
}
if ($head -notmatch '^[0-9a-f]{40}$') { throw 'Unexpected271 implementation head' }
$credentialLines = "protocol=https`nhost=github.com`n`n" | git credential fill
if ($LASTEXITCODE -ne 0) { throw 'Credential provider failed' }
$passwordLine = $credentialLines | Where-Object { $_ -like 'password=*' } | Select-Object -First 1
if ($null -eq $passwordLine) { throw 'Credential provider returned no token' }
$secret = $passwordLine.Substring(9)
$headers = @{ 'User-Agent'='Erlang.Net-development-validation'; Authorization="Bearer $secret"; Accept='application/vnd.github+json' }
try {
    if ($Mode -eq 'Dispatch') {
        $branch = Invoke-RestMethod -Uri "$repository/branches/main" -Headers $headers
        if ($branch.commit.sha -cne $head) { throw 'Published remote main differs from committed271 head' }
        if (!(Test-Path -LiteralPath $frozenPath)) { [IO.File]::WriteAllText($frozenPath,$head+[char]10) }
        $receipt = [ordered]@{ Head=$head; PacketFreshVms=1; MixedFreshVms=1; SharedPinnedBuilds=1; Ordinary270Rerun=$false; Workflows=@() }
        # Write before each mutation so an unknown dispatch outcome cannot silently repeat it.
        foreach ($workflow in @('distribution-packet-contrasts.yml')) {
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
        if ($run.head_sha -cne $head) { throw 'Run head differs from frozen271' }
        $artifactNames = switch ($run.name) {
            'Pinned OTP distribution packet observations' { @('pinned-distribution-packet','pinned-mixed-map-identity') }
            'Validate CLR implementation' { 'validation' }
            default { $null }
        }
        if ($null -eq $artifactNames) { continue }
        $jobs = Invoke-RestMethod -Uri "$($run.url)/jobs" -Headers $headers
        [pscustomobject]@{ Run=$run.id; Name=$run.name; Status=$run.status; Conclusion=$run.conclusion; Head=$run.head_sha; Jobs=@($jobs.jobs | ForEach-Object { [pscustomobject]@{ Id=$_.id; Status=$_.status; Conclusion=$_.conclusion; Active=@($_.steps | Where-Object status -eq 'in_progress' | ForEach-Object name) } }) } | ConvertTo-Json -Depth 6
        if ($Mode -ne 'Download' -or $run.status -ne 'completed') { continue }
        foreach ($artifactName in @($artifactNames)) {
        $metadataPath = "artifacts/part-271-$artifactName-metadata.json"
        if (Test-Path -LiteralPath $metadataPath) {
            $saved = Get-Content -LiteralPath $metadataPath -Raw | ConvertFrom-Json
            if ($saved.Run.id -ne $run.id -or $saved.Run.head_sha -cne $head) { throw 'Existing271 download is bound to a different run; never replace' }
            continue
        }
        $artifacts = Invoke-RestMethod -Uri "$($run.url)/artifacts" -Headers $headers
        $artifact = @($artifacts.artifacts | Where-Object name -eq $artifactName)
        if ($artifact.Count -ne 1 -or $artifact[0].expired) { throw 'Missing/expired271 artifact' }
        $zip = "artifacts/part-271-$artifactName.zip"
        $destination = "artifacts/part-271-$artifactName"
        if ((Test-Path -LiteralPath $zip) -or (Test-Path -LiteralPath $destination)) { throw 'Partial271 download exists; inspect rather than overwrite' }
        Invoke-WebRequest -Uri $artifact[0].archive_download_url -Headers $headers -OutFile $zip
        Expand-Archive -LiteralPath $zip -DestinationPath $destination
        [pscustomobject]@{ Run=$run; Jobs=$jobs.jobs; Artifact=$artifact[0]; ZipSHA256=(Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash } | ConvertTo-Json -Depth 18 | Set-Content -LiteralPath $metadataPath
        Write-Output "Downloaded $artifactName artifact $($artifact[0].id)"
        }
    }
} finally {
    $secret=$null; $passwordLine=$null; $credentialLines=$null; $headers=$null
}
