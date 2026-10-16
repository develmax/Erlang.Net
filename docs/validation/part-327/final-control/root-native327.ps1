param([Parameter(Mandatory=$true)][ValidatePattern('^[A-Fa-f0-9]{64}$')][string]$FullGuardSha256)
$ErrorActionPreference = 'Stop'
$python = 'C:/Users/Maksim/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$guardPath = 'artifacts/root-part327-main1/full-guard.json'
if ((Get-FileHash -LiteralPath $guardPath -Algorithm SHA256).Hash -cne $FullGuardSha256.ToUpperInvariant()) { throw 'Exact actual full guard required' }
$full = Get-Content -LiteralPath $guardPath -Raw | ConvertFrom-Json
if ($full.Passed -ne 6729 -or $full.Failed -ne 0 -or $full.IntegrationResults -ne 11) { throw 'Full 6729 and eleven integrations required' }
$toolSha = (Get-FileHash -LiteralPath 'tools/Erlang.Tool/bin/Debug/net10.0/Erlang.Tool.dll' -Algorithm SHA256).Hash
$runtimeSha = (Get-FileHash -LiteralPath 'tools/Erlang.Tool/bin/Debug/net10.0/Erlang.Runtime.dll' -Algorithm SHA256).Hash
$nativeArguments = @(
    '--main-freeze-sha256', '380AF06807C90F6891C3C834A66573536310F23118954512E6A538E3E4BD8AD3',
    '--author-final-freeze-sha256', '464C81BB1624BC63CEA8B404ACAFEEF7246FFA745EDC3FC0D10D59C447593EDA',
    '--closure-sha256', '57275A459C17E8A6F991CE0FAD8C3FDE16F3DF0D6B132CF8A9C53D0EC9491C3F',
    '--full-guard-sha256', $FullGuardSha256.ToUpperInvariant(),
    '--source-peer', 'artifacts/root327-final-composition-putdata-peer.json',
    '--source-peer-sha256', '0228D7CA1B4C1E6A19B142A500648B2645A41D06DBC32667E83DA8F894DCC407',
    '--tool-sha256', $toolSha, '--runtime-sha256', $runtimeSha,
    '--output', 'artifacts/root-part327-main-windows-native1'
)
& $python -B artifacts/part327-main-native-proposal2/run327-main-native.py @nativeArguments
if ($LASTEXITCODE -ne 0) { throw 'Fresh native probe execution failed; preserve outputs' }
& $python -B artifacts/part327-main-native-proposal2/guard327-main-native.py @nativeArguments
if ($LASTEXITCODE -ne 0) { throw 'Actual native proof verification failed; preserve outputs' }
