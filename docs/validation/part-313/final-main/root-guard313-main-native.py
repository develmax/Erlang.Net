from pathlib import Path
import hashlib
import json
import runpy
import sys

sys.dont_write_bytecode = True
root = Path.cwd()
art = root / 'artifacts'
out = art / 'root-part313-main-windows-native1'
proof = art / 'root-part313-main1'
sha = lambda b: hashlib.sha256(b).hexdigest().upper()
read = lambda p: json.loads(p.read_bytes())
freeze = read(proof / 'source-freeze.json')
closure = read(proof / 'source-resource-closure.json')
for row in closure['Files']:
    raw = (root / row['Path']).read_bytes()
    assert len(raw) == row['Bytes'] and sha(raw) == row['Sha256'], row['Path']
assert sha((proof / 'source-resource-closure.json').read_bytes()) == freeze['FinalSourceClosureSha256']
assert read(proof / 'full-exit.json')['ExitCode'] == 0
assert read(art / 'tests.json')['Passed'] == 6640 and read(art / 'tests.json')['Failed'] == 0
assert sha((art / 'nif-reallocation-native309-windows-sdk-peer1/sdk-only-review.json').read_bytes()) == '94D2C3797020558D6AE28DBC4A78E2E4E313893BEAE1913969438403179D71D0'

for role in ['legacy', 'reallocation']:
    process = read(out / (role + '.process.json'))
    attempt = read(out / (role + '.attempt.json'))
    assert process['ExitCode'] == 0 and process['TimedOut'] is False
    assert process['NativeExecuted'] is True and process['MainExecution'] is True
    assert process['OriginalExecuted'] is False and not (out / (role + '.stderr')).read_bytes()
    assert attempt['SourceFreezeSha256'] == sha((proof / 'source-freeze.json').read_bytes())
    assert attempt['SourceClosureSha256'] == freeze['FinalSourceClosureSha256']
    for row in attempt['Bindings']:
        assert sha((root / row['Path']).read_bytes()) == row['Sha256'], row['Path']

old = (out / 'legacy.stdout').read_bytes()
new = (out / 'reallocation.stdout').read_bytes()
assert sha(old) == '605319B46478B00DFC8434CD384FDFA7031A19B614EFA50ADA3D55758D600F78'
assert sha(new) == '31E98784E2550952C18B66B4A04BF0B77C85D17B9FD36D770FCDEBFFB94531DB'
assert old == (art / 'root-part313-native-legacy-windows1/native.stdout').read_bytes()
assert new == (art / 'root-part313-native-windows-sdk1/sdk.stdout').read_bytes()
parsed = runpy.run_path(str(root / 'tools/Erlang.NifPendingVarargsObservations/reviewed_sdk_stdout.py'))['parse_sdk_stdout'](old, b'', require_abi=False)
assert parsed['GeneratedCSharpSha256'] == '6E28E8B093F042C22ACAFD2C0213A55C7DCEFC2D162B486AC8443FC7C4E5F77A'
rows = [json.loads(line) for line in new.splitlines()]
assert len(rows) == 4
assert [(x['Case'], x['ReturnedMask']) for x in rows[:3]] == [('null_positive', 1539), ('null_zero', 1601), ('resize_sequence', 2047)]
assert all(x['HeldAdmissionDelayedUnload'] is True for x in rows[:3])
assert rows[3]['LinuxGlobalSymbolResident'] is False and rows[3]['WindowsHeaderLayoutVerified'] is True
guard = {'Status': 'ACTUAL_MAIN_POST_FULL_WINDOWS_LEGACY_SEVEN_AND_NEW_REALLOCATION_THREE_PASS',
         'FullPassed': 6640, 'SourceFilesRehashed': len(closure['Files']),
         'SourceFreezeSha256': sha((proof / 'source-freeze.json').read_bytes()),
         'LegacyStrictParsed': parsed, 'NewCases': rows[:3], 'NewControls': rows[3],
         'NewStdoutExactIndependentlyReviewedIsolatedOutput': True,
         'IsolatedPassiveReviewSha256': '94D2C3797020558D6AE28DBC4A78E2E4E313893BEAE1913969438403179D71D0',
         'OriginalExecutedForNewCases': False, 'OriginalAndSdkCompared': False,
         'LinuxNewBridgeExecuted': False, 'ActualAllocatorOom': False,
         'DefaultNativeInstructionHandlerInstalled': False, 'GeneralNifAbiComplete': False}
target = out / 'guard.json'
assert not target.exists()
target.write_text(json.dumps(guard, indent=2) + '\n', encoding='utf-8')
print(json.dumps({'Status': guard['Status'], 'SourceFiles': guard['SourceFilesRehashed'], 'GuardSha256': sha(target.read_bytes())}))
