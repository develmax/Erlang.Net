"""Actual bounded trusted Windows probes after independent final source review."""
from pathlib import Path
import hashlib
import json
import os
import subprocess
import sys
import time

sys.dont_write_bytecode = True
import argparse
parser = argparse.ArgumentParser(description='Reviewed post-full Main Windows trusted probes with explicit source/count authority; no build or original VM.')
parser.add_argument('--main-proof', required=True, type=Path)
parser.add_argument('--part', required=True, type=int)
parser.add_argument('--expected-full-passed', required=True, type=int)
parser.add_argument('--expected-integration-results', required=True, type=int)
parser.add_argument('--legacy-input-count', required=True, type=int)
parser.add_argument('--reallocation-input-count', required=True, type=int)
parser.add_argument('--main-freeze-sha256', required=True)
parser.add_argument('--full-guard-sha256', required=True)
parser.add_argument('--source-peer', required=True, type=Path)
parser.add_argument('--source-peer-sha256', required=True)
parser.add_argument('--tool-sha256', required=True)
parser.add_argument('--runtime-sha256', required=True)
parser.add_argument('--output', required=True, type=Path)
parser.add_argument('--tested-freeze', required=True, type=Path)
parser.add_argument('--tested-freeze-sha256',required=True)
parser.add_argument('--tested-actual-guard', required=True, type=Path)
parser.add_argument('--tested-actual-guard-sha256',required=True)
parser.add_argument('--main-composition-peer', required=True, type=Path)
parser.add_argument('--main-composition-peer-sha256',required=True)
parser.add_argument('--closure-sha256',required=True)
args = parser.parse_args()

root = Path.cwd().resolve()
assert (root / '.git').exists(), 'Main repository required'
assert args.output.resolve().is_relative_to(root/'artifacts') and args.output.resolve()!=args.main_proof.resolve()
assert args.part >= 331 and args.expected_full_passed > 0 and args.expected_integration_results > 0
candidate = root
proof = args.main_proof.resolve()
assert proof.is_relative_to(root/'artifacts')
freeze_path = proof / 'source-freeze.json'
closure_path = proof / 'source-resource-closure.json'
peer_path = args.source_peer.resolve()
assert peer_path.is_relative_to(root / 'artifacts')
out = args.output.resolve()
sha = lambda raw: hashlib.sha256(raw).hexdigest().upper()
read = lambda path: json.loads(path.read_text(encoding='utf-8-sig'))

# Fixed review inputs are passive files; no imported capture runner can execute.
import re
CONFIGURATION = Path(__file__).resolve().parent / 'configuration.json'
assert sha(CONFIGURATION.read_bytes()) == '8742307F6361D8ABD53B29FA0E23E9979B0BA767DB6EE4703FB70F357A239857'
CONFIG = read(CONFIGURATION)
assert re.fullmatch('[A-Fa-f0-9]{64}',args.tested_freeze_sha256) and re.fullmatch('[A-Fa-f0-9]{64}',args.closure_sha256)
CONFIG['TestedFreezeSha256']=args.tested_freeze_sha256.upper()
CONFIG['MainClosureSha256']=args.closure_sha256.upper()
MAIN_FREEZE=read(proof / 'source-freeze.json')
MAIN_PLANS=MAIN_FREEZE['Plans']
assert len(MAIN_PLANS)==2 and {x['Path'] for x in MAIN_PLANS}=={'tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json','tools/Erlang.NifReallocationObservations/SOURCE-PLAN.json'}
EXPECTED_INPUTS={'tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json':args.legacy_input_count,'tools/Erlang.NifReallocationObservations/SOURCE-PLAN.json':args.reallocation_input_count}
assert all(value > 0 for value in EXPECTED_INPUTS.values())
assert {item['Path']:item['Inputs'] for item in MAIN_PLANS}==EXPECTED_INPUTS

assert re.fullmatch('[A-Fa-f0-9]{64}',args.main_freeze_sha256) and re.fullmatch('[A-Fa-f0-9]{64}',args.source_peer_sha256)
tested_freeze = args.tested_freeze.resolve()
tested_guard = args.tested_actual_guard.resolve()
composition_peer = args.main_composition_peer.resolve()
for authority in (tested_freeze, tested_guard, composition_peer):
    assert authority.is_relative_to(root/'artifacts')

