param(
    [string]$Admission = 'artifacts/library376-official-compiler/official-admission.json',
    [string]$Output = 'artifacts/erts-internal-compatibility',
    [string]$RunnerDll,
    [switch]$CaptureOnly
)
$ErrorActionPreference = 'Stop'
$repository = Split-Path $PSScriptRoot -Parent
Push-Location $repository
try {
    $profile = Get-Content -LiteralPath $Admission -Raw | ConvertFrom-Json
    if ($profile.SourceCommit -cne 'ad05823719d77c8faee87348ea39513d4e2f99c5' -or
        $profile.ReleaseVersion -cne '29.1.1' -or -not $profile.EveryExtractedFileMatchesArchive -or
        (Get-FileHash -LiteralPath $profile.ExecutionManifestPath).Hash -cne $profile.ExecutionManifestSha256) {
        throw 'The OTP 29.1.1 provider is not admitted'
    }
    $inventory = Get-Content -LiteralPath $profile.ExecutionManifestPath -Raw | ConvertFrom-Json
    $closure = @($inventory | Where-Object {
        $_.Path -match '(^bin/erl.exe$|^erts-[^/]+/bin/(erl.exe|beam.smp.dll)$|/(erts_internal|erts_debug|erlang|ets|lists|persistent_term)\.(beam|erl)$)'
    })
    if ($closure.Count -lt 7) { throw 'The OTP provider closure is incomplete' }
    function Test-Provider {
        foreach ($entry in $closure) {
            if ((Get-FileHash -LiteralPath (Join-Path $profile.InstalledOtpRoot $entry.Path)).Hash -cne $entry.Sha256) {
                throw 'An admitted provider file changed'
            }
        }
    }
    Test-Provider
    $directory = [IO.Path]::GetFullPath($Output)
    if (Test-Path -LiteralPath $directory) { throw 'Use a new output directory to preserve earlier evidence' }
    New-Item -ItemType Directory -Path $directory | Out-Null
    $relative = [IO.Path]::GetRelativePath($repository, $directory).Replace('\', '/')
    if ($relative.Contains('"') -or $relative.Contains("`n")) { throw 'Invalid output path' }
    $expression = '{ok,erts_internal_port_cases}=compile:file("tools/Validation/Erlang.ErtsInternalCompatibility/erts_internal_port_cases.erl",[{outdir,"' + $relative + '"}]),' +
        'true=code:add_patha("' + $relative + '"),ok=erts_internal_port_cases:write("' + $relative + '"),halt().'
    & (Join-Path $profile.InstalledOtpRoot 'bin/erl.exe') -noshell -eval $expression 2>&1 |
        Tee-Object -FilePath (Join-Path $directory 'oracle.log')
    if ($LASTEXITCODE -ne 0) { throw 'OTP capture failed' }
    Test-Provider
    $profile | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $directory 'provider.json') -Encoding utf8
    if ($CaptureOnly) { return }
    if (-not $RunnerDll) {
        dotnet build tools/Validation/Erlang.ErtsInternalCompatibility/Erlang.ErtsInternalCompatibility.csproj -m:1 --nologo
        if ($LASTEXITCODE -ne 0) { throw 'Focused validator build failed' }
        $RunnerDll = Join-Path $repository 'tools/Validation/Erlang.ErtsInternalCompatibility/bin/Debug/net10.0/Erlang.ErtsInternalCompatibility.dll'
    }
    $failedBackends = @()
    foreach ($backend in @('Managed', 'Beam')) {
        dotnet $RunnerDll $repository $directory $backend 2>&1 |
            Tee-Object -FilePath (Join-Path $directory ($backend + '.log'))
        if ($LASTEXITCODE -ne 0) { $failedBackends += $backend }
    }
    if ($failedBackends.Count -ne 0) {
        throw "Comparison failures in $($failedBackends -join ', '); retain the full reports"
    }
}
finally {
    Pop-Location
}
