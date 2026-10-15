from pathlib import Path
import hashlib
import json
import runpy
import sys

sys.dont_write_bytecode = True
root = Path.cwd()
artifacts = root / 'artifacts'
read = lambda path: json.loads(path.read_bytes())
sha = lambda data: hashlib.sha256(data).hexdigest().upper()
freeze_path = artifacts / 'root-part312-revision2/source-freeze.json'
freeze = read(freeze_path)
assert freeze['SourcePaths'] == 38 and freeze['CSharpPaths'] == 35
assert freeze['ExpectedFullPassed'] == 6640 and freeze['SourceClosurePaths'] == 2171
assert freeze['ReviewedSourceInputs'] == 1038
assert freeze['PublicLoadNifRegistered'] and not freeze['DefaultNativeLoadInstructionHandlerInstalled']
assert not freeze['GeneralNifAbiComplete']
for row in freeze['Files']:
    payload = (root / row['Path']).read_bytes()
    assert len(payload) == row['Bytes'] and sha(payload) == row['Sha256'], row['Path']
assert read(artifacts / 'root-part312-revision2/full-exit.json')['ExitCode'] == 0
closure_path = artifacts / 'root-part312-revision2/source-resource-closure.json'
assert sha(closure_path.read_bytes()) == freeze['FinalSourceClosureSha256']
closure = read(closure_path)
assert len(closure['Files']) == 2171
for row in closure['Files']:
    payload = (root / row['Path']).read_bytes()
    assert len(payload) == row['Bytes'] and sha(payload) == row['Sha256'], row['Path']
previous = read(root / 'docs/validation/part-311/tests.json')
current = read(artifacts / 'tests.json')
assert previous['Passed'] == 6639 and current['Passed'] == 6640 and current['Failed'] == 0
names = [row['Name'] for row in current['Tests']]
historical = [row['Name'] for row in previous['Tests']]
assert len(names) == len(set(names)) == 6640
assert [name for name in names if name in set(historical)] == historical
added = [name for name in names if name not in set(historical)]
assert added == freeze['AddedHostNames'] == ['runtime/source-stage-transaction-ownership-and-purge-generation']
assert all(row['Status'] == 'Passed' for row in current['Tests'])
integration = read(artifacts / 'integration-results.json')
assert len(integration) == 11 and integration == read(root / 'docs/validation/part-311/integration-results.json')
assert 'Source layout: 0 files need formatting' in (artifacts / 'root-part312-revision2/full-validation.log').read_text(encoding='utf-8-sig')
plan_path = root / 'tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json'
plan = read(plan_path)
helper = runpy.run_path(str(plan_path.parent / 'run-reviewed.py'))
assert len(plan['Files']) == 1038
assert helper['reviewed_inputs'](root, plan_path) == {row['Path'] for row in plan['Files']}
for row in plan['Files']:
    actual = helper['record'](root / row['Path'], root, row['HashMode'])
    actual['Path'] = row['Path']
    assert actual == row, row['Path']
optional = read(artifacts / 'root-part311-optional-io-guard.json')
assert any(row['Name'] == optional['PassedHost'] and row['Status'] == 'Passed' for row in current['Tests'])
for row in optional['Sources']:
    assert sha((root / row['Path']).read_bytes()) == row['Sha256'], row['Path']
optional['FullTestsSha256'] = sha((artifacts / 'tests.json').read_bytes())
optional_path = artifacts / 'root-part312-optional-io-guard.json'
assert not optional_path.exists()
optional_path.write_text(json.dumps(optional, indent=2) + '\n', encoding='utf-8')
result = {
    'Status': 'ACTUAL_MAIN_6640_FULL_ALL6639_HISTORICAL_ORDERED_NAMES_38_SOURCE_35_CSHARP_AND_11_INTEGRATION_PASS',
    'Passed': 6640, 'Failed': 0, 'AddedNames': added,
    'HistoricalOrderedNamesPreserved': 6639, 'SourceHashesExact': True,
    'SourceFreezeSha256': sha(freeze_path.read_bytes()),
    'SourcePaths': 38, 'CSharpPaths': 35, 'ReviewedSourceInputs': 1038,
    'IntegrationResults': integration, 'NewPublicMfa': False,
    'PublicLoadNifRegistered': True, 'DefaultNativeLoadInstructionHandlerInstalled': False,
    'GeneralNifAbiComplete': False, 'NewStageControlsAreHostOnly': True,
    'NewOriginalStageInterleavingExecution': False,
}
target = artifacts / 'root-part312-full-guard.json'
assert not target.exists()
target.write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
print(json.dumps(result))
