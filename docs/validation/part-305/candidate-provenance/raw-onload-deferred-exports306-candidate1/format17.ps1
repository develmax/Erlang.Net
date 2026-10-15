$ErrorActionPreference = 'Stop'
$env:DOTNET_PROCESSOR_COUNT = '2'
$env:DOTNET_CLI_USE_MSBUILD_SERVER = '0'
$env:MSBUILDDISABLENODEREUSE = '1'
$taskRepo = 'D:\Distr\Work\Codex\Erlang.Net\Erlang.Net'
$taskBase = Join-Path $taskRepo 'artifacts/raw-onload-deferred-exports306-candidate1'
$taskPaths = @(Get-Content "$taskBase/paths17.json" -Raw | ConvertFrom-Json)
if ($taskPaths.Count -ne 17) { throw 'Expected seventeen CSharp source paths' }
$taskToken = Join-Path $taskRepo 'artifacts/spawn-protocol-pressure-integrated-isolated5/candidate/tests/PressureTokenGuard/bin/Debug/net10.0/PressureTokenGuard.dll'
$taskStyle = Join-Path $taskRepo 'tools/Erlang.Style/bin/Debug/net10.0/Erlang.Style.dll'
Push-Location "$taskBase/candidate"
try {
    & dotnet $taskToken "$taskBase/paths17.json" "$taskBase/tokens17-folder2-before.json" *> "$taskBase/tokens17-folder2-before.log"
    if ($LASTEXITCODE -ne 0) { throw 'Before token guard failed' }
    & dotnet format whitespace . --folder --include @taskPaths --verbosity minimal *> "$taskBase/sdk17-folder2-format.log"
    if ($LASTEXITCODE -ne 0) { throw 'SDK formatting failed' }
    $taskStage = Join-Path $taskBase 'style-owned17'
    foreach ($taskDirectory in @('src', 'tools', 'tests', 'benchmarks', 'examples')) { New-Item -ItemType Directory -Force "$taskStage/$taskDirectory" | Out-Null }
    foreach ($taskPath in $taskPaths) {
        New-Item -ItemType Directory -Force (Split-Path "$taskStage/$taskPath") | Out-Null
        Copy-Item -LiteralPath $taskPath -Destination "$taskStage/$taskPath"
    }
    & dotnet $taskStyle --write $taskStage *> "$taskBase/custom17-folder2-format.log"
    if ($LASTEXITCODE -ne 0) { throw 'Custom formatting failed' }
    foreach ($taskPath in $taskPaths) { Copy-Item -LiteralPath "$taskStage/$taskPath" -Destination $taskPath }
    & dotnet $taskToken "$taskBase/paths17.json" "$taskBase/tokens17-folder2-after.json" *> "$taskBase/tokens17-folder2-after.log"
    if ($LASTEXITCODE -ne 0) { throw 'After token guard failed' }
    if ((Get-FileHash "$taskBase/tokens17-folder2-before.json").Hash -ne (Get-FileHash "$taskBase/tokens17-folder2-after.json").Hash) { throw 'Formatting changed semantic tokens' }
    @{Status='PASS';SourcePaths=17;TokensExact=$true;TokenGuardSha256=(Get-FileHash $taskToken).Hash;StyleToolSha256=(Get-FileHash $taskStyle).Hash;MainEdited=$false;NativeExecuted=$false} | ConvertTo-Json | Set-Content "$taskBase/format17-folder2-guard.json"
    Get-Content "$taskBase/format17-folder2-guard.json"
} finally { Pop-Location }
