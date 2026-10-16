"""Prepare four reviewed native plan/pin overlays; never mutate repository inputs."""
import argparse
import copy
import hashlib
import json
import pathlib
import re
import subprocess

parser = argparse.ArgumentParser()
parser.add_argument('--repository', required=True, type=pathlib.Path)
parser.add_argument('--expected-head', required=True)
parser.add_argument('--tested-freeze', required=True, type=pathlib.Path)
parser.add_argument('--tested-freeze-sha256', required=True)
parser.add_argument('--output', required=True, type=pathlib.Path)
parser.add_argument('--include-attributes', action='store_true')
args = parser.parse_args()
root = args.repository.resolve()
out = args.output.resolve()
assert out.is_relative_to(root / 'artifacts') and not out.exists()
sha = lambda data: hashlib.sha256(data).hexdigest().upper()
load = lambda path: json.loads(path.read_bytes())
assert subprocess.check_output(['git', '--no-optional-locks', 'rev-parse', 'HEAD'], cwd=root, text=True).strip() == args.expected_head
assert sha(args.tested_freeze.read_bytes()) == args.tested_freeze_sha256.upper()
freeze = load(args.tested_freeze)
assert len(freeze['Files']) == 15
for row in freeze['Files']:
    data = (root / row['Path']).read_bytes()
    assert sha(data) == row['Sha256'] and len(data) == row['Bytes'], row['Path']
new_paths = {'src/Erlang.Otp.Behaviours/ProcLib.cs', 'src/Erlang.Otp.Behaviours/ProcLibAtoms.cs', 'src/Erlang.Otp.Behaviours/ProcLibNames.cs'}
modified = {'src/Erlang.Runtime/FriendAssemblies.cs', 'src/Erlang.Runtime/RuntimeAssemblyNames.cs', 'src/Erlang.Otp.Behaviours/OtpModules.cs'}
legacy = 'tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json'
legacy_workflow = '.github/workflows/nif-pending-native.yml'
own = 'tools/Erlang.NifReallocationObservations/SOURCE-PLAN.json'
own_workflow = '.github/workflows/nif-reallocation-native.yml'
specs = [
    (legacy, legacy_workflow, 'REVIEWED_SOURCE_PLAN_SHA256', 1120, '3187D751083A34B7434E8B6C229C6D631277CBC7F98612D1888FB27ECA56D342', modified),
    (own, own_workflow, 'REVIEWED_REALLOCATION_PLAN_SHA256', 1145, '0A1FAE308FBFC653924ED02A99705DE8C4622D293A15197784C205FE664D778D', modified | {legacy, legacy_workflow} | ({'.gitattributes'} if args.include_attributes else set()))
]
overlay = {}
plans = []
parents = {}

def data_at(name):
    assert not pathlib.PurePosixPath(name).is_absolute() and '..' not in pathlib.PurePosixPath(name).parts
    return overlay[name] if name in overlay else (root / name).read_bytes()

for plan_name, workflow, pin, previous_count, parent_hash, allowed in specs:
    original = data_at(plan_name)
    assert sha(original) == parent_hash
    previous = json.loads(original)
    rows = previous['Files']
    assert len(rows) == previous_count and len({row['Path'] for row in rows}) == previous_count
    assert not new_paths.intersection(row['Path'] for row in rows)
    assert workflow not in {row['Path'] for row in rows}
    updated_rows = []
    changed = set()
    for row in rows:
        assert row['HashMode'] in ('TextLF', 'BinaryExact')
        data = data_at(row['Path'])
        if row['HashMode'] == 'TextLF':
            data = data.replace(b'\r\n', b'\n')
        updated = dict(row, Bytes=len(data), Sha256=sha(data))
        updated_rows.append(updated)
        if updated != row:
            changed.add(row['Path'])
    assert changed == allowed, (plan_name, sorted(changed), sorted(allowed))
    for name in new_paths:
        data = data_at(name).replace(b'\r\n', b'\n')
        updated_rows.append({'Path': name, 'Bytes': len(data), 'Sha256': sha(data), 'HashMode': 'TextLF'})
    updated_rows.sort(key=lambda row: row['Path'])
    value = copy.deepcopy(previous)
    value['Files'] = updated_rows
    assert {key: item for key, item in value.items() if key != 'Files'} == {key: item for key, item in previous.items() if key != 'Files'}
    assert len(updated_rows) == previous_count + 3
    overlay[plan_name] = (json.dumps(value, indent=2) + '\n').encode()
    assert not overlay[plan_name].startswith(b'\xef\xbb\xbf') and b'\r' not in overlay[plan_name]
    workflow_before = data_at(workflow)
    pattern = rb'(?m)^([ \t]*' + pin.encode() + rb': )[A-F0-9]{64}([ \t]*\r?)$'
    workflow_after, count = re.subn(pattern, lambda match: match[1] + sha(overlay[plan_name]).encode() + match[2], workflow_before)
    assert count == 1
    overlay[workflow] = workflow_after
    parents[plan_name] = sha(original)
    parents[workflow] = sha(workflow_before)
    plans.append({'Path': plan_name, 'Inputs': len(updated_rows), 'Sha256': sha(overlay[plan_name]), 'Workflow': workflow, 'Pin': pin, 'AddedInputs': sorted(new_paths), 'ChangedExistingInputs': sorted(changed), 'AllNonFilesFieldsUnchanged': True})

for plan in plans:
    for row in json.loads(overlay[plan['Path']])['Files']:
        data = data_at(row['Path'])
        if row['HashMode'] == 'TextLF':
            data = data.replace(b'\r\n', b'\n')
        assert len(data) == row['Bytes'] and sha(data) == row['Sha256'], row['Path']
out.mkdir(parents=True)
files = []
for name, data in overlay.items():
    target = out / 'overlay' / name
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_bytes(data)
    files.append({'Path': name, 'Bytes': len(data), 'Sha256': sha(data), 'ParentSha256': parents[name]})
report = {'Status': 'SOURCE_ONLY_NATIVE_PLAN331_COMPOSITION_OVERLAY', 'ParentHead': args.expected_head, 'TestedSourceFreezeSha256': args.tested_freeze_sha256.upper(), 'MainWritten': False, 'SdkOrNativeExecuted': False, 'Plans': plans, 'Files': files}
receipt = out / 'source-freeze.json'
receipt.write_bytes((json.dumps(report, indent=2) + '\n').encode())
print(json.dumps({'Freeze': str(receipt), 'Sha256': sha(receipt.read_bytes()), 'Plans': plans}))
