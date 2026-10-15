import hashlib
import json
from pathlib import Path
import runpy
import subprocess
import sys

sys.dont_write_bytecode = True
root = Path.cwd()
artifacts = root / 'artifacts'
read = lambda path: json.loads(path.read_bytes())
sha = lambda data: hashlib.sha256(data).hexdigest().upper()
parent = '51994bed995f9dbf31b8b88b3381cad2642012e0'
assert subprocess.check_output(['git', 'rev-parse', 'HEAD']).decode().strip() == parent
assert not subprocess.check_output(['git', 'diff', '--cached', '--name-only', '-z'])
image = read(artifacts / 'root-part305-image-promotion.json')
raw = read(artifacts / 'root-part305-raw-promotion.json')
assert image['Parent'] == raw['Parent'] == parent
program = 'tests/Erlang.Tests/Program.cs'
for row in image['Paths']:
    if row['Path'] != program:
        assert sha((root / row['Path']).read_bytes()) == row['ResultSha256'], row['Path']
for row in raw['Files']:
    data = (root / row['Path']).read_bytes()
    assert len(data) == row['Bytes'] and sha(data) == row['Sha256'], row['Path']
for package, count in [('raw-onload-exact-image305-candidate1', 8), ('raw-onload-deferred-exports306-candidate1', 17)]:
    folder = artifacts / package
    assert read(folder / f'format{count}-folder2-guard.json')['TokensExact'] is True
    assert read(folder / f'tokens{count}-folder2-before.json') == read(folder / f'tokens{count}-folder2-after.json')
plan_path = root / 'tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json'
plan = read(plan_path)
assert len(plan['Files']) == 1009
helper = runpy.run_path(str(plan_path.parent / 'run-reviewed.py'))
assert helper['reviewed_inputs'](root, plan_path) == {row['Path'] for row in plan['Files']}
for row in plan['Files']:
    data = (root / row['Path']).read_bytes()
    if row['HashMode'] == 'TextLF':
        data = data.replace(b'\r\n', b'\n')
    assert len(data) == row['Bytes'] and sha(data) == row['Sha256'], row['Path']
names = {row['Path'] for row in image['Paths']} | {row['Path'] for row in raw['Files']}
names |= {plan_path.relative_to(root).as_posix(), '.github/workflows/nif-pending-native.yml'}
assert len(names) == 30 and sum(name.endswith('.cs') for name in names) == 24
changed = set(subprocess.check_output(['git', 'diff', '--name-only', '-z']).decode().split('\0')) - {''}
assert changed <= names, sorted(changed - names)
rows = []
for name in sorted(names):
    data = (root / name).read_bytes()
    old = subprocess.run(['git', 'show', 'HEAD:' + name], capture_output=True)
    rows.append({'Path': name, 'Bytes': len(data), 'Sha256': sha(data), 'ParentGitSha256': sha(old.stdout) if old.returncode == 0 else None})
result = {'Status': 'COMBINED_EXACT_PARENT_SOURCE_FROZEN_FULL_PENDING', 'ParentHead': parent, 'SourcePaths': len(rows), 'CSharpPaths': 24, 'TokensExactForCSharp': True, 'ReviewedSourceInputs': 1009, 'ReviewedSourcePlanSha256': sha(plan_path.read_bytes()), 'ExpectedFullPassed': 6624, 'ExpectedRegistry': 503, 'ExpectedErlangPresence': 345, 'AddedHostNames': ['HOST exact-image process-code requests and purge drain', 'HOST raw on_load deferred exports against six captured OTP finals'], 'PublicMfaAdded': False, 'PublicLoadNifActivated': False, 'NewOriginalExecuted': False, 'OriginalSixCapturedFixturesUsed': True, 'PendingOnLoadPurgeImplemented': False, 'ZeroExportNifOwnerAssociationComplete': False, 'Files': rows}
out = artifacts / 'root-part305-final-source-freeze.json'
assert not out.exists()
out.write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
print(sha(out.read_bytes()))
