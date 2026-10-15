import hashlib
import json
from pathlib import Path
import runpy
import sys

sys.dont_write_bytecode = True
root = Path.cwd()
path = root / 'tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json'
workflow = root / '.github/workflows/nif-pending-native.yml'
old = path.read_bytes()
digest = lambda data: hashlib.sha256(data).hexdigest().upper()
assert digest(old) == 'C8577299D35B72112F8FFAE3BEBD689A8BC9D7D8318F1601EBD75178D6B937E0'
original = json.loads(old)
helper = runpy.run_path(str(path.parent / 'run-reviewed.py'))
inputs = helper['reviewed_inputs'](root, path)
before = {row['Path']: row for row in original['Files']}
assert len(before) == 1007 and len(inputs) == 1009
assert inputs - before.keys() == {'src/Erlang.Runtime/ProcessRuntime.CodeChecks.cs', 'src/Erlang.Runtime/ProcessRuntime.CodePurge.cs'}
assert not before.keys() - inputs
rows = []
for name in sorted(inputs):
    mode = before[name]['HashMode'] if name in before else 'TextLF'
    row = helper['record'](root / name, root, mode)
    row['Path'] = name
    rows.append(row)
assert all(row['HashMode'] in ('TextLF', 'BinaryExact') for row in rows)
original['Files'] = rows
data = (json.dumps(original, indent=2) + '\n').encode()
workflow_old = workflow.read_bytes()
needle = digest(old).encode()
assert workflow_old.count(needle) == 1
workflow_new = workflow_old.replace(needle, digest(data).encode())
changed = [row['Path'] for row in rows if row['Path'] not in before or row != before[row['Path']]]
receipt = {'Status': 'SOURCE_ONLY_PLAN_1009_RESEALED_NATIVE_NOT_EXECUTED', 'PreviousSha256': digest(old), 'Sha256': digest(data), 'Inputs': len(rows), 'ChangedInputs': changed, 'WorkflowPreviousSha256': digest(workflow_old), 'WorkflowSha256': digest(workflow_new), 'NativeExecuted': False, 'OriginalExecuted': False}
out = root / 'artifacts/root-part305-native-plan-reseal.json'
assert not out.exists()
path.write_bytes(data)
workflow.write_bytes(workflow_new)
out.write_text(json.dumps(receipt, indent=2) + '\n', encoding='utf-8')
print(json.dumps(receipt))
