param(
    [string]$Admission = 'artifacts/library376-official-compiler/official-admission.json',
    [string]$Output = 'artifacts/file-compatibility',
    [string]$RunnerDll,
    [switch]$CaptureOnly
)

$ErrorActionPreference = 'Stop'
$repository = Split-Path $PSScriptRoot -Parent
$otpTagPrefix = 'OTP-'
$minimumOracleClosureEntries = 100
$completeCycleCaseCount = 5
$messages = @{
    Provider = 'File comparisons require the admitted pinned official OTP provider.'
    Closure = 'Incomplete file oracle closure.'
    ChangedInput = 'File oracle input changed: '
    ExistingOutput = 'Choose a new output directory.'
    OutputPath = 'Unsupported output path.'
    Capture = 'File oracle capture failed.'
    Runner = 'Build and supply RunnerDll, or use CaptureOnly.'
    Differences = 'File comparison has unresolved differences; inspect both reports.'
}
Push-Location $repository
try {
    $baseline = Get-Content -LiteralPath 'reference-baseline.json' -Raw | ConvertFrom-Json
    $expectedVersion = $baseline.tag.Substring($otpTagPrefix.Length)
    $profile = Get-Content -LiteralPath $Admission -Raw | ConvertFrom-Json
    if ($profile.ReleaseVersion -cne $expectedVersion -or
        $profile.SourceCommit -cne $baseline.commit -or
        -not $profile.EveryExtractedFileMatchesArchive -or
        (Get-FileHash -LiteralPath $profile.ExecutionManifestPath -Algorithm SHA256).Hash -cne $profile.ExecutionManifestSha256) {
        throw $messages.Provider
    }
    $inventory = Get-Content -LiteralPath $profile.ExecutionManifestPath -Raw | ConvertFrom-Json
    $closure = @($inventory | Where-Object {
        $_.Path -eq 'bin/erl.exe' -or
        $_.Path -match '^erts-[^/]+/bin/(erl.exe|beam.smp.dll)$' -or
        $_.Path -match '^lib/(kernel-[^/]+|stdlib-[^/]+|erts-[^/]+)/(ebin|src|include)/'
    })
    if ($closure.Count -lt $minimumOracleClosureEntries) { throw $messages.Closure }
    function Test-FileOracleClosure {
        foreach ($entry in $closure) {
            if ((Get-FileHash -LiteralPath (Join-Path $profile.InstalledOtpRoot $entry.Path) -Algorithm SHA256).Hash -cne $entry.Sha256) {
                throw ($messages.ChangedInput + $entry.Path)
            }
        }
    }
    Test-FileOracleClosure
    $directory = [IO.Path]::GetFullPath($Output)
    if (Test-Path -LiteralPath $directory) { throw $messages.ExistingOutput }
    New-Item -ItemType Directory -Path $directory | Out-Null
    $relative = [IO.Path]::GetRelativePath($repository, $directory).Replace('\', '/')
    if ($relative.Contains('"') -or $relative.Contains("`n")) { throw $messages.OutputPath }
    $expression = '{ok,file_port_device}=compile:file("tools/Validation/Erlang.FileCompatibility/file_port_device.erl",[{outdir,"' + $relative + '"}]),' +
        '{ok,file_port_cases}=compile:file("tools/Validation/Erlang.FileCompatibility/file_port_cases.erl",[{outdir,"' + $relative + '"}]),' +
        'true=code:add_patha("' + $relative + '"),ok=file_port_cases:write("' + $relative + '"),halt().'
    & (Join-Path $profile.InstalledOtpRoot 'bin/erl.exe') -noshell -eval $expression 2>&1 |
        Tee-Object -FilePath (Join-Path $directory 'oracle.log')
    if ($LASTEXITCODE -ne 0) { throw $messages.Capture }
    Test-FileOracleClosure
    $inputs = @(
        'tools/Validation/Erlang.FileCompatibility/file_port_cases.erl',
        'tools/Validation/Erlang.FileCompatibility/file_port_device.erl',
        'src/Otp/Original/Foundation/Erlang.Otp.Kernel.Beam/Otp/src/file.erl',
        'src/Otp/Original/Foundation/Erlang.Otp.Kernel.Beam/Otp/ebin/file.beam'
    )
    $provenance = [ordered]@{
        Baseline = $profile.ReleaseTag
        Commit = $profile.SourceCommit
        ExecutionManifestSha256 = $profile.ExecutionManifestSha256
        OracleInputs = $closure
        Inputs = @($inputs | ForEach-Object { @{ Path = $_; Sha256 = (Get-FileHash -LiteralPath $_).Hash } })
        Reports = @('original.etf', 'runtime-gates.etf', 'exports.etf', 'file_port_cases.beam', 'file_port_device.beam') |
            ForEach-Object { @{ Path = $_; Sha256 = (Get-FileHash -LiteralPath (Join-Path $directory $_)).Hash } }
    }
    $provenance | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $directory 'provenance.json') -Encoding utf8
    if (-not $CaptureOnly) {
        if (-not $RunnerDll -or -not (Test-Path -LiteralPath $RunnerDll)) { throw $messages.Runner }
        $failed = $false
        foreach ($backend in @('Managed', 'Beam')) {
            & dotnet $RunnerDll $repository $directory $backend 'original.etf'
            if ($LASTEXITCODE -ne 0) { $failed = $true }
            foreach ($row in 0..($completeCycleCaseCount - 1)) {
                & dotnet $RunnerDll $repository $directory $backend 'runtime-gates.etf' $row
                if ($LASTEXITCODE -ne 0) { $failed = $true }
            }
        }
        if ($failed) { throw $messages.Differences }
    }
}
finally {
    Pop-Location
}
