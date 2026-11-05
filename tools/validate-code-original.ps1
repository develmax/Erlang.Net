param(
    [string]$Admission = 'artifacts/library376-official-compiler/official-admission.json',
    [string]$Output = 'artifacts/code-original'
)

$ErrorActionPreference = 'Stop'
$repository = Split-Path $PSScriptRoot -Parent
Push-Location $repository
try {
    $profile = Get-Content -LiteralPath $Admission -Raw | ConvertFrom-Json
    if ($profile.ReleaseVersion -cne '29.1.1' -or
        $profile.SourceCommit -cne 'ad05823719d77c8faee87348ea39513d4e2f99c5' -or
        -not $profile.EveryExtractedFileMatchesArchive -or
        (Get-FileHash -LiteralPath $profile.ExecutionManifestPath).Hash -cne $profile.ExecutionManifestSha256) {
        throw 'Code comparison requires the admitted OTP 29.1.1 provider.'
    }
    $inventory = Get-Content -LiteralPath $profile.ExecutionManifestPath -Raw | ConvertFrom-Json
    $inputs = @($inventory | Where-Object {
        $_.Path -eq 'bin/erl.exe' -or
        $_.Path -match '^erts-[^/]+/bin/(erl.exe|beam.smp.dll)$' -or
        $_.Path -match '^lib/kernel-[^/]+/(ebin|src)/(code|code_server)\.(beam|erl)$'
    })
    if ($inputs.Count -lt 7) { throw 'Code oracle identity closure is incomplete.' }
    function Test-CodeInputs {
        foreach ($entry in $inputs) {
            if ((Get-FileHash -LiteralPath (Join-Path $profile.InstalledOtpRoot $entry.Path)).Hash -cne $entry.Sha256) {
                throw 'Code oracle input changed.'
            }
        }
    }
    Test-CodeInputs
    $directory = [IO.Path]::GetFullPath($Output)
    if (Test-Path -LiteralPath $directory) { throw 'Choose an unused code capture directory.' }
    New-Item -ItemType Directory -Path $directory | Out-Null
    $relative = [IO.Path]::GetRelativePath($repository, $directory).Replace('\', '/')
    if ($relative.Contains('"') -or $relative.Contains("`n")) { throw 'Unsupported output path.' }
    $source = 'tests/Erlang.Tests/Resources/CodeBackend/code_port_cases.erl'
    $compiler = '{ok,code_port_cases}=compile:file("' + $source + '",[{outdir,"' + $relative + '"}]),halt().'
    $executable = Join-Path $profile.InstalledOtpRoot 'bin/erl.exe'
    & $executable -noshell -eval $compiler 2>&1 | Tee-Object -FilePath (Join-Path $directory 'compile.log')
    if ($LASTEXITCODE -ne 0) { throw 'Code observer compilation failed.' }
    $subject = '{ok,code_port_loaded}=compile:file("tests/Erlang.Tests/Resources/CodeBackend/code_port_loaded.erl",' +
        '[{outdir,"' + $relative + '"}]),halt().'
    & $executable -noshell -eval $subject 2>&1 | Tee-Object -FilePath (Join-Path $directory 'compile-subject.log')
    if ($LASTEXITCODE -ne 0) { throw 'Code subject compilation failed.' }
    $capture = '{module,code_port_cases}=code:load_abs("' + $relative + '/code_port_cases"),' +
        'ok=code_port_cases:write("' + $relative + '"),ok=code_port_cases:write_lifecycle("' + $relative + '"),halt().'
    & $executable -noshell -mode embedded -eval $capture 2>&1 | Tee-Object -FilePath (Join-Path $directory 'capture.log')
    if ($LASTEXITCODE -ne 0) { throw 'Code oracle capture failed.' }
    Test-CodeInputs
    [ordered]@{
        Baseline = $profile.ReleaseTag
        Commit = $profile.SourceCommit
        ExecutionManifestSha256 = $profile.ExecutionManifestSha256
        Inputs = $inputs
        ObserverSourceSha256 = (Get-FileHash -LiteralPath $source).Hash
        ObserverBeamSha256 = (Get-FileHash -LiteralPath (Join-Path $directory 'code_port_cases.beam')).Hash
        CorpusSha256 = (Get-FileHash -LiteralPath (Join-Path $directory 'contracts.etf')).Hash
        ExportsSha256 = (Get-FileHash -LiteralPath (Join-Path $directory 'exports.etf')).Hash
        SubjectBeamSha256 = (Get-FileHash -LiteralPath (Join-Path $directory 'code_port_loaded.beam')).Hash
        LifecycleSha256 = (Get-FileHash -LiteralPath (Join-Path $directory 'lifecycle.etf')).Hash
        Mode = 'embedded'
        InitialCodePath = @()
    } | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $directory 'provenance.json') -Encoding utf8
}
finally {
    Pop-Location
}
