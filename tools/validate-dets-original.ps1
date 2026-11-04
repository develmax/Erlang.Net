param(
    [string]$Admission = 'artifacts/library376-official-compiler/official-admission.json',
    [string]$Output = 'artifacts/dets-acceptance/original',
    [switch]$Suite,
    [switch]$Gc,
    [switch]$Delete
)
$ErrorActionPreference = 'Stop'
if (([int]$Suite.IsPresent + [int]$Gc.IsPresent + [int]$Delete.IsPresent) -gt 1) { throw 'Select one observer mode.' }
$repository = Split-Path $PSScriptRoot -Parent
Push-Location $repository
try {
    $profile = Get-Content -LiteralPath $Admission -Raw | ConvertFrom-Json
    if ($profile.ReleaseVersion -cne '29.1.1' -or
        $profile.SourceCommit -cne 'ad05823719d77c8faee87348ea39513d4e2f99c5' -or
        -not $profile.EveryExtractedFileMatchesArchive -or
        (Get-FileHash -LiteralPath $profile.ExecutionManifestPath).Hash -cne $profile.ExecutionManifestSha256) {
        throw 'DETS requires the admitted pinned OTP provider.'
    }
    $inventory = Get-Content -LiteralPath $profile.ExecutionManifestPath -Raw | ConvertFrom-Json
    $closure = @($inventory | Where-Object {
        $_.Path -eq 'bin/erl.exe' -or
        $_.Path -match '^erts-[^/]+/bin/(erl.exe|beam.smp.dll)$' -or
        ($Delete -and $_.Path -match '^lib/erts-[^/]+/(ebin|src)/prim_file\.(beam|erl)$') -or
        $_.Path -match '^lib/stdlib-8.1/(ebin|src)/(dets|dets_v9|dets_utils|dets_server|dets_sup)\.(beam|erl)$'
    })
    if ($closure.Count -lt 13) { throw 'DETS reference closure is incomplete.' }
    if ($Delete -and @($closure | Where-Object { $_.Path -match '/prim_file\.(beam|erl)$' }).Count -lt 2) { throw 'prim_file reference closure is incomplete.' }
    function Test-Closure {
        foreach ($entry in $closure) {
            if ((Get-FileHash -LiteralPath (Join-Path $profile.InstalledOtpRoot $entry.Path)).Hash -cne $entry.Sha256) {
                throw 'DETS reference input changed.'
            }
        }
    }
    Test-Closure
    $directory = [IO.Path]::GetFullPath($Output)
    if (Test-Path -LiteralPath $directory) { throw 'Select a fresh DETS capture directory.' }
    New-Item -ItemType Directory -Path $directory | Out-Null
    $fixtureRoot = if ($Delete) { 'tests/Erlang.Tests/Resources/PrimFileDelete' } elseif ($Suite) { 'tests/Erlang.Tests/Resources/DetsSuiteBackend' } elseif ($Gc) { 'tests/Erlang.Tests/Resources/DetsGcBackend' } else { 'tests/Erlang.Tests/Resources/DetsBackend' }
    $observer = if ($Delete) { 'prim_file_delete_cases' } elseif ($Suite) { 'dets_suite_cases' } elseif ($Gc) { 'dets_magic_cases' } else { 'dets_port_cases' }
    if (-not $Suite -and -not $Gc -and -not $Delete) { Copy-Item tests/Erlang.Tests/Resources/DetsBackend/legacy/* -Destination $directory }
    $relative = [IO.Path]::GetRelativePath($repository, $directory).Replace('\', '/')
    if ($relative.Contains('"') -or $relative.Contains("`n")) { throw 'Unsupported capture path.' }
    $compileSuite = if ($Suite) { '{ok,dets_SUITE}=compile:file("' + $fixtureRoot + '/dets_SUITE.erl",[{outdir,"' + $relative + '"}]),' } else { '' }
    $expression = $compileSuite + '{ok,' + $observer + '}=compile:file("' + $fixtureRoot + '/' + $observer + '.erl",[{outdir,"' + $relative + '"}]),' +
        'true=code:add_patha("' + $relative + '"),' +
        'RunTag=make_ref(),{RunPid,RunMon}=spawn_monitor(' + $observer + ',run_worker,[self(),RunTag,"' + $relative + '"]),' +
        'Rows=receive {RunTag,RunRows}->RunRows;{''DOWN'',RunMon,process,RunPid,RunReason}->erlang:error({dets_observer_failed,RunReason}) end,' +
        'ok=file:write_file("' + $relative + '/original.etf",term_to_binary(Rows)),' +
        'Tag=make_ref(),spawn(' + $observer + ',error_worker,[self(),Tag]),' +
        'receive {Tag,Errors}->ok=file:write_file("' + $relative + '/errors.etf",term_to_binary(Errors)) end,' +
        'ok=file:write_file("' + $relative + '/exports.etf",term_to_binary(dets:module_info(exports))),halt().'
    & (Join-Path $profile.InstalledOtpRoot 'bin/erl.exe') -noshell -eval $expression 2>&1 |
        Tee-Object -FilePath (Join-Path $directory 'oracle.log')
    if ($LASTEXITCODE -ne 0) { throw 'DETS reference capture failed; preserve the output.' }
    Test-Closure
    Copy-Item -LiteralPath ($fixtureRoot + '/' + $observer + '.erl') -Destination $directory
    [ordered]@{
        Baseline = $profile.ReleaseTag
        Commit = $profile.SourceCommit
        ExecutionManifestSha256 = $profile.ExecutionManifestSha256
        Inputs = $closure
        ObserverSourceSha256 = (Get-FileHash -LiteralPath ($fixtureRoot + '/' + $observer + '.erl')).Hash
        ObserverBeamSha256 = (Get-FileHash (Join-Path $directory ($observer + '.beam'))).Hash
        DeleteRacesSha256 = if ($Delete) { (Get-FileHash (Join-Path $directory "deletion-races.etf")).Hash } else { $null }
        CorpusSha256 = (Get-FileHash (Join-Path $directory 'original.etf')).Hash
        ErrorsSha256 = (Get-FileHash (Join-Path $directory 'errors.etf')).Hash
        ExportsSha256 = (Get-FileHash (Join-Path $directory 'exports.etf')).Hash
        SuiteSourceSha256 = if ($Suite) { (Get-FileHash -LiteralPath ($fixtureRoot + '/dets_SUITE.erl')).Hash } else { $null }
        SuiteBeamSha256 = if ($Suite) { (Get-FileHash -LiteralPath (Join-Path $directory 'dets_SUITE.beam')).Hash } else { $null }
        LegacyInputs = @(Get-ChildItem tests/Erlang.Tests/Resources/DetsBackend/legacy -File | ForEach-Object {
            [ordered]@{ Name = $_.Name; Sha256 = (Get-FileHash -LiteralPath $_.FullName).Hash }
        })
    } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $directory 'provenance.json') -Encoding utf8
}
finally { Pop-Location }
