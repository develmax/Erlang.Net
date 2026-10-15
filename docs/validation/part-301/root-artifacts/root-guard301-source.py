from pathlib import Path
import hashlib
import json
import runpy
import sys

sys.dont_write_bytecode = True
root = Path.cwd().resolve()
artifacts = root / 'artifacts'
read = lambda path: json.loads(path.read_bytes())
digest = lambda data: hashlib.sha256(data).hexdigest().upper()
freeze_path = artifacts / 'root-part301-final-source-freeze.json'
freeze = read(freeze_path)
assert freeze['SourcePaths'] == len(freeze['Files']) == 14
assert freeze['CSharpPaths'] == 12
for row in freeze['Files']:
    data = (root / row['Path']).read_bytes()
    assert len(data) == row['Bytes'] and digest(data) == row['Sha256'], row['Path']
directory = root / 'tools/Erlang.NifPendingVarargsObservations'
plan_path = directory / 'reviewed-source-plan.json'
plan = read(plan_path)
assert digest(plan_path.read_bytes()) == freeze['ReviewedSourcePlanSha256']
assert len(plan['Files']) == freeze['ReviewedSourceInputs'] == 1003
namespace = runpy.run_path(str(directory / 'run-reviewed.py'))
assert namespace['reviewed_inputs'](root, plan_path) == {row['Path'] for row in plan['Files']}
for row in plan['Files']:
    data = (root / row['Path']).read_bytes()
    if row['HashMode'] == 'TextLF':
        data = data.replace(b'\r\n', b'\n')
    else:
        assert row['HashMode'] == 'BinaryExact'
    assert len(data) == row['Bytes'] and digest(data) == row['Sha256'], row['Path']
isolated = artifacts / 'root-process-info301-isolated'
assert read(isolated / 'tokens12-before.json') == read(isolated / 'tokens12-after.json')
result = {'Status': 'PASS_EXACT_PROMOTED_SOURCE_AND_1003_REVIEWED_INPUTS', 'SourceFreezeSha256': digest(freeze_path.read_bytes()), 'SourcePaths': 14, 'CSharpPaths': 12, 'ReviewedInputs': 1003}
(artifacts / 'root-part301-source-guard.json').write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
print(json.dumps(result))

