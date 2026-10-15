$ErrorActionPreference = 'Stop'
$env:DOTNET_PROCESSOR_COUNT = '2'
$env:DOTNET_CLI_USE_MSBUILD_SERVER = '0'
$env:MSBUILDDISABLENODEREUSE = '1'
$repo = (Get-Location).Path
$base = Join-Path $repo 'artifacts/root-nif-lifetime300-isolated'
$paths = @('tests/Erlang.Tests/Program.cs', 'tests/Erlang.Tests/NifLifetimeHostChecks.RetirementHalt.cs', 'tests/Erlang.Tests/NifRetirementHaltHostNames.cs')
foreach ($directory in @('nif-owned-lifetime299-overlay', 'nif-native-lifecycle-fence300-overlay', 'nif-native-lifecycle-default-alc300-revision1', 'nif-native-fence-controls300-overlay')) {
    $freeze = Get-Content "$repo/artifacts/$directory/source-freeze.json" -Raw | ConvertFrom-Json
    $paths += @($freeze.Files.Path)
}
$paths = @($paths | Sort-Object -Unique)
if ($paths.Count -ne 27) { throw 'Expected 27 composed CSharp source paths' }
$paths | ConvertTo-Json | Set-Content "$base/paths27.json"
$token = Join-Path $repo 'artifacts/spawn-protocol-pressure-integrated-isolated5/candidate/tests/PressureTokenGuard/bin/Debug/net10.0/PressureTokenGuard.dll'
Push-Location "$base/candidate"
try {
    & dotnet $token "$base/paths27.json" "$base/tokens27-before.json"
    if ($LASTEXITCODE -ne 0) { throw 'Before token guard failed' }
    & dotnet format whitespace tests/Erlang.Tests/Erlang.Tests.csproj --no-restore --include @paths --verbosity minimal *> "$base/sdk27-format.log"
    if ($LASTEXITCODE -ne 0) { throw 'SDK formatting failed' }
    $stage = Join-Path $base 'style-owned27'
    foreach ($directory in @('src', 'tools', 'tests', 'benchmarks', 'examples')) {
        New-Item -ItemType Directory -Force "$stage/$directory" | Out-Null
    }
    foreach ($path in $paths) {
        New-Item -ItemType Directory -Force (Split-Path "$stage/$path") | Out-Null
        Copy-Item -LiteralPath $path -Destination "$stage/$path"
    }
    & dotnet "$repo/tools/Erlang.Style/bin/Debug/net10.0/Erlang.Style.dll" --write $stage *> "$base/custom27-format.log"
    if ($LASTEXITCODE -ne 0) { throw 'Custom formatting failed' }
    foreach ($path in $paths) { Copy-Item -LiteralPath "$stage/$path" -Destination $path }
    & dotnet $token "$base/paths27.json" "$base/tokens27-after.json"
    if ($LASTEXITCODE -ne 0) { throw 'After token guard failed' }
    if ((Get-FileHash "$base/tokens27-before.json").Hash -ne (Get-FileHash "$base/tokens27-after.json").Hash) { throw 'Formatting changed semantic tokens' }
    @{Status='PASS'; SourcePaths=27; TokensExact=$true; PreformatFocusGroups=52} | ConvertTo-Json | Set-Content "$base/format27-guard.json"
    Get-Content "$base/format27-guard.json"
} finally { Pop-Location }

