from pathlib import Path
import hashlib
import json
import subprocess

root = Path.cwd()
proof = root / 'artifacts/root-part314-main1'
sha = lambda raw: hashlib.sha256(raw).hexdigest().upper()
read = lambda path: json.loads(path.read_text(encoding='utf-8-sig'))
assert not (proof / 'full-guard.json').exists()
freeze = read(proof / 'source-freeze.json')
assert read(proof / 'full-exit.json')['ExitCode'] == 0
closure_path = proof / 'source-resource-closure.json'
assert sha(closure_path.read_bytes()) == freeze['FinalSourceClosureSha256']
closure = read(closure_path)
assert len(closure['Files']) == 2215
for row in closure['Files']:
    raw = (root / row['Path']).read_bytes()
    assert len(raw) == row['Bytes'] and sha(raw) == row['Sha256'], row['Path']
current = read(root / 'artifacts/tests.json')
previous = read(root / 'docs/validation/part-313/final-main/tests.json')
assert current['Passed'] == 6641 and previous['Passed'] == 6640
assert current['Failed'] == 0 and all(row['Status'] == 'Passed' for row in current['Tests'])
new_name = 'runtime/allocator-query-owned-mailbox-transport'
assert [row['Name'] for row in current['Tests'] if row['Name'] != new_name] == [row['Name'] for row in previous['Tests']]
assert sum(row['Name'] == new_name for row in current['Tests']) == 1
integration = read(root / 'artifacts/integration-results.json')
assert integration == read(root / 'docs/validation/part-313/final-main/integration-results.json') and len(integration) == 11
assert 'Source layout: 0 files need formatting' in (proof / 'full-validation.log').read_text(encoding='utf-8-sig')
plans = []
for relative, expected_count, expected_digest in [
    ('tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json', 1057, 'A55D9AB23ED5915BF13D67B7F8D15845D59E5B60360D10DA4EC254883F42AE6A'),
    ('tools/Erlang.NifReallocationObservations/SOURCE-PLAN.json', 1082, '9D10C312F86E12AE4829399A0D5245F25A6BC3EF045BCF17CE83521199ACC0CB')
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
    'Status': 'PASS_ACTUAL_MAIN314_6641_FULL_AND_ELEVEN_INTEGRATIONS',
    'Passed': 6641, 'Failed': 0, 'All6640PreviousOrderedNamesPreserved': True,
    'AddedOrderedName': new_name, 'IntegrationResults': 11,
    'SourcePaths': 17, 'CSharpPaths': 12, 'SourceResourceClosureFiles': 2215,
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
