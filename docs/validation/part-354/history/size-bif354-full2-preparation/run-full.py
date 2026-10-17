"""Root-owned corrected full354 execution2. No success inferred from a proposal or test count."""
from pathlib import Path
import hashlib
import importlib.util
import json
import os
import shutil
import sys

r = Path(__file__).resolve().parents[2]
p = r / 'artifacts/size-bif354-full2-preparation/composition-plan.json'
c = r / 'artifacts/size-bif354-full-candidate2'
o = r / 'artifacts/size-bif354-full-actual2'
sha = lambda path: hashlib.sha256(path.read_bytes()).hexdigest().upper()
load = lambda path: json.loads(path.read_bytes())
assert sha(p) == '2E0A920C615F96CE8FE0F4BA91DFE0EDB6DF4853ABB71801E78FF67E8C602A2B'
plan = load(p)
assert Path(plan['Destination']) == c
assert len(plan['Files']) == 3209 and len(plan['StableClosure']) == 3208

def verify(rows):
    for row in rows:
        path = c / row['Path']
        assert path.stat().st_size == row['Bytes'] and sha(path) == row['Sha256'], row['Path']

assert len(plan['OwnedPaths']) == 131
verify(plan['Files'])
for row in plan['InputPins']:
    path = r / row['Path']
    assert path.stat().st_size == row['Bytes'] and sha(path) == row['Sha256'], row['Path']
focus_guard_path = r / sys.argv[3]
assert sha(focus_guard_path) == sys.argv[4].upper()
focus_guard = load(focus_guard_path)
assert focus_guard['Status'] == 'PASS_INDEPENDENT354_DIAGNOSTIC_ACQUISITION_WITH_SEPARATE_SEMANTIC_OUTCOMES'
assert focus_guard['SemanticPassed'] == 44 and focus_guard['SemanticFailed'] == 0 and focus_guard['ManagedExecuted'] == 44 and focus_guard['ParseAll'] == 36
focus_freeze_path = r / 'artifacts/size-bif354-focus-execution-proposal14/source-freeze.json'
assert sha(focus_freeze_path) == 'F804F2FF04F8ACCA2465270DA0155448B7DA6FD1A3ED6C6C9708C7FA2AF71B1A'
focus_freeze = load(focus_freeze_path)
focus_inputs = load(r / 'artifacts/size-bif354-focus-execution-proposal14/execution-inputs.json')
assert sha(r / 'artifacts/size-bif354-focus-execution-proposal14/execution-inputs.json') == next(row['Sha256'] for row in focus_freeze['Files'] if row['Path'] == 'execution-inputs.json')
focus_rows = {row['Path']: row for row in focus_inputs['Rows']}
assert sha(focus_guard_path) == plan['AcceptedFocusGuardSha256']
code_rows = [row for row in plan['Files'] if row['Path'].startswith(('src/', 'tests/Erlang.Tests/', 'tools/Erlang.Differential/'))]
assert code_rows
for row in code_rows:
    assert row['Sha256'] == focus_rows[row['Path']]['Sha256'] and row['Bytes'] == focus_rows[row['Path']]['Bytes'], row['Path']
assert len(load(c / 'docs/mfa-compatibility.json')['VerifiedSourceFiles']) == 115
assert not (c / 'artifacts/tests.json').exists()
assert not (c / 'artifacts/integration-results.json').exists()
assert not list((c / 'artifacts/term-order-children').glob('*/receipt.json'))
helper = r / 'artifacts/spawn-priority351-preparation/execute-root.py'
assert sha(helper) == 'C66ACEC6FFE57C57EFAABC96FD1DB00598D7AAD7D05C2EA814A146B759D4171A'
peer = r / sys.argv[1]
assert sha(peer) == sys.argv[2].upper()
assert load(peer)['Status'].startswith('PASS_')
spec = importlib.util.spec_from_file_location('owned_full354', helper)
m = importlib.util.module_from_spec(spec)
spec.loader.exec_module(m)
o.mkdir(exist_ok=False)
shutil.copyfile(p, o / 'composition-plan.json')
shutil.copyfile(peer, o / 'runner-source-review.json')
shutil.copyfile(focus_guard_path, o / 'corrected-focus-independent.json')
shutil.copyfile(c / 'docs/supported-mfas.json', o / 'registry-before.json')
shutil.copyfile(c / plan['PreviousTestReport'], o / 'previous-tests.json')
m.write(o / 'inputs.json', {'PlanSha256': sha(p), 'RunnerSha256': sha(Path(__file__)),
    'OwnershipHelperSha256': sha(helper), 'SourcePeerSha256': sha(peer), 'Candidate': str(c),
    'InputRows': 3209, 'StableRows': 3208, 'ExpectedTests': 7342, 'OldOrderedPrefix': 7313,
    'ExpectedIntegrationOutcomes': 11, 'ExpectedPackages': 2, 'RegistryWillNotBeRestored': True, 'Attempt': 2, 'FailedAttempt1Retained': True,
    'OriginalOtpExecuted': False, 'AcceptedFocusGuardSha256': sha(focus_guard_path),
    'FocusedProductionAndTestInputRowsIdentical': len(code_rows)})
