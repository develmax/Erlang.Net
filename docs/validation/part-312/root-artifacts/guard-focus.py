from pathlib import Path
import hashlib
import json

r = Path.cwd()
d = r / 'artifacts/root-part312-revision2'
read = lambda path: json.loads(path.read_bytes())
sha = lambda data: hashlib.sha256(data).hexdigest().upper()
first = read(d / 'focus-results.json')
final = read(d / 'focus-results2.json')
assert first['Status'] == 'FAIL' and [row['Status'] for row in first['Cases']] == ['PASS', 'PASS', 'PASS', 'FAIL']
assert first['Cases'][-1]['Name'] == 'NifInitializationLifecycleHostChecks' and 'System.TypeLoadException' in first['Cases'][-1]['Exception']
assert final['Status'] == 'PASS'
assert [row['Name'] for row in final['Cases']] == ['BeamLoaderMfaChecks:Races', 'SourcePurgeStageHostChecks', 'BeamLoaderMfaChecks:Admission', 'InitializationLifecycleChecks']
assert all(row['Status'] == 'PASS' for row in final['Cases'])
assert first['Identities'] == final['Identities']
for row in final['Identities']:
    assert sha(Path(row['Path']).read_bytes()) == row['Sha256']
freeze = d / 'source-freeze.json'
for row in read(freeze)['Files']:
    raw = (r / row['Path']).read_bytes()
    assert len(raw) == row['Bytes'] and sha(raw) == row['Sha256']
raw = (r / 'tests/Erlang.Tests/BeamLoaderAdmissionRaceChecks.cs').read_bytes()
body = raw[raw.index(b'    private static async Task ClosedBeforeAdmission'):raw.index(b'    private static object Permission')]
assert sha(body) == '10D4EBBEB551A1EEC2B4DBC11BA9B2C98E0F6309959282FAE887788F1FEC5950'
out = d / 'focus-correction-guard.json'
assert not out.exists()
out.write_text(json.dumps({'Status': 'ACTUAL_MAIN_FOUR_FOCUSED_GROUPS_PASS_SAME_COMPILED_IDENTITIES', 'SourceFreezeSha256': sha(freeze.read_bytes()), 'Passed': 4, 'ClosedRaceBodyByteExactAfterFormatting': True, 'FirstAttempt': 'Three actual passes followed by Root-authored nonexistent type name; fourth method not executed.', 'CorrectedAttempt': 'Actual InitializationLifecycleChecks included; four passes, no source mutation or rebuild between focus attempts.', 'BuildWarnings': 0, 'BuildErrors': 0, 'OriginalExecuted': False, 'NativeExecuted': False, 'Files': [{'Path': str(path.relative_to(r)).replace('\\', '/'), 'Sha256': sha(path.read_bytes())} for path in [d / 'target-build.log', d / 'focus-results.json', d / 'focus-results2.json']]}, indent=2) + '\n', encoding='utf-8')
print(sha(out.read_bytes()))
