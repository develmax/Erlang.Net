from pathlib import Path
import hashlib
import json
import re
import shutil
import subprocess
import sys

root = Path.cwd().resolve()
artifacts = root / 'artifacts'
isolated = artifacts / 'root-nif-lifetime300-isolated'
candidate = isolated / 'candidate'
read = lambda path: json.loads(path.read_bytes())
digest = lambda data: hashlib.sha256(data).hexdigest().upper()
git = lambda *args: subprocess.check_output(['git', *args], cwd=root, stderr=subprocess.DEVNULL)
parent = 'f29bbeeb42aa44b52bd919e352542edbd2e2efaa'
assert git('rev-parse', 'HEAD').decode().strip() == parent
assert not git('diff', '--name-only', 'HEAD').strip()
assert read(isolated / 'final27-focus52-exit.json')['ExitCode'] == 0
assert read(isolated / 'final27-native-windows-exit.json')['ExitCode'] == 0
assert read(isolated / 'halt-red-control-exit.json')['ExitCode'] != 0
assert b'ReentrantRetirementOperation' in (isolated / 'halt-red-control.log').read_bytes()
assert read(isolated / 'tokens27-before.json') == read(isolated / 'tokens27-after.json')
paths = read(isolated / 'paths27.json')
assert len(paths) == len(set(paths)) == 27
paths += ['tools/Erlang.NifNativeBackend/README.md']
assert len((isolated / 'final27-native-windows.log').read_text(encoding='utf-8-sig').splitlines()) == 4
for name in paths:
    assert (candidate / name).is_file()
    target = root / name
    target.parent.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(candidate / name, target)

plan = root / 'tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json'
backup = artifacts / 'root-part300-previous-reviewed-source-plan.json'
assert not backup.exists()
backup.write_bytes(plan.read_bytes())
assert digest(backup.read_bytes()) == '7D5BBD71767A5C202A87C955D558A7D2AFE45C490EE33B236D918900122ACBFA'
plan.unlink()
try:
    subprocess.run([sys.executable, str(plan.parent / 'seal-source-plan.py'), '--repository', str(root), '--output', str(plan)], check=True)
except BaseException:
    plan.write_bytes(backup.read_bytes())
    raise
plan_hash = digest(plan.read_bytes())
workflow = root / '.github/workflows/nif-pending-native.yml'
data = workflow.read_bytes()
old = b'7D5BBD71767A5C202A87C955D558A7D2AFE45C490EE33B236D918900122ACBFA'
assert data.count(old) == 1
workflow.write_bytes(data.replace(old, plan_hash.encode()))
paths += [str(plan.relative_to(root)).replace('\\', '/'), str(workflow.relative_to(root)).replace('\\', '/')]
records = []
for name in sorted(paths):
    data = (root / name).read_bytes()
    try:
        previous = digest(git('show', 'HEAD:' + name))
    except subprocess.CalledProcessError:
        previous = None
    records.append({'Path': name, 'Bytes': len(data), 'Sha256': digest(data), 'PreviousGitSha256': previous})
assert len(records) == 30
freeze = {
    'ParentHead': parent,
    'SourcePaths': len(records),
    'CSharpPaths': 27,
    'TokensExactForCSharp': True,
    'ReviewedSourceInputs': len(read(plan)['Files']),
    'ReviewedSourcePlanSha256': plan_hash,
    'PublicLoadNifRegistered': False,
    'GeneralNifAbiComplete': False,
    'IsolatedFocusGroups': 52,
    'OwnedLifetimeScenarios': 12,
    'ActualNativeFenceConcurrencyPassed': True,
    'NonDefaultNativeProviderRefusalPassed': True,
    'HaltRegressionRedThenGreenPassed': True,
    'Files': records,
}
output = artifacts / 'root-part300-final-source-freeze.json'
assert not output.exists()
output.write_text(json.dumps(freeze, indent=2) + '\n', encoding='utf-8')
print(json.dumps({key: value for key, value in freeze.items() if key != 'Files'}))
