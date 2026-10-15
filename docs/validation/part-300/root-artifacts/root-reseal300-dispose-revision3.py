from pathlib import Path
import hashlib
import json
import subprocess
import sys

root = Path.cwd().resolve()
artifacts = root / 'artifacts'
isolated = artifacts / 'root-nif-lifetime300-isolated'
read = lambda path: json.loads(path.read_bytes())
digest = lambda data: hashlib.sha256(data).hexdigest().upper()
assert read(isolated / 'dormant-revision3-main-focus53-exit.json')['ExitCode'] == 0
assert read(isolated / 'tokens27-before.json') == read(isolated / 'tokens27-after.json')
assert read(artifacts / 'root-part300-initial-full-tests.json')['Failed'] == 1
initial = read(artifacts / 'root-part300-initial-source-freeze.json')
for row in initial['Files']:
    if row['Path'].endswith('.cs'):
        assert (root / row['Path']).read_bytes() == (isolated / 'candidate' / row['Path']).read_bytes()
plan = root / 'tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json'
old_hash = digest(plan.read_bytes())
assert old_hash == initial['ReviewedSourcePlanSha256'] == '303578B18C2DC46482FC70C0F0B965828FFE528B3575462DE9B45840694B3CA4'
backup = artifacts / 'root-part300-pre-dispose-reviewed-source-plan.json'
assert not backup.exists()
backup.write_bytes(plan.read_bytes())
plan.unlink()
try:
    subprocess.run([sys.executable, str(plan.parent / 'seal-source-plan.py'), '--repository', str(root), '--output', str(plan)], check=True)
except BaseException:
    plan.write_bytes(backup.read_bytes())
    raise
new_hash = digest(plan.read_bytes())
assert len(read(plan)['Files']) == 1001
workflow = root / '.github/workflows/nif-pending-native.yml'
data = workflow.read_bytes()
assert data.count(old_hash.encode()) == 1
workflow.write_bytes(data.replace(old_hash.encode(), new_hash.encode()))
for row in initial['Files']:
    data = (root / row['Path']).read_bytes()
    row['Bytes'] = len(data)
    row['Sha256'] = digest(data)
initial.update({
    'ReviewedSourcePlanSha256': new_hash,
    'IsolatedFocusGroups': 53,
    'OwnedLifetimeScenarios': 13,
    'NonblockingDisposeRegressionPassed': True,
    'DormantReentrantDisposeRegressionPassed': True,
    'RegistryHeldNifDisposeRefusedBeforeMutation': True,
    'InitialFullFailurePreserved': True,
})
output = artifacts / 'root-part300-final-source-freeze.json'
output.write_text(json.dumps(initial, indent=2) + '\n', encoding='utf-8')
print(json.dumps({key: value for key, value in initial.items() if key != 'Files'}))
