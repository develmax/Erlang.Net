from pathlib import Path, PurePosixPath
import ast
import hashlib
import importlib.util
import json
import re
import subprocess
import sys
import unittest

A = Path(__file__).resolve().parent
G = r'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/.git'
HEAD = '0acbc26ad14315321826636b901d83856758ec2a'
BASE = subprocess.check_output(['git', '--git-dir=' + G, 'rev-parse', '7cd478612']).decode().strip()
R = A / 'exact-payload'
R.mkdir(exist_ok=False)
rows = {}


def get(name):
    assert not PurePosixPath(name).is_absolute() and '..' not in PurePosixPath(name).parts
    if name not in rows:
        raw = subprocess.check_output(['git', '--git-dir=' + G, 'show', HEAD + ':' + name])
        path = R / name
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(raw)
        rows[name] = {'Path': name, 'Bytes': len(raw), 'Sha256': hashlib.sha256(raw).hexdigest().upper()}
    return (R / name).read_bytes()


prefix = 'tools/Validation/Erlang.CiReuse/'
for name in ('source_bindings.py', 'test_source_bindings.py'):
    get(prefix + name)
get('tools/validate-async-microstates.ps1')
async_raw = get('tools/Validation/Erlang.AsyncMicrostateValidation/source-plan.json')
async_sha = hashlib.sha256(async_raw).hexdigest().upper()
caller = (R / 'tools/validate-async-microstates.ps1').read_text()
default = re.findall(r"\[string\]\$PlanSha256\s*=\s*['\"]([0-9A-Fa-f]{64})['\"]", caller)
assert default == [async_sha] and async_sha.startswith('711A')
plan = json.loads(get('tools/Native/Erlang.NifIntegratedObservations/inputs.json'))
get('.github/workflows/nif-upgrade-integrated.yml')
get('tools/Native/Erlang.NifVersion651Observations/build-inputs.json')
for row in plan['PlanPins'].values():
    dependency = json.loads(get(row['Path']))
    owner = PurePosixPath(row['Path']).parent
    for source in dependency.get('Inputs', []):
        if 'Root' in source:
            resolved = (owner / source['Path']).as_posix() if source['Root'] in ('Packet', 'Tool') else source['Path']
            get(resolved)
for group in ('Inputs', 'DriverSources'):
    for row in plan[group]:
        get(row['Path'])
sys.path.insert(0, str(R / prefix))
spec = importlib.util.spec_from_file_location('exact_tests1599', R / prefix / 'test_source_bindings.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)
with (A / 'controls.txt').open('w') as output:
    result = unittest.TextTestRunner(stream=output, verbosity=2).run(unittest.defaultTestLoader.loadTestsFromModule(module))
assert result.wasSuccessful()
import source_bindings
roles = {}
for role, fields in source_bindings.PRODUCER_FIELDS.items():
    raw = json.loads((R / plan['PlanPins'][role]['Path']).read_bytes())
    roles[role] = []
    for field in sorted(fields):
        changed = dict(raw)
        changed.pop(field)
        try:
            source_bindings.require_producer_fields(role, changed)
        except ValueError as error:
            roles[role].append({'Field': field, 'Rejected': True, 'Error': str(error)})
        else:
            raise AssertionError((role, field, 'missing field accepted'))
old = subprocess.check_output(['git', '--git-dir=' + G, 'show', BASE + ':tools/validate-async-microstates.ps1']).decode()
try:
    source_bindings.powershell_pin(R, old)
except ValueError as error:
    stale = {'Rejected': True, 'Error': str(error)}
else:
    raise AssertionError('Old A372 default accepted')
readme = get('docs/validation/part-569/ci-integrated-pins-and-reuse/README.md').decode()
assert 'its old cause remains unproven' in readme and 'Fresh actual async23 and whole-package hosted acceptance remain pending' in readme
report = {'Head': HEAD, 'Base': BASE, 'CurrentDefault': default[0], 'ActualRawPlanSha256': async_sha,
          'DefaultEqualsRawPlan': True, 'OldA372Rejected': stale, 'TestsRun': result.testsRun,
          'DeletionControls': roles, 'DeletionControlCount': sum(map(len, roles.values())),
          'ActualCommittedVerify': source_bindings.verify(R), 'HistoryScopePreserved': True,
          'NoProductionOrVmExecution': True, 'ReadOnlyGitObjectAccess': True,
          'BoundedScope': 'Current PS default and required existing-role fields/deletion controls. No hosted CI/full-regression/VM/API compatibility acceptance.',
          'Findings': [], 'NoFindingsWithinRequestedScope': True}
(A / 'bindings.json').write_text(json.dumps({'Files': list(rows.values())}, indent=2))
(A / 'review.json').write_text(json.dumps(report, indent=2))
print(json.dumps({'Head': HEAD, 'Default': default[0], 'Tests': result.testsRun, 'DeletionControls': report['DeletionControlCount'], 'Files': len(rows), 'Findings': 0}))
print('ReviewSha256', hashlib.sha256((A / 'review.json').read_bytes()).hexdigest().upper())
