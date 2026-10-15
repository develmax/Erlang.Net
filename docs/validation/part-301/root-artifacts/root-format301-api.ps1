$ErrorActionPreference = 'Stop'
$env:DOTNET_PROCESSOR_COUNT = '2'
$env:DOTNET_CLI_USE_MSBUILD_SERVER = '0'
$env:MSBUILDDISABLENODEREUSE = '1'
$repo = (Get-Location).Path
$base = Join-Path $repo 'artifacts/root-process-info301-isolated'
$paths = @(Get-Content "$base/paths8.json" -Raw | ConvertFrom-Json)
if ($paths.Count -ne 8) { throw 'Expected eight API source paths' }
$token = Join-Path $repo 'artifacts/spawn-protocol-pressure-integrated-isolated5/candidate/tests/PressureTokenGuard/bin/Debug/net10.0/PressureTokenGuard.dll'
Push-Location "$base/candidate"
try {
    & dotnet $token "$base/paths8.json" "$base/tokens8-before.json"
    if ($LASTEXITCODE -ne 0) { throw 'Before token guard failed' }
    & dotnet format whitespace tests/Erlang.Tests/Erlang.Tests.csproj --no-restore --include @paths --verbosity minimal *> "$base/sdk8-format.log"
    if ($LASTEXITCODE -ne 0) { throw 'SDK formatting failed' }
    $stage = Join-Path $base 'style-owned8'
    foreach ($directory in @('src', 'tools', 'tests', 'benchmarks', 'examples')) { New-Item -ItemType Directory -Force "$stage/$directory" | Out-Null }
    foreach ($path in $paths) {
        New-Item -ItemType Directory -Force (Split-Path "$stage/$path") | Out-Null
        Copy-Item -LiteralPath $path -Destination "$stage/$path"
    }
    & dotnet "$repo/tools/Erlang.Style/bin/Debug/net10.0/Erlang.Style.dll" --write $stage *> "$base/custom8-format.log"
    if ($LASTEXITCODE -ne 0) { throw 'Custom formatting failed' }
    foreach ($path in $paths) { Copy-Item -LiteralPath "$stage/$path" -Destination $path }
    & dotnet $token "$base/paths8.json" "$base/tokens8-after.json"
    if ($LASTEXITCODE -ne 0) { throw 'After token guard failed' }
    if ((Get-FileHash "$base/tokens8-before.json").Hash -ne (Get-FileHash "$base/tokens8-after.json").Hash) { throw 'Formatting changed semantic tokens' }
    @{Status='PASS'; SourcePaths=8; TokensExact=$true; PreformatHostGroups=3} | ConvertTo-Json | Set-Content "$base/format8-guard.json"
    Get-Content "$base/format8-guard.json"
} finally { Pop-Location }
