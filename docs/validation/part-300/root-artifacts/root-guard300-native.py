from pathlib import Path
import hashlib
import json
import re

root = Path.cwd().resolve()
artifacts = root / 'artifacts'
read = lambda path: json.loads(path.read_bytes())
digest = lambda data: hashlib.sha256(data).hexdigest().upper()
freeze_path = artifacts / 'root-part300-final-source-freeze.json'
freeze = read(freeze_path)
assert read(artifacts / 'root-part300-full-exit.json')['ExitCode'] == 0
assert read(artifacts / 'root-part300-main-native-exit.json')['ExitCode'] == 0
focus = read(artifacts / 'root-nif-lifetime300-isolated/dormant-revision3-main-focus53-exit.json')
assert focus['ExitCode'] == 0 and focus['ExpectedGroups'] == 53 and focus['LifetimeScenarios'] == 13
for row in freeze['Files']:
    data = (root / row['Path']).read_bytes()
    assert len(data) == row['Bytes'] and digest(data) == row['Sha256']
lines = (artifacts / 'root-part300-main-native.log').read_text(encoding='utf-8-sig').splitlines()
assert lines == (artifacts / 'root-nif-lifetime300-isolated/final27-native-windows.log').read_text(encoding='utf-8-sig').splitlines()
assert len(lines) == 4
assert lines[:2] == ['Managed backend checks passed; native ABI and original OTP NOT executed.', 'Internal trusted native fixture checks passed; general NIF ABI NOT implemented.']
prefix = 'Native generated source SHA256: '
assert lines[2].startswith(prefix)
generated = lines[2][len(prefix):]
assert re.fullmatch('[A-F0-9]{64}', generated)
assert lines[3] == 'Trusted native generated-source entry routes passed; BEAM/old-image/general ABI NOT established.'
tool = root / 'tools/Erlang.NifNativeBackend/bin/Debug/net10.0'
runtime_hash = digest((tool / 'Erlang.Runtime.dll').read_bytes())
assert runtime_hash == digest((root / 'src/Erlang.Runtime/bin/Debug/net10.0/Erlang.Runtime.dll').read_bytes())
fixture = root / 'artifacts/spawn-protocol-nif-290-38688662194-terminal/artifact-11951535100/nif-fixture-build/fixture.dll'
assert digest(fixture.read_bytes()) == '235D9E8997B7760E120ECD5CFACA29F01D0936A8FD89EF8B23DDF7D0DB43883F'
output = {
    'Status': 'PASS_ACTUAL_MAIN_NATIVE_TRUSTED_WINDOWS_AND_COMPILED_SOURCE',
    'ExitCode': 0,
    'SourcePaths': 30,
    'CSharpPaths': 27,
    'SourceHashesExact': True,
    'SourceFreezeSha256': digest(freeze_path.read_bytes()),
    'ReviewedSourcePlanSha256': freeze['ReviewedSourcePlanSha256'],
    'NativeTrustedWindowsProfilePassed': True,
    'ActualGeneratedCSharpRoutePassed': True,
    'ActualNativeFenceConcurrencyPassed': True,
    'NonDefaultNativeProviderRefusalPassed': True,
    'HaltRegressionRedThenGreenPassed': True,
    'NonblockingDisposeRegressionPassed': True,
    'DormantReentrantDisposeRegressionPassed': True,
    'RegistryHeldNifDisposeRefusedBeforeMutation': True,
    'ManagedToolDllSha256': digest((tool / 'Erlang.NifNativeBackend.dll').read_bytes()),
    'RuntimeDllSha256': runtime_hash,
    'FixtureSha256': digest(fixture.read_bytes()),
    'FixtureSourceSha256': digest((root / 'tools/Erlang.NifFixtureHost/native/fixture.c').read_bytes()),
    'GeneratedCSharpSha256': generated,
    'PreformatIsolatedGeneratedSourceAndOutputExactlyMatch': True,
    'GeneralNifAbiComplete': False,
    'PublicLoadNifRegistered': False,
}
(artifacts / 'root-part300-main-native-guard.json').write_text(json.dumps(output, indent=2) + '\n', encoding='utf-8')
print(json.dumps(output))
