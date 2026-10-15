from pathlib import Path
import hashlib
import json

root = Path.cwd().resolve()
artifacts = root / 'artifacts'
read = lambda path: json.loads(path.read_bytes())
digest = lambda data: hashlib.sha256(data).hexdigest().upper()
freeze_path = artifacts / 'root-part306-final-source-freeze2.json'
freeze = read(freeze_path)
assert read(artifacts / 'root-part306-full-exit.json')['ExitCode'] == 0
assert read(artifacts / 'root-part306-main-native-exit.json')['ExitCode'] == 0
for row in freeze['Files']:
    data = (root / row['Path']).read_bytes()
    assert len(data) == row['Bytes'] and digest(data) == row['Sha256'], row['Path']
lines = (artifacts / 'root-part306-main-native.log').read_text(encoding='utf-8-sig').splitlines()
assert lines == (artifacts / 'root-part303-main-native.log').read_text(encoding='utf-8-sig').splitlines()
assert len(lines) == 4
generated = lines[2].removeprefix('Native generated source SHA256: ')
assert generated == '6E28E8B093F042C22ACAFD2C0213A55C7DCEFC2D162B486AC8443FC7C4E5F77A'
tests = read(artifacts / 'tests.json')
assert tests['Passed'] == freeze['ExpectedFullPassed'] and tests['Failed'] == 0
for name in freeze['AddedHostNames']:
    assert any(row['Name'] == name and row['Status'] == 'Passed' for row in tests['Tests']), name
tool = root / 'tools/Erlang.NifNativeBackend/bin/Debug/net10.0'
runtime_hash = digest((tool / 'Erlang.Runtime.dll').read_bytes())
assert runtime_hash == digest((root / 'src/Erlang.Runtime/bin/Debug/net10.0/Erlang.Runtime.dll').read_bytes())
fixture = root / 'artifacts/spawn-protocol-nif-290-38688662194-terminal/artifact-11951535100/nif-fixture-build/fixture.dll'
assert digest(fixture.read_bytes()) == '235D9E8997B7760E120ECD5CFACA29F01D0936A8FD89EF8B23DDF7D0DB43883F'
result = {'Status': 'PASS_ACTUAL_MAIN_NATIVE_TRUSTED_WINDOWS_AND_COMPILED_SOURCE', 'ExitCode': 0, 'SourcePaths': freeze['SourcePaths'], 'CSharpPaths': freeze['CSharpPaths'], 'SourceHashesExact': True, 'SourceFreezeSha256': digest(freeze_path.read_bytes()), 'ReviewedSourcePlanSha256': freeze['ReviewedSourcePlanSha256'], 'NativeTrustedWindowsProfilePassed': True, 'ActualGeneratedCSharpRoutePassed': True, 'ActualNativeFenceConcurrencyPassed': True, 'NonDefaultNativeProviderRefusalPassed': True, 'ManagedToolDllSha256': digest((tool / 'Erlang.NifNativeBackend.dll').read_bytes()), 'RuntimeDllSha256': runtime_hash, 'FixtureSha256': digest(fixture.read_bytes()), 'GeneratedCSharpSha256': generated, 'GeneralNifAbiComplete': False, 'PublicLoadNifActivated': False, 'NewRawOriginalReexecuted': False, 'PendingOnLoadPurgeImplemented': False}
out = artifacts / 'root-part306-main-native-guard.json'
assert not out.exists()
out.write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
print(json.dumps(result))