env = dict(os.environ, DOTNET_PROCESSOR_COUNT='2', MSBUILDDISABLENODEREUSE='1', DOTNET_CLI_TELEMETRY_OPTOUT='1')
command = ['C:/Users/Maksim/.cache/codex-runtimes/codex-primary-runtime/dependencies/native/powershell/pwsh.exe',
    '-NoProfile', '-File', str(c / 'tools/validate.ps1')]
try:
    m.owned(command, o, 'full', 900, env, c)
finally:
    shutil.copyfile(c / 'docs/supported-mfas.json', o / 'raw-supported-mfas.json')
    outputs = []
    for name in ['artifacts/tests.json', 'artifacts/integration-results.json', 'artifacts/readiness/module-readiness.json',
                 'artifacts/readiness/MODULE_READINESS.md', 'artifacts/readiness/API_COMPATIBILITY.md',
                 'artifacts/tool-readiness/module-readiness.json']:
        path = c / name
        if path.is_file():
            dest = o / name
            dest.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(path, dest)
            outputs.append({'Path': name, 'Bytes': path.stat().st_size, 'Sha256': sha(path)})
    children = [{'Path': path.relative_to(c).as_posix(), 'Sha256': sha(path)}
        for path in sorted((c / 'artifacts/term-order-children').glob('*/receipt.json'))]
    packages = [{'Path': path.relative_to(c).as_posix(), 'Bytes': path.stat().st_size, 'Sha256': sha(path)}
        for path in sorted((c / 'artifacts/packages').glob('*.nupkg'))]
    dlls = [{'Path': path.relative_to(c).as_posix(), 'Bytes': path.stat().st_size, 'Sha256': sha(path)}
        for path in sorted((c / 'tests/Erlang.Tests/bin/Debug/net10.0').glob('*.dll'))]
    m.write(o / 'captured-outputs.json', {'Outputs': outputs, 'Children': children, 'Packages': packages, 'TestDlls': dlls,
        'RawRegistrySha256': sha(o / 'raw-supported-mfas.json'), 'RegistryRestored': False})
    verify(plan['StableClosure'])
    m.write(o / 'stable-closure-after.json', {'VerifiedRows': 3208, 'PlanSha256': sha(p), 'Unchanged': True})
actual_tests = load(o / 'artifacts/tests.json')
previous = load(o / 'previous-tests.json')
assert actual_tests['Passed'] == 7342 and actual_tests['Failed'] == 0
assert actual_tests['Tests'][:7313] == previous['Tests'] or [x['Name'] for x in actual_tests['Tests'][:7313]] == [x['Name'] for x in previous['Tests']]
assert [x['Name'] for x in actual_tests['Tests'][7313:]] == plan['AddedTestNames']
assert len(load(o / 'artifacts/integration-results.json')) == 11
m.write(o / 'producer-completion.json' , {'Status': 'ACTUAL_CANONICAL_FULL354_COMMAND_EXITED_ZERO_REQUIRES_INDEPENDENT_REVIEW',
    'IndependentAcceptance': False, 'OriginalOtpExecuted': False, 'MainChanged': False})
print('Actual full354 command exited zero. Raw outputs retained; independent review required.')
