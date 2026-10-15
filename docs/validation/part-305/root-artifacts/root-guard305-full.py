import hashlib
import json
from pathlib import Path

root = Path.cwd()
artifacts = root / 'artifacts'
read = lambda path: json.loads(path.read_bytes())
sha = lambda data: hashlib.sha256(data).hexdigest().upper()
freeze_path = artifacts / 'root-part305-final-source-freeze.json'
assert sha(freeze_path.read_bytes()) == '5D712DACFAA2298F608472CC1D75AF83C25162AFD4FBD7474871A60A8CA4D255'
freeze = read(freeze_path)
for row in freeze['Files']:
    data = (root / row['Path']).read_bytes()
    assert len(data) == row['Bytes'] and sha(data) == row['Sha256'], row['Path']
exit_result = read(artifacts / 'root-part305-full-exit.json')
assert exit_result['ExitCode'] == 0
previous = read(root / 'docs/validation/part-303/tests.json')
current = read(artifacts / 'tests.json')
assert previous['Passed'] == 6622 and current['Passed'] == 6624 and current['Failed'] == 0
assert len(current['Tests']) == 6624 and all(row['Status'] == 'Passed' for row in current['Tests'])
added = freeze['AddedHostNames']
assert len(added) == 2 and all(sum(row['Name'] == name for row in current['Tests']) == 1 for name in added)
assert [row['Name'] for row in current['Tests'] if row['Name'] not in added] == [row['Name'] for row in previous['Tests']]
integration = read(artifacts / 'integration-results.json')
assert integration == read(root / 'docs/validation/part-303/integration-results.json')
assert len(integration) == 11
log = (artifacts / 'root-part305-full-validation.log').read_text(encoding='utf-8-sig')
for case in ('fresh', 'current', 'deleted_old'):
    for finish in ('direct_true', 'false_false_true'):
        assert 'PASS raw on_load captured final ' + case + '_' + finish in log
assert 'Source layout: 0 files need formatting' in log
result = {'Status': 'ACTUAL_MAIN_6624_FULL_ALL6622_ORDERED_NAMES_30_SOURCE_24_CSHARP_AND_11_INTEGRATION_PASS', 'Passed': 6624, 'Failed': 0, 'HistoricalOrderedNamesPreserved': 6622, 'AddedHostNames': added, 'ActualSixOriginalBeamFixtureCasesPassed': True, 'SourcePaths': 30, 'CSharpPaths': 24, 'SourceHashesExact': True, 'SourceFreezeSha256': sha(freeze_path.read_bytes()), 'ReviewedSourceInputs': 1009, 'IntegrationResults': integration, 'NativeExecutedByThisGuard': False, 'NewOriginalExecuted': False, 'PublicLoadNifActivated': False}
out = artifacts / 'root-part305-full-guard.json'
assert not out.exists()
out.write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
print(json.dumps(result))