def independent_inputs_exact():
    assert sha(tested_freeze.read_bytes()) == args.tested_freeze_sha256.upper() == MAIN_FREEZE['TestedFreezeSha256']
    assert sha(tested_guard.read_bytes()) == args.tested_actual_guard_sha256.upper() == MAIN_FREEZE['TestedActualGuardSha256']
    assert sha(peer_path.read_bytes()) == args.source_peer_sha256.upper() == MAIN_FREEZE['IndependentSourcePeerSha256']
    assert sha(composition_peer.read_bytes()) == args.main_composition_peer_sha256.upper()
    composition = read(composition_peer)
    assert composition['Status'] == 'PASS_INDEPENDENT_ACTUAL_MAIN331_COMPOSITION_AND_CORRECTED_FULL_GUARD_SOURCE'
    assert composition['SourceClosureRowsRehashed'] == MAIN_FREEZE['SourceClosurePaths']
    assert composition['OwnedSourcePaths'] == MAIN_FREEZE['SourcePaths']
    assert composition['OwnedCSharpFiles'] == MAIN_FREEZE['CSharpPaths']
    assert composition['All15MainCSharpRowsExactActuallyTestedFreeze'] is True
    assert composition['TestedFreezeSha256'] == MAIN_FREEZE['TestedFreezeSha256']
    assert composition['TestedActualGuardSha256'] == MAIN_FREEZE['TestedActualGuardSha256']
    assert composition['OriginalSourcePeerSha256'] == MAIN_FREEZE['IndependentSourcePeerSha256']
    assert composition['SourceFreezeSha256'] == args.main_freeze_sha256.upper()
    assert composition['SourceClosureSha256'] == args.closure_sha256.upper() == MAIN_FREEZE['SourceClosureSha256']
    implementation = read(peer_path)
    assert str(implementation['Status']).startswith('PASS') and implementation['SourceFreezeSha256'] == MAIN_FREEZE['TestedFreezeSha256']
    actual = read(tested_guard)
    assert actual['Passed'] == MAIN_FREEZE['ExpectedTests'] == args.expected_full_passed and actual['Failed'] == 0
    assert actual['FormattedSourceFreezeSha256'] == MAIN_FREEZE['TestedFreezeSha256'] and actual['All6747PreviousOrderedTestNamesRetained'] is True
    assert actual['NewCompiledScenarios'] == 21 and actual['NewDirectMfaContracts'] == 6 and actual['NewHostGroups'] == 1
    tested = read(tested_freeze)
    assert len(tested['Files']) == MAIN_FREEZE['CSharpPaths'] == 15
    frozen_main = {row['Path']:row for row in MAIN_FREEZE['Files']}
    for row in tested['Files']:
        assert frozen_main[row['Path']] == row
        raw = (root/row['Path']).read_bytes()
        assert len(raw) == row['Bytes'] and sha(raw) == row['Sha256'], row['Path']

def references_exact():
    independent_inputs_exact()
    for item in CONFIG['References']:
        raw = (root / item['Path']).read_bytes()
        assert len(raw) == item['Bytes'] and sha(raw) == item['Sha256'], item['Path']
    for item in MAIN_PLANS:
        assert len(json.loads((root/item['Path']).read_bytes())['Files'])==EXPECTED_INPUTS[item['Path']]
        raw = (root / item['Path']).read_bytes()
        assert b'\r' not in raw and not raw.startswith(b'\xef\xbb\xbf') and sha(raw) == item['Sha256']
        workflow = (root / item['Workflow']).read_bytes().replace(b'\r\n', b'\n')
        pins = re.findall(rb'(?m)^\s*' + item['Pin'].encode() + rb': ([A-F0-9]{64})$', workflow)
        assert pins == [item['Sha256'].encode()]
references_exact()
assert CONFIG['IndependentPeerRole']=='FINAL_SOURCE_COMPOSITION_PASSIVE_REVIEW'
assert os.name == 'nt' and not out.exists()
freeze = read(freeze_path)
assert args.source_peer_sha256.upper() == freeze['IndependentSourcePeerSha256']
closure = read(closure_path)
assert freeze['SourcePaths'] == len(freeze['Files']) and freeze['CSharpPaths'] == sum(x['Path'].endswith('.cs') for x in freeze['Files'])
assert len(closure['Files']) == freeze['SourceClosurePaths']
assert sha(freeze_path.read_bytes()) == args.main_freeze_sha256.upper() 
assert sha(closure_path.read_bytes()) == freeze['SourceClosureSha256'] == args.closure_sha256.upper()
assert sha(peer_path.read_bytes()) == args.source_peer_sha256.upper()
assert read(peer_path).get('Passed') is True or str(read(peer_path).get('Status', '')).startswith('PASS')
independent_inputs_exact()
full_path = proof / 'full-guard.json'
full = read(full_path)
assert sha(full_path.read_bytes()) == args.full_guard_sha256.upper()
assert str(full['Status']).startswith(f'PASS_ACTUAL_MAIN{args.part}_')
assert full['Passed'] == freeze['ExpectedTests'] and full['Failed'] == 0 and full['IntegrationResults'] == args.expected_integration_results and freeze['ExpectedTests']==args.expected_full_passed
assert full['SourceFreezeSha256'] == sha(freeze_path.read_bytes()) and full['SourceClosureSha256'] == sha(closure_path.read_bytes())
full_exit = read(proof / 'full-exit.json')
assert full_exit['ExitCode'] == 0 and full_exit['TimedOut'] is False and full_exit['Settled'] is True

def source_exact():
    references_exact()
    for row in closure['Files']:
        raw = (candidate / row['Path']).read_bytes()
        assert len(raw) == row['Bytes'] and sha(raw) == row['Sha256'], row['Path']

