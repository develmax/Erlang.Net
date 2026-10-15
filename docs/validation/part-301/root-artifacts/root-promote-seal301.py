from pathlib import Path
import hashlib
import json
import shutil
import subprocess
import sys

root = Path.cwd().resolve()
artifacts = root / 'artifacts'
isolated = artifacts / 'root-process-info301-isolated'
candidate = isolated / 'candidate'
read = lambda path: json.loads(path.read_bytes())
digest = lambda data: hashlib.sha256(data).hexdigest().upper()
git = lambda *args: subprocess.check_output(['git', *args], cwd=root, stderr=subprocess.DEVNULL)
parent = 'c2d2cc304d03201dd52592c77d0389062aea0268'
assert git('rev-parse', 'HEAD').decode().strip() == parent
assert not git('diff', '--name-only', 'HEAD').strip()
assert read(isolated / 'focus3-exit.json')['ExitCode'] == 0
assert read(isolated / 'cases8-exit.json')['ExitCode'] == 0
assert len((isolated / 'cases8.log').read_text(encoding='utf-8-sig').splitlines()) == 8
assert read(isolated / 'tokens8-before.json') == read(isolated / 'tokens8-after.json')
paths = read(isolated / 'paths8.json')
assert len(paths) == len(set(paths)) == 8
for name in paths:
    target = root / name
    target.parent.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(candidate / name, target)
plan = root / 'tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json'
backup = artifacts / 'root-part301-previous-reviewed-source-plan.json'
assert not backup.exists()
old_hash = digest(plan.read_bytes())
assert old_hash == '588DC646C2E9593FF89F5F496EC6D6BD308DE1F73AD9CC4329E89759C69ED97E'
backup.write_bytes(plan.read_bytes())
plan.unlink()
try:
    subprocess.run([sys.executable, str(plan.parent / 'seal-source-plan.py'), '--repository', str(root), '--output', str(plan)], check=True)
except BaseException:
    plan.write_bytes(backup.read_bytes())
    raise
new_hash = digest(plan.read_bytes())
assert len(read(plan)['Files']) == 1003
workflow = root / '.github/workflows/nif-pending-native.yml'
data = workflow.read_bytes()
assert data.count(old_hash.encode()) == 1
workflow.write_bytes(data.replace(old_hash.encode(), new_hash.encode()))
paths += [plan.relative_to(root).as_posix(), workflow.relative_to(root).as_posix()]
records = []
for name in sorted(paths):
    data = (root / name).read_bytes()
    try:
        previous = digest(git('show', 'HEAD:' + name))
    except subprocess.CalledProcessError:
        previous = None
    records.append({'Path': name, 'Bytes': len(data), 'Sha256': digest(data), 'PreviousGitSha256': previous})
freeze = {'ParentHead': parent, 'SourcePaths': 10, 'CSharpPaths': 8, 'TokensExactForCSharp': True, 'ReviewedSourceInputs': 1003, 'ReviewedSourcePlanSha256': new_hash, 'NewPublicMfas': ['erlang:process_info/1'], 'ExpectedRegistry': 503, 'ExpectedErlangPresence': 345, 'ExpectedFullPassed': 6621, 'IsolatedHostGroups': 3, 'GeneratedCasesPassed': 8, 'OriginalNewCasesExecuted': False, 'FullPinnedDefaultPhysicalParity': False, 'Files': records}
output = artifacts / 'root-part301-final-source-freeze.json'
assert not output.exists()
output.write_text(json.dumps(freeze, indent=2) + '\n', encoding='utf-8')
print(json.dumps({key: value for key, value in freeze.items() if key != 'Files'}))
