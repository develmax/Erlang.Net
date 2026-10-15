$ErrorActionPreference = 'Stop'
$env:DOTNET_PROCESSOR_COUNT = '2'
$env:DOTNET_CLI_USE_MSBUILD_SERVER = '0'
$env:MSBUILDDISABLENODEREUSE = '1'
$taskRepo = 'D:\Distr\Work\Codex\Erlang.Net\Erlang.Net'
$taskBase = Join-Path $taskRepo 'artifacts/root-nif-source-caller303-isolated'
$taskPaths = @(Get-Content "$taskBase/paths11.json" -Raw | ConvertFrom-Json)
if ($taskPaths.Count -ne 11) { throw 'Expected eleven CSharp source paths' }
$taskToken = Join-Path $taskRepo 'artifacts/spawn-protocol-pressure-integrated-isolated5/candidate/tests/PressureTokenGuard/bin/Debug/net10.0/PressureTokenGuard.dll'
$taskStyle = Join-Path $taskRepo 'tools/Erlang.Style/bin/Debug/net10.0/Erlang.Style.dll'
Push-Location "$taskBase/candidate"
try {
    & dotnet $taskToken "$taskBase/paths11.json" "$taskBase/tokens11-folder-before.json" *> "$taskBase/tokens11-folder-before.log"
    if ($LASTEXITCODE -ne 0) { throw 'Before token guard failed' }
    & dotnet format whitespace . --folder --no-restore --include @taskPaths --verbosity minimal *> "$taskBase/sdk11-folder-format.log"
    if ($LASTEXITCODE -ne 0) { throw 'SDK formatting failed' }
    $taskStage = Join-Path $taskBase 'style-owned11'
    foreach ($taskDirectory in @('src', 'tools', 'tests', 'benchmarks', 'examples')) { New-Item -ItemType Directory -Force "$taskStage/$taskDirectory" | Out-Null }
    foreach ($taskPath in $taskPaths) {
        New-Item -ItemType Directory -Force (Split-Path "$taskStage/$taskPath") | Out-Null
        Copy-Item -LiteralPath $taskPath -Destination "$taskStage/$taskPath"
    }
    & dotnet $taskStyle --write $taskStage *> "$taskBase/custom11-folder-format.log"
    if ($LASTEXITCODE -ne 0) { throw 'Custom formatting failed' }
    foreach ($taskPath in $taskPaths) { Copy-Item -LiteralPath "$taskStage/$taskPath" -Destination $taskPath }
    & dotnet $taskToken "$taskBase/paths11.json" "$taskBase/tokens11-folder-after.json" *> "$taskBase/tokens11-folder-after.log"
    if ($LASTEXITCODE -ne 0) { throw 'After token guard failed' }
    if ((Get-FileHash "$taskBase/tokens11-folder-before.json").Hash -ne (Get-FileHash "$taskBase/tokens11-folder-after.json").Hash) { throw 'Formatting changed semantic tokens' }
    @{Status='PASS';SourcePaths=11;TokensExact=$true;TokenGuardSha256=(Get-FileHash $taskToken).Hash;StyleToolSha256=(Get-FileHash $taskStyle).Hash;MainEdited=$false;NativeExecuted=$false} | ConvertTo-Json | Set-Content "$taskBase/format11-folder-guard.json"
    Get-Content "$taskBase/format11-folder-guard.json"
} finally { Pop-Location }
