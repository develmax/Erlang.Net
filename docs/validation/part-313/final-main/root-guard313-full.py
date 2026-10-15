from pathlib import Path
import hashlib
import json
import subprocess

root = Path.cwd()
art = root / 'artifacts'
proof = art / 'root-part313-main1'
sha = lambda b: hashlib.sha256(b).hexdigest().upper()
read = lambda p: json.loads(p.read_bytes())
freeze = read(proof / 'source-freeze.json')
assert read(proof / 'full-exit.json')['ExitCode'] == 0
closure_path = proof / 'source-resource-closure.json'
assert sha(closure_path.read_bytes()) == freeze['FinalSourceClosureSha256']
closure = read(closure_path)
assert len(closure['Files']) == 2205
for row in closure['Files']:
    raw = (root / row['Path']).read_bytes()
    assert len(raw) == row['Bytes'] and sha(raw) == row['Sha256'], row['Path']
current = read(art / 'tests.json')
previous = read(root / 'docs/validation/part-312/tests.json')
assert current['Passed'] == previous['Passed'] == 6640 and current['Failed'] == 0
assert [x['Name'] for x in current['Tests']] == [x['Name'] for x in previous['Tests']]
assert all(x['Status'] == 'Passed' for x in current['Tests'])
assert read(art / 'integration-results.json') == read(root / 'docs/validation/part-312/integration-results.json')
assert len(read(art / 'integration-results.json')) == 11
assert 'Source layout: 0 files need formatting' in (proof / 'full-validation.log').read_text(encoding='utf-8-sig')
plans = []
for name, digest, count in [
    ('tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json',
     '229DB0B265679904A15473349AD6B00E206C017958B30F8414C7552C3F313CE6', 1049),
    ('tools/Erlang.NifReallocationObservations/SOURCE-PLAN.json',
     'EE514F42467BF1A3FD4764D07F696CA4F2BFDA2126157F3A0C06E07DD065209A', 1074)]:
    path = root / name
    assert sha(path.read_bytes()) == digest
    rows = read(path)['Files']
    assert len(rows) == len({x['Path'] for x in rows}) == count
    for row in rows:
        raw = (root / row['Path']).read_bytes()
        assert row['HashMode'] in {'TextLF', 'BinaryExact'}
        if row['HashMode'] == 'TextLF':
            raw = raw.replace(b'\r\n', b'\n')
        assert len(raw) == row['Bytes'] and sha(raw) == row['Sha256'], row['Path']
    plans.append({'Path': name, 'Inputs': count, 'Sha256': digest})

# Preserve the raw exporter output, then restore only the two already approved
# compatibility disclosures. Every registry row must then equal published312.
registry_path = root / 'docs/supported-mfas.json'
raw_registry = registry_path.read_bytes()
registry = json.loads(raw_registry)
historical = json.loads(subprocess.check_output(['git', 'show', 'HEAD:docs/supported-mfas.json']))
assert len(registry) == len(historical) == 505
key = lambda x: (x['Module'], x['Function'], x['Arity'])
assert [key(x) for x in registry] == [key(x) for x in historical]
changes = []
for current_row, old_row in zip(registry, historical):
    if key(current_row) in {('erlang', 'process_info', 1), ('erlang', 'load_nif', 2)}:
        assert {k: v for k, v in current_row.items() if k != 'Limits'} == {k: v for k, v in old_row.items() if k != 'Limits'}
        changes.append({'Mfa': list(key(current_row)), 'Field': 'Limits',
                        'Raw': current_row['Limits'], 'Derived': old_row['Limits']})
        current_row['Limits'] = old_row['Limits']
    else:
        assert current_row == old_row, key(current_row)
assert registry == historical and len(changes) == 2
assert sum(x['Module'] == 'erlang' for x in registry) == 347
assert not (proof / 'raw-supported-mfas.json').exists()
(proof / 'raw-supported-mfas.json').write_bytes(raw_registry)
registry_path.write_bytes((json.dumps(registry, indent=2) + '\n').encode())
guard = {'Status': 'ACTUAL_MAIN313_6640_FULL_AND_ELEVEN_INTEGRATIONS_PASS',
         'Passed': 6640, 'Failed': 0, 'All6640PreviousOrderedNamesExact': True,
         'IntegrationResults': 11, 'SourcePaths': 45, 'CSharpPaths': 19,
         'SourceResourceClosureFiles': 2205, 'SourceHashesExact': True,
         'SourceFreezeSha256': sha((proof / 'source-freeze.json').read_bytes()),
         'SourceClosureSha256': sha(closure_path.read_bytes()), 'Plans': plans,
         'Registry': 505, 'ErlangMfas': 347, 'PublicMfaDelta': 0,
         'RawRegistrySha256': sha(raw_registry), 'RestoredRegistrySha256': sha(registry_path.read_bytes()),
         'RegistryMetadataDisclosuresRestored': changes,
         'OriginalNewCasesExecutedByFullPipeline': False,
         'NewNativeFixtureExecutedByFullPipeline': False,
         'DefaultNativeLoadInstructionHandlerInstalled': False, 'GeneralNifAbiComplete': False}
target = proof / 'full-guard.json'
assert not target.exists()
target.write_text(json.dumps(guard, indent=2) + '\n', encoding='utf-8')
print(json.dumps({'Status': guard['Status'], 'GuardSha256': sha(target.read_bytes())}))
