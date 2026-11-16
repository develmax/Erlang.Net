param([string]$Output = 'artifacts/supervisor/source-audit.json')
$ErrorActionPreference = 'Stop'
$repository = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
Push-Location $repository
try {
    $reference = Join-Path (Split-Path $repository -Parent) 'otp'
    $rows = @()
    $source = 'src/Otp/Original/Foundation/Erlang.Otp.Stdlib.Beam/Otp/supervisor.erl'
    $checks = @([pscustomobject]@{Local=$source;Upstream='lib/stdlib/src/supervisor.erl'})
    foreach ($file in Get-ChildItem tools/Validation/Erlang.SupervisorCompatibility/Reference -Filter '*.erl') {
        $checks += [pscustomobject]@{Local=[IO.Path]::GetRelativePath($repository,$file.FullName);Upstream=('lib/stdlib/test/' + $file.Name)}
    }
    foreach ($check in $checks) {
        $expected = git -C $reference rev-parse ('OTP-29.1.1:' + $check.Upstream)
        if ($LASTEXITCODE -ne 0) { throw 'Pinned OTP blob unavailable.' }
        $actual = git hash-object -- $check.Local
        if ($LASTEXITCODE -ne 0 -or $expected -cne $actual) { throw ('Pinned source differs: ' + $check.Local) }
        $rows += [ordered]@{Path=$check.Local.Replace('\','/');Upstream=$check.Upstream;GitBlob=$actual;Sha256=(Get-FileHash -LiteralPath $check.Local).Hash}
    }
    $managed = @(Get-ChildItem src/Otp/Managed/Erlang.Otp.Stdlib/Processes/Supervision -Filter 'Supervisor*.cs' | Where-Object {
        $_.Name -notin 'Supervisor.cs','SupervisorAtoms.cs','SupervisorConfiguration.cs','SupervisorErrorReasons.cs','SupervisorHandle.cs','SupervisorInspection456.cs','SupervisorMessageTags.cs'
    } | ForEach-Object {
        [ordered]@{Path=[IO.Path]::GetRelativePath($repository,$_.FullName).Replace('\','/');Sha256=(Get-FileHash $_.FullName).Hash}
    })
    [ordered]@{
        Baseline='OTP-29.1.1';Commit='ad05823719d77c8faee87348ea39513d4e2f99c5'
        UnchangedPinnedSources=$rows;ManagedSources=$managed
        Scope='Source identity and implementation inventory only; semantic results are in backend reports.'
    } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $Output -Encoding utf8
}
finally { Pop-Location }
