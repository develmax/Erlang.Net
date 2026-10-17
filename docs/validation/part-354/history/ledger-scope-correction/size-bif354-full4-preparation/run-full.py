"""Root-owned corrected full354 execution4. No success inferred from a proposal or test count."""
from pathlib import Path
import hashlib
import importlib.util
import json
import os
import shutil
import sys
import argparse

r = Path(__file__).resolve().parents[2]
p = r / 'artifacts/size-bif354-full4-preparation/composition-plan.json'
parser = argparse.ArgumentParser()
parser.add_argument('--approved-plan-sha256', required=True)
parser.add_argument('--source-peer', required=True)
parser.add_argument('--source-peer-sha256', required=True)
parser.add_argument('--accepted-red-green', required=True)
parser.add_argument('--accepted-red-green-sha256', required=True)
a = parser.parse_args()
c = r / 'artifacts/size-bif354-full-candidate4'
o = r / 'artifacts/size-bif354-full-actual4'
sha = lambda path: hashlib.sha256(path.read_bytes()).hexdigest().upper()
load = lambda path: json.loads(path.read_bytes())
assert sha(p) == a.approved_plan_sha256.upper()
plan = load(p)
assert Path(plan['Destination']) == c
assert len(plan['Files']) == len(plan['StableClosure']) + 1
assert plan['ExpectedFullTestsIfPasses'] == 7343 and plan['ExpectedOldOrderedPrefix'] == 7342 and plan['ExpectedNewTests'] == 1

def verify(rows):
    for row in rows:
        path = c / row['Path']
        assert path.stat().st_size == row['Bytes'] and sha(path) == row['Sha256'], row['Path']

assert plan['RuntimeSourceSha256'] == 'F2BBB954CE212ED87AE16FE8F50ACA14268A9A5C9A06812E11799EB7C270AB58'
verify(plan['Files'])
for row in plan['InputPins']:
    path = r / row['Path']
    assert path.stat().st_size == row['Bytes'] and sha(path) == row['Sha256'], row['Path']
red_green_path = (r / a.accepted_red_green).resolve()
assert red_green_path.is_relative_to(r / 'artifacts') and sha(red_green_path) == a.accepted_red_green_sha256.upper()
red_green = load(red_green_path)
assert red_green['Status'].startswith('PASS_')
# This is the single actual report explicitly accepted by Root; the wrapper does not fabricate probe outcomes.
assert sha(c / 'src/Erlang.Runtime/RuntimeActorDispatcher.cs') == plan['RuntimeSourceSha256']
assert len(load(c / 'docs/mfa-compatibility.json')['VerifiedSourceFiles']) == 115
assert not (c / 'artifacts/tests.json').exists()
assert not (c / 'artifacts/integration-results.json').exists()
assert not list((c / 'artifacts/term-order-children').glob('*/receipt.json'))
helper = r / 'artifacts/spawn-priority351-preparation/execute-root.py'
assert sha(helper) == 'C66ACEC6FFE57C57EFAABC96FD1DB00598D7AAD7D05C2EA814A146B759D4171A'
peer = (r / a.source_peer).resolve()
assert peer.is_relative_to(r / 'artifacts') and sha(peer) == a.source_peer_sha256.upper()
assert load(peer)['Status'].startswith('PASS_')
spec = importlib.util.spec_from_file_location('owned_full354', helper)
m = importlib.util.module_from_spec(spec)
spec.loader.exec_module(m)
o.mkdir(exist_ok=False)
shutil.copyfile(p, o / 'composition-plan.json')
shutil.copyfile(peer, o / 'runner-source-review.json')
shutil.copyfile(red_green_path, o / 'accepted-red-green.json')
shutil.copyfile(c / 'docs/supported-mfas.json', o / 'registry-before.json')
shutil.copyfile(c / plan['PreviousTestReport'], o / 'previous-tests.json')
shutil.copyfile(r / plan['PreviousAllTestNamesSource'], o / 'previous-registered-tests.json')
m.write(o / 'inputs.json', {'PlanSha256': sha(p), 'RunnerSha256': sha(Path(__file__)),
    'OwnershipHelperSha256': sha(helper), 'SourcePeerSha256': sha(peer), 'Candidate': str(c),
    'InputRows': len(plan['Files']), 'StableRows': len(plan['StableClosure']), 'ExpectedTests': 7343, 'OldOrderedPrefix': 7342,
    'ExpectedIntegrationOutcomes': 11, 'ExpectedPackages': 2, 'RegistryWillNotBeRestored': True, 'Attempt': 4, 'FailedAttempts1And2Retained': True, 'FailedFull3ReadinessMetadataRetained': True,
    'OriginalOtpExecuted': False, 'AcceptedRedGreenGuardSha256': sha(red_green_path),
    'OneRuntimePatchAndOneNewPermanentTest': True})
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
    m.write(o / 'stable-closure-after.json', {'VerifiedRows': len(plan['StableClosure']), 'PlanSha256': sha(p), 'Unchanged': True})
actual_tests = load(o / 'artifacts/tests.json')
previous = load(o / 'previous-registered-tests.json')
assert actual_tests['Passed'] == 7343 and actual_tests['Failed'] == 0
assert actual_tests['Tests'][:7342] == previous['Tests'] or [x['Name'] for x in actual_tests['Tests'][:7342]] == [x['Name'] for x in previous['Tests']]
assert [x['Name'] for x in actual_tests['Tests'][7342:]] == plan['AddedTestNames']
assert len(load(o / 'artifacts/integration-results.json')) == 11
m.write(o / 'producer-completion.json' , {'Status': 'ACTUAL_CANONICAL_FULL354_COMMAND_EXITED_ZERO_REQUIRES_INDEPENDENT_REVIEW',
    'IndependentAcceptance': False, 'OriginalOtpExecuted': False, 'MainChanged': False})
print('Actual full354 command exited zero. Raw outputs retained; independent review required.')
