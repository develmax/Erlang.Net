"""Root-owned corrected full353 execution2. No success inferred from a proposal or test count."""
from pathlib import Path
import hashlib
import importlib.util
import json
import os
import shutil
import sys

r = Path(__file__).resolve().parent.parent
p = r / 'artifacts/scheduler-affinity353-composition-plan2/composition-plan.json'
c = r / 'artifacts/scheduler-affinity353-full-candidate2'
o = r / 'artifacts/scheduler-affinity353-full-actual2'
sha = lambda path: hashlib.sha256(path.read_bytes()).hexdigest().upper()
load = lambda path: json.loads(path.read_bytes())
assert sha(p) == '3F8C5F95912959D7BE22BDDA3E6F39B696AC644EA98E5461C6B67C64771F06BC'
plan = load(p)
assert Path(plan['Destination']) == c
assert len(plan['Files']) == 3107 and len(plan['StableClosure']) == 3106

def verify(rows):
    for row in rows:
        path = c / row['Path']
        assert path.stat().st_size == row['Bytes'] and sha(path) == row['Sha256'], row['Path']

verify(plan['Files'])
focus_guard_path = r / sys.argv[3]
assert sha(focus_guard_path) == sys.argv[4].upper()
focus_guard = load(focus_guard_path)
assert focus_guard['Status'] == 'PASS_INDEPENDENT_ACTUAL_CORRECTED353_FOCUS40_NO_FULL_ACCEPTANCE'
assert focus_guard['ManagedPassed'] == 40 and focus_guard['ParseAll'] == 7
focus_freeze_path = r / 'artifacts/scheduler-affinity353-idle-focus2-source/extended-source-freeze.json'
assert sha(focus_freeze_path) == '513564D6C8BEDB585CF938EA85BD83D08296B0F4B24122635D4F170B9A9DBA6D'
focus_freeze = load(focus_freeze_path)
focus_rows = {row['Path']: row for row in focus_freeze['Rows']}
code_rows = [row for row in plan['Files'] if row['Path'].startswith(('src/', 'tests/Erlang.Tests/', 'tools/Erlang.Differential/'))]
assert code_rows
for row in code_rows:
    assert row['Sha256'] == focus_rows[row['Path']]['Sha256'] and row['Bytes'] == focus_rows[row['Path']]['Bytes'], row['Path']
assert sha(c / 'src/Erlang.Runtime/RuntimeActorDispatcher.cs') == '0AF57D1F9F65CE153129E6D9718B3F545F7CE83FFE409E6889F51957040C3555'
assert not (c / 'artifacts/tests.json').exists()
assert not (c / 'artifacts/integration-results.json').exists()
assert not list((c / 'artifacts/term-order-children').glob('*/receipt.json'))
helper = r / 'artifacts/spawn-priority351-preparation/execute-root.py'
assert sha(helper) == 'C66ACEC6FFE57C57EFAABC96FD1DB00598D7AAD7D05C2EA814A146B759D4171A'
peer = r / sys.argv[1]
assert sha(peer) == sys.argv[2].upper()
assert load(peer)['Status'].startswith('PASS_')
spec = importlib.util.spec_from_file_location('owned_full353', helper)
m = importlib.util.module_from_spec(spec)
spec.loader.exec_module(m)
o.mkdir(exist_ok=False)
shutil.copyfile(p, o / 'composition-plan.json')
shutil.copyfile(peer, o / 'runner-source-review.json')
shutil.copyfile(focus_guard_path, o / 'corrected-focus-independent.json')
shutil.copyfile(c / 'docs/supported-mfas.json', o / 'registry-before.json')
shutil.copyfile(c / 'docs/validation/part-351/full/tests.json', o / 'previous-tests.json')
m.write(o / 'inputs.json', {'PlanSha256': sha(p), 'RunnerSha256': sha(Path(__file__)),
    'OwnershipHelperSha256': sha(helper), 'SourcePeerSha256': sha(peer), 'Candidate': str(c),
    'InputRows': 3107, 'StableRows': 3106, 'ExpectedTests': 7313, 'OldOrderedPrefix': 7297,
    'ExpectedIntegrationOutcomes': 11, 'ExpectedPackages': 2, 'RegistryWillNotBeRestored': True,
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
    m.write(o / 'stable-closure-after.json', {'VerifiedRows': 3106, 'PlanSha256': sha(p), 'Unchanged': True})
m.write(o / 'producer-completion.json', {'Status': 'ACTUAL_CANONICAL_FULL353_COMMAND_EXITED_ZERO_REQUIRES_INDEPENDENT_REVIEW',
    'IndependentAcceptance': False, 'OriginalOtpExecuted': False, 'MainChanged': False})
print('Actual full353 command exited zero. Raw outputs retained; independent review required.')
