param([ValidateSet('x64', 'arm64')][string]$Architecture = 'arm64')
$ErrorActionPreference = 'Stop'
$artifactRoot = 'artifacts/nif-platform-ci'
$adapter = "$artifactRoot/native/Erlang.Nif.WindowsVariadic.dll"
$shipped = "$artifactRoot/shipped/Erlang.Nif.WindowsVariadic.dll"
New-Item -ItemType Directory "$artifactRoot/shipped" -Force | Out-Null
Copy-Item "src/Runtime/Erlang.Runtime.Core/runtimes/win-$Architecture/native/Erlang.Nif.WindowsVariadic.dll" $shipped
$sourceDigest = (Get-FileHash src/Runtime/Erlang.Runtime.Core/Native/Nif/native/windows_varargs526.c).Hash
python tools/Native/Erlang.NifNativeBackend/build-windows-varargs526.py --repository . --source-sha256 $sourceDigest --output "$artifactRoot/native" --architecture $Architecture --run-smoke
if ($LASTEXITCODE) { exit $LASTEXITCODE }
Copy-Item $adapter "$adapter.rebuilt"
try {
    Copy-Item $shipped $adapter
    & "$artifactRoot/native/smoke.exe" > "$artifactRoot/shipped/native-smoke.log"
    if ($LASTEXITCODE) { throw 'The committed native adapter failed its target C varargs smoke' }
} finally {
    Copy-Item "$adapter.rebuilt" $adapter
}
Copy-Item $adapter "src/Runtime/Erlang.Runtime.Core/runtimes/win-$Architecture/native/Erlang.Nif.WindowsVariadic.dll"
dotnet build tools/Native/Erlang.NifPlatformValidation -m:1 "-p:ErlangNifRuntimeIdentifier=win-$Architecture"
if ($LASTEXITCODE) { exit $LASTEXITCODE }
$driver = 'tools/Native/Erlang.NifPlatformValidation/bin/Debug/net10.0/Erlang.NifNativeBackend.dll'
dotnet $driver $adapter > "$artifactRoot/managed-checks.json"
if ($LASTEXITCODE) { exit $LASTEXITCODE }
dotnet $driver $shipped > "$artifactRoot/shipped/managed-checks.json"
if ($LASTEXITCODE) { exit $LASTEXITCODE }
$report = Get-Content "$artifactRoot/managed-checks.json" -Raw | ConvertFrom-Json
if ($report.Architecture -ine $Architecture) { throw 'The checks did not execute in the requested native .NET architecture' }
python -c "import hashlib,json,pathlib; p=pathlib.Path('tools/Native/Erlang.NifPlatformValidation'); v=json.loads((p/'beam/provenance.json').read_text()); h=lambda q:hashlib.sha256(q.read_bytes()).hexdigest().upper(); assert h(p/'beam/nif_platform_fixture.beam')==v['BeamSha256']; assert h(p/'nif_platform_fixture.erl')==v['ErlangSourceSha256']; assert h(p/'nif_platform_fixture.c')==v['CSourceSha256']; assert h(p/'beam/original.etf')==v['OriginalEtfSha256']"
if ($LASTEXITCODE) { exit $LASTEXITCODE }
git clone --depth 1 --branch OTP-29.1.1 https://github.com/erlang/otp.git "$artifactRoot/otp"
if ($LASTEXITCODE) { exit $LASTEXITCODE }
python tools/Native/Erlang.NifPlatformValidation/build-windows-fixture.py --repository . --otp-source "$artifactRoot/otp" --output "$artifactRoot/image" --architecture $Architecture
if ($LASTEXITCODE) { exit $LASTEXITCODE }
dotnet $driver --image tools/Native/Erlang.NifPlatformValidation/beam/nif_platform_fixture.beam "$artifactRoot/image/nif_platform_fixture.dll" "$artifactRoot/image/managed.etf" > "$artifactRoot/image/managed-checks.json"
if ($LASTEXITCODE) { exit $LASTEXITCODE }
python -c "import pathlib; p=pathlib.Path('tools/Native/Erlang.NifPlatformValidation'); assert (p/'beam/original.etf').read_bytes()==pathlib.Path('artifacts/nif-platform-ci/image/managed.etf').read_bytes()"
if ($LASTEXITCODE) { exit $LASTEXITCODE }
