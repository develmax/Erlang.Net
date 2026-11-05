param(
    [ValidateSet('x64', 'x86')][string]$Architecture,
    [string]$RuntimeHost = 'dotnet'
)
$ErrorActionPreference = 'Stop'
$area = 'artifacts/nif-windows-abi'
New-Item -ItemType Directory $area -Force | Out-Null
$digest = (Get-FileHash src/Runtime/Erlang.Runtime.Core/Native/Nif/native/windows_varargs526.c).Hash
python tools/Native/Erlang.NifNativeBackend/build-windows-varargs526.py --repository . --source-sha256 $digest --output "$area/native" --architecture $Architecture --run-smoke
if ($LASTEXITCODE) { exit $LASTEXITCODE }
git clone --depth 1 --branch OTP-29.1.1 https://github.com/erlang/otp.git "$area/otp"
if ($LASTEXITCODE) { exit $LASTEXITCODE }
python tools/Native/Erlang.NifPlatformValidation/build-windows-fixture.py --repository . --otp-source "$area/otp" --output "$area/image" --architecture $Architecture
if ($LASTEXITCODE) { exit $LASTEXITCODE }
dotnet build tools/Native/Erlang.NifPlatformValidation -m:1 -p:UseAppHost=false "-p:ErlangNifRuntimeIdentifier=win-$Architecture"
if ($LASTEXITCODE) { exit $LASTEXITCODE }
$driver = 'tools/Native/Erlang.NifPlatformValidation/bin/Debug/net10.0/Erlang.NifNativeBackend.dll'
& $RuntimeHost $driver --abi-layout "$area/image/abi-layout.json" > "$area/abi-checks.json"
if ($LASTEXITCODE) { exit $LASTEXITCODE }
& $RuntimeHost $driver --windows-adapter "$area/native/Erlang.Nif.WindowsVariadic.dll" "$area/native/Erlang.Nif.VariadicProbe.dll" > "$area/adapter-checks.json"
if ($LASTEXITCODE) { exit $LASTEXITCODE }
& $RuntimeHost $driver --windows-adapter "src/Runtime/Erlang.Runtime.Core/runtimes/win-$Architecture/native/Erlang.Nif.WindowsVariadic.dll" "$area/native/Erlang.Nif.VariadicProbe.dll" > "$area/shipped-checks.json"
if ($LASTEXITCODE) { exit $LASTEXITCODE }
& $RuntimeHost $driver --windows-entry "$area/image/nif_platform_fixture.dll" > "$area/entry-checks.json"
if ($LASTEXITCODE) { exit $LASTEXITCODE }
$abi = Get-Content "$area/abi-checks.json" -Raw | ConvertFrom-Json
if ($abi.Architecture -ine $Architecture) { throw 'ABI checks ran in another process architecture' }
if ($Architecture -eq 'x86') {
    if ($abi.NativeAdmission -or $abi.PointerBytes -ne 4) { throw 'Unverified 32-bit VM admission was enabled' }
    & $RuntimeHost $driver --runtime-gate > "$area/runtime-gate.json"
    if ($LASTEXITCODE) { exit $LASTEXITCODE }
} else {
    & $RuntimeHost $driver --image tools/Native/Erlang.NifPlatformValidation/beam/nif_platform_fixture.beam "$area/image/nif_platform_fixture.dll" "$area/image/managed.etf" > "$area/image-checks.json"
    if ($LASTEXITCODE) { exit $LASTEXITCODE }
    python -c "from pathlib import Path; assert Path('$area/image/managed.etf').read_bytes()==Path('tools/Native/Erlang.NifPlatformValidation/beam/original.etf').read_bytes()"
    if ($LASTEXITCODE) { exit $LASTEXITCODE }
}
