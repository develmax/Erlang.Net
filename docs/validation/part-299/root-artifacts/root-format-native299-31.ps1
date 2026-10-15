$ErrorActionPreference='Stop'
$env:DOTNET_PROCESSOR_COUNT='2'
$env:DOTNET_CLI_USE_MSBUILD_SERVER='0'
$env:MSBUILDDISABLENODEREUSE='1'
$repo=(Get-Location).Path
$base=Join-Path $repo 'artifacts/root-nif-native299-isolated'
$candidate=Join-Path $base 'candidate'
$paths=@(Get-Content "$base/paths31.json" -Raw|ConvertFrom-Json)
$paths|ConvertTo-Json|Set-Content "$base/paths31.json"
$token=Join-Path $repo 'artifacts/spawn-protocol-pressure-integrated-isolated5/candidate/tests/PressureTokenGuard/bin/Debug/net10.0/PressureTokenGuard.dll'
Push-Location $candidate
try {
 & dotnet $token "$base/paths31.json" "$base/tokens31-before.json"
 if($LASTEXITCODE -ne 0){throw 'Before tokens failed'}
 & dotnet format whitespace tools/Erlang.NifNativeBackend/Erlang.NifNativeBackend.csproj --no-restore --include @paths --verbosity minimal *> "$base/sdk31-format.log"
 if($LASTEXITCODE -ne 0){throw 'SDK format failed'}
 $stage=Join-Path $base 'style-owned31'
 foreach($d in @('src','tools','tests','benchmarks','examples')){New-Item -ItemType Directory -Force "$stage/$d"|Out-Null}
 foreach($p in $paths){New-Item -ItemType Directory -Force (Split-Path "$stage/$p")|Out-Null;Copy-Item -LiteralPath $p -Destination "$stage/$p"}
 & dotnet "$repo/tools/Erlang.Style/bin/Debug/net10.0/Erlang.Style.dll" --write $stage *> "$base/custom31-format.log"
 if($LASTEXITCODE -ne 0){throw 'Custom format failed'}
 foreach($p in $paths){Copy-Item -LiteralPath "$stage/$p" -Destination $p}
 & dotnet $token "$base/paths31.json" "$base/tokens31-after.json"
 if($LASTEXITCODE -ne 0){throw 'After tokens failed'}
 if((Get-FileHash "$base/tokens31-before.json").Hash -ne (Get-FileHash "$base/tokens31-after.json").Hash){throw 'Token mismatch'}
 @{Status='PASS';SourcePaths=$paths.Count;TokensExact=$true;ManagedAndWindowsNativePassedBeforeFormatting=$true}|ConvertTo-Json|Set-Content "$base/format31-guard.json"
 Get-Content "$base/format31-guard.json"
} finally {Pop-Location}


