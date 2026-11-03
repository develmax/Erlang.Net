param(
    [string]$Admission = 'artifacts/library376-official-compiler/official-admission.json',
    [string]$Output = 'artifacts/string-compatibility'
)

$ErrorActionPreference = 'Stop'
$repository = Split-Path $PSScriptRoot -Parent
Push-Location $repository
try {
    $outputPath = [IO.Path]::GetFullPath($Output)
    if (Test-Path -LiteralPath $outputPath) {
        throw 'Choose a new output directory to preserve previous comparison evidence.'
    }
    $profile = Get-Content -LiteralPath $Admission -Raw | ConvertFrom-Json
    if ($profile.ReleaseVersion -cne '29.1.1' -or
        $profile.SourceCommit -cne 'ad05823719d77c8faee87348ea39513d4e2f99c5' -or
        -not $profile.EveryExtractedFileMatchesArchive) {
        throw 'The string oracle requires the admitted official OTP 29.1.1 provider.'
    }
    if ((Get-FileHash -LiteralPath $profile.ExecutionManifestPath -Algorithm SHA256).Hash -cne $profile.ExecutionManifestSha256) {
        throw 'The oracle execution manifest changed.'
    }
    $inventory = Get-Content -LiteralPath $profile.ExecutionManifestPath -Raw | ConvertFrom-Json
    $selected = @($inventory | Where-Object {
        $_.Path -eq 'bin/erl.exe' -or
        $_.Path -match '^erts-[^/]+/bin/(erl.exe|beam.smp.dll)$' -or
        $_.Path -match '^lib/(stdlib-8.1|erts-17.1)/(ebin|src)/(string|unicode|unicode_util|lists|maps|erts_internal)\.(beam|erl)$'
    })
    if ($selected.Count -lt 12) {
        throw 'The original string execution closure is incomplete.'
    }
    function Test-OriginalClosure {
        foreach ($entry in $selected) {
            $path = Join-Path $profile.InstalledOtpRoot $entry.Path
            if ((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -cne $entry.Sha256) {
                throw "Original execution input changed: $($entry.Path)"
            }
        }
    }
    Test-OriginalClosure
    New-Item -ItemType Directory -Path $outputPath | Out-Null
    $fixture = 'tools/Validation/Erlang.StringCompatibility/string_port_cases.erl'
    $relativeOutput = [IO.Path]::GetRelativePath($repository, $outputPath).Replace('\', '/')
    if ($relativeOutput.Contains('"') -or $relativeOutput.Contains("`n")) {
        throw 'Unsupported output path.'
    }
    $expression = '{ok,string_port_cases}=compile:file("' + $fixture + '",[{outdir,"' + $relativeOutput + '"}]),' +
        'true=code:add_patha("' + $relativeOutput + '"),' +
        'ok=string_port_cases:write("' + $relativeOutput + '"),halt().'
    & (Join-Path $profile.InstalledOtpRoot 'bin/erl.exe') -noshell -eval $expression 2>&1 |
        Tee-Object -FilePath (Join-Path $outputPath 'oracle.log')
    if ($LASTEXITCODE -ne 0) {
        throw 'Original OTP string acquisition failed.'
    }
    dotnet build tools/Validation/Erlang.StringCompatibility -m:1 -v:q 2>&1 |
        Tee-Object -FilePath (Join-Path $outputPath 'build.log')
    if ($LASTEXITCODE -ne 0) {
        throw 'String comparison runner build failed.'
    }
    $failed = $false
    foreach ($backend in @('managed', 'beam')) {
        dotnet run --project tools/Validation/Erlang.StringCompatibility --no-build -- $repository $outputPath $backend 2>&1 |
            Tee-Object -FilePath (Join-Path $outputPath ($backend + '.log'))
        $failed = $failed -or $LASTEXITCODE -ne 0
    }
    Test-OriginalClosure
    [ordered]@{
        Baseline = $profile.SourceCommit
        ReleaseVersion = $profile.ReleaseVersion
        FixtureSha256 = (Get-FileHash -LiteralPath $fixture -Algorithm SHA256).Hash
        OriginalInputs = $selected
        ProviderUnchangedAfterExecution = $true
    } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $outputPath 'provenance.json') -Encoding utf8
    if ($failed) {
        throw 'A string backend has compatibility failures; inspect both retained reports.'
    }
}
finally {
    Pop-Location
}