source_exact()
old = root / 'artifacts/spawn-protocol-nif-290-38688662194-terminal/artifact-11951535100/nif-fixture-build/fixture.dll'
native = root / 'artifacts/root-part313-native-windows-build1/reallocation_fixture.dll'
layout = root / 'artifacts/root-part313-native-windows-layout1/layout.json'
pins = {old: '235D9E8997B7760E120ECD5CFACA29F01D0936A8FD89EF8B23DDF7D0DB43883F', native: 'EAFD8CECED541CCFC0C02CE04EC19428448BD64630F343D28C360E19DE848E21', layout: '35231D9DA206E7A3E3BB07F9CA59BA67FE91AE3AF34B64C920D0FE841CE30D45'}
for path, expected in pins.items():
    assert sha(path.read_bytes()) == expected, path
assert read(root / 'artifacts/root-part315-main-windows-native1/guard.json')['FullPassed'] == 6642
assert read(root / 'artifacts/root-part316-main-windows-native1/guard.json')['FullPassed'] == 6654
tool = candidate / 'tools/Erlang.NifNativeBackend/bin/Debug/net10.0/Erlang.NifNativeBackend.dll'
runtime = tool.with_name('Erlang.Runtime.dll')
assert runtime.read_bytes() == (candidate / 'src/Erlang.Runtime/bin/Debug/net10.0/Erlang.Runtime.dll').read_bytes()
assert sha(tool.read_bytes()) == args.tool_sha256.upper()
assert sha(runtime.read_bytes()) == args.runtime_sha256.upper()
for path in sorted(tool.parent.rglob('*')):
    if path.is_file() and path.suffix in {'.dll', '.json'}:
        pins[path] = sha(path.read_bytes())
assert tool in pins and runtime in pins
pins[full_path] = sha(full_path.read_bytes())
pins[freeze_path] = sha(freeze_path.read_bytes())
pins[closure_path] = sha(closure_path.read_bytes())
pins[peer_path] = sha(peer_path.read_bytes())
for authority in (tested_freeze, tested_guard, composition_peer):
    pins[authority] = sha(authority.read_bytes())
out.mkdir()
env = os.environ.copy()
env['DOTNET_PROCESSOR_COUNT'] = '2'
for key in ['DOTNET_STARTUP_HOOKS', 'DOTNET_ADDITIONAL_DEPS', 'DOTNET_SHARED_STORE']:
    env.pop(key, None)
roles = [
    ('legacy', [str(old), pins[old]]),
    ('provider-query', ['--native-payload-provider-query', str(old), pins[old]]),
    ('reallocation', ['--reallocation-probe', str(native), pins[native], str(old), pins[old], str(layout), pins[layout]])
]
for role, arguments in roles:
    source_exact()
    for path, expected in pins.items():
        assert sha(path.read_bytes()) == expected, path
    command = ['dotnet', str(tool), *arguments]
    attempt = {'Role': role, 'Command': command, 'StartedUnixSeconds': time.time(), 'SourceFreezeSha256': sha(freeze_path.read_bytes()), 'SourceClosureSha256': sha(closure_path.read_bytes()), 'SourcePeerSha256': sha(peer_path.read_bytes()), 'ActualFullGuardSha256': sha(full_path.read_bytes()), 'MainExecution': True, 'IndependentPeerRole': CONFIG['IndependentPeerRole'], 'Bindings': [{'Path': path.relative_to(root).as_posix(), 'Sha256': expected} for path, expected in pins.items()], 'NativeExecutionAuthorized': True, 'OriginalExecuted': False, 'AutomaticRetryAllowed': False}
    attempt_path = out / (role + '.attempt.json')
    with attempt_path.open('x', encoding='utf-8') as stream:
        json.dump(attempt, stream, indent=2)
        stream.write('\n')
    started = time.monotonic()
    try:
        process = subprocess.run(command, cwd=candidate, env=env, capture_output=True, timeout=120)
        stdout, stderr, code, timed_out = process.stdout, process.stderr, process.returncode, False
    except subprocess.TimeoutExpired as failure:
        stdout, stderr, code, timed_out = failure.stdout or b'', failure.stderr or b'', None, True
    (out / (role + '.stdout')).write_bytes(stdout)
    (out / (role + '.stderr')).write_bytes(stderr)
    receipt = {'Role': role, 'ExitCode': code, 'TimedOut': timed_out, 'ElapsedSeconds': time.monotonic() - started, 'StdoutSha256': sha(stdout), 'StderrSha256': sha(stderr), 'NativeProbeInvoked': True, 'NativeExecutionValidated': False, 'OriginalExecuted': False, 'MainExecution': True, 'ActualSdkAssemblies': [{'Path': str(path.relative_to(root)), 'Sha256': pins[path]} for path in [tool, runtime]]}
    (out / (role + '.process.json')).write_text(json.dumps(receipt, indent=2) + '\n', encoding='utf-8')
    source_exact()
    for path, expected in pins.items():
        assert sha(path.read_bytes()) == expected, path
    print(json.dumps(receipt))
    assert code == 0 and not timed_out and not stderr, role + ': preserve first capture; no retry'
print('Actual post-full Main processes completed; independent passive typed review remains required.')
