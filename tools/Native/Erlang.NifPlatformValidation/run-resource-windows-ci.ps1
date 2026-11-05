param(
    [ValidateSet('x86','x64')][string]$Architecture,
    [ValidateSet('Debug','Release')][string]$Configuration,
    [string]$RuntimeHost = 'dotnet'
)
$ErrorActionPreference = 'Stop'
$area = 'artifacts/nif-resource-ci'
New-Item -ItemType Directory $area -Force | Out-Null
git clone --depth 1 --branch OTP-29.1.1 https://github.com/erlang/otp.git "$area/otp"
if ($LASTEXITCODE) { exit $LASTEXITCODE }
python tools/Native/Erlang.NifPlatformValidation/build-windows-fixture.py --repository . --otp-source "$area/otp" --output "$area/image" --architecture $Architecture --resource-fixture
if ($LASTEXITCODE) { exit $LASTEXITCODE }
dotnet build tools/Native/Erlang.NifPlatformValidation -m:1 -c $Configuration -p:UseAppHost=false "-p:ErlangNifRuntimeIdentifier=win-$Architecture"
if ($LASTEXITCODE) { exit $LASTEXITCODE }
$driver = "tools/Native/Erlang.NifPlatformValidation/bin/$Configuration/net10.0/Erlang.NifNativeBackend.dll"
foreach ($mode in @('resource-storage','resource-guest','word-storage')) {
    & $RuntimeHost $driver "--$mode" > "$area/$mode.json"
    if ($LASTEXITCODE) { exit $LASTEXITCODE }
}
& $RuntimeHost $driver --resource-image artifacts/resource-oracle32/nif_resource_fixture.beam "$area/image/nif_resource_fixture.dll" "$area/managed.etf" > "$area/image-checks.json"
if ($LASTEXITCODE) { exit $LASTEXITCODE }
python tools/Native/Erlang.NifPlatformValidation/verify-resource-parity.py --repository . --observed "$area/managed.etf" > "$area/parity.json"
if ($LASTEXITCODE) { exit $LASTEXITCODE }
