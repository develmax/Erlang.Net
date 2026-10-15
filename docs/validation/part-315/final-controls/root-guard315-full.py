from pathlib import Path
import hashlib
import json
import subprocess

root = Path.cwd()
proof = root / 'artifacts/root-part315-main1'
sha = lambda raw: hashlib.sha256(raw).hexdigest().upper()
read = lambda path: json.loads(path.read_text(encoding='utf-8-sig'))
assert not (proof / 'full-guard.json').exists()
freeze = read(proof / 'source-freeze.json')
assert read(proof / 'full-exit.json')['ExitCode'] == 0
closure_path = proof / 'source-resource-closure.json'
assert sha(closure_path.read_bytes()) == freeze['FinalSourceClosureSha256']
closure = read(closure_path)
assert len(closure['Files']) == 2227
for row in closure['Files']:
    raw = (root / row['Path']).read_bytes()
    assert len(raw) == row['Bytes'] and sha(raw) == row['Sha256'], row['Path']
current = read(root / 'artifacts/tests.json')
previous = read(root / 'docs/validation/part-314/final-main/tests.json')
assert current['Passed'] == 6642 and previous['Passed'] == 6641
assert current['Failed'] == 0 and all(row['Status'] == 'Passed' for row in current['Tests'])
new_name = 'runtime/native-payload-provider-real-joined-request-extents'
assert [row['Name'] for row in current['Tests'] if row['Name'] != new_name] == [row['Name'] for row in previous['Tests']]
assert sum(row['Name'] == new_name for row in current['Tests']) == 1
integration = read(root / 'artifacts/integration-results.json')
assert integration == read(root / 'docs/validation/part-314/final-main/integration-results.json') and len(integration) == 11
assert 'Source layout: 0 files need formatting' in (proof / 'full-validation.log').read_text(encoding='utf-8-sig')
plans = []
for relative, expected_count, expected_digest in [
    ('tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json', 1065, 'F9D1CAD1C03239E49D85581B55B5C3A406FBE29B55314C6E7E423C1F97BDE278'),
    ('tools/Erlang.NifReallocationObservations/SOURCE-PLAN.json', 1090, 'D68B34BA634B104ACAC155DCE22E6DE7247CBE52017A4DAB3E8032B8DEC4C03E')
]:
    path = root / relative
    assert sha(path.read_bytes()) == expected_digest
    rows = read(path)['Files']
    assert len(rows) == len({row['Path'] for row in rows}) == expected_count
    for row in rows:
        raw = (root / row['Path']).read_bytes()
        assert row['HashMode'] in {'TextLF', 'BinaryExact'}
        if row['HashMode'] == 'TextLF':
            raw = raw.replace(b'\r\n', b'\n')
        assert len(raw) == row['Bytes'] and sha(raw) == row['Sha256'], row['Path']
    plans.append({'Path': relative, 'Inputs': expected_count, 'Sha256': expected_digest})
registry_path = root / 'docs/supported-mfas.json'
raw_registry = registry_path.read_bytes()
registry = json.loads(raw_registry)
historical = json.loads(subprocess.check_output(['git', 'show', 'HEAD:docs/supported-mfas.json']))
key = lambda row: (row['Module'], row['Function'], row['Arity'])
assert len(registry) == len(historical) == 505
assert [key(row) for row in registry] == [key(row) for row in historical]
changes = []
for row, old in zip(registry, historical):
    if key(row) in {('erlang', 'process_info', 1), ('erlang', 'load_nif', 2)}:
        assert {k: v for k, v in row.items() if k != 'Limits'} == {k: v for k, v in old.items() if k != 'Limits'}
        changes.append({'Mfa': list(key(row)), 'Field': 'Limits', 'Raw': row['Limits'], 'Derived': old['Limits']})
        row['Limits'] = old['Limits']
    else:
        assert row == old, key(row)
assert registry == historical and len(changes) == 2
assert sum(row['Module'] == 'erlang' for row in registry) == 347
(proof / 'raw-supported-mfas.json').write_bytes(raw_registry)
registry_path.write_bytes((json.dumps(registry, indent=2) + '\n').encode())
guard = {
    'Status': 'PASS_ACTUAL_MAIN315_6642_FULL_AND_ELEVEN_INTEGRATIONS',
    'Passed': 6642, 'Failed': 0, 'All6641PreviousOrderedNamesPreserved': True,
    'AddedOrderedName': new_name, 'IntegrationResults': 11,
    'SourcePaths': 23, 'CSharpPaths': 17, 'SourceResourceClosureFiles': 2227,
    'SourceFreezeSha256': sha((proof / 'source-freeze.json').read_bytes()),
    'SourceClosureSha256': sha(closure_path.read_bytes()), 'Plans': plans,
    'Registry': 505, 'ErlangMfas': 347, 'PublicMfaDelta': 0,
    'RawRegistrySha256': sha(raw_registry), 'RegistryMetadataDisclosuresRestored': changes,
    'OriginalNewCasesExecutedByFullPipeline': False,
    'NativeFixtureExecutedByFullPipeline': False,
    'GeneralNifAbiComplete': False,
    'GuardSourceSha256': sha(Path(__file__).read_bytes())
}
(proof / 'full-guard.json').write_text(json.dumps(guard, indent=2) + '\n', encoding='utf-8')
print(json.dumps({'Status': guard['Status'], 'GuardSha256': sha((proof / 'full-guard.json').read_bytes())}))
