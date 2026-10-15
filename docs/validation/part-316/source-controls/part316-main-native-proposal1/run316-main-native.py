"""Actual bounded trusted Windows probes after independent final source review."""
from pathlib import Path
import hashlib
import json
import os
import subprocess
import sys
import time

sys.dont_write_bytecode = True
root = Path.cwd().resolve()
assert len(sys.argv) == 1
candidate = root
proof = root / 'artifacts/root-part316-main1'
freeze_path = proof / 'source-freeze.json'
closure_path = proof / 'source-resource-closure.json'
peer_path = root / 'artifacts/allocator-public-sys-profile316-final-beam310-source-peer.json'
out = root / 'artifacts/root-part316-main-windows-native1'
sha = lambda raw: hashlib.sha256(raw).hexdigest().upper()
read = lambda path: json.loads(path.read_text(encoding='utf-8-sig'))
assert os.name == 'nt' and not out.exists()
freeze = read(freeze_path)
closure = read(closure_path)
assert freeze['SourcePaths'] == 25 and freeze['CSharpPaths'] == 18
assert len(closure['Files']) == 2241
assert sha(freeze_path.read_bytes()) == '5561E5E2095E247B50AC60194D5FAABCCE18DD5DE7A4935EEEC657C4A6FFB8D2'
assert sha(closure_path.read_bytes()) == freeze['FinalSourceClosureSha256'] == '87CC2B5E1C8DDEF229E95DA38875431E73BA715D6EE71275F7DAEA5755E00299'
assert sha(peer_path.read_bytes()) == 'DC664622FAB0EB68B2DA4EDD31090DDFE7D6E39356E07F32D3063A6036E2AF41'
assert read(peer_path)['SourceClosureSha256'] == sha(closure_path.read_bytes())
full_path = proof / 'full-guard.json'
full = read(full_path)
assert full['Status'] == 'PASS_ACTUAL_MAIN316_6654_FULL_AND_ELEVEN_INTEGRATIONS'
assert full['Passed'] == 6654 and full['Failed'] == 0 and full['IntegrationResults'] == 11
assert full['SourceFreezeSha256'] == sha(freeze_path.read_bytes()) and full['SourceClosureSha256'] == sha(closure_path.read_bytes())
assert read(proof / 'full-exit.json')['ExitCode'] == 0

def source_exact():
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
assert sha(full_path.read_bytes()) == sha((proof / 'full-guard.json').read_bytes())
tool = candidate / 'tools/Erlang.NifNativeBackend/bin/Debug/net10.0/Erlang.NifNativeBackend.dll'
runtime = tool.with_name('Erlang.Runtime.dll')
assert runtime.read_bytes() == (candidate / 'src/Erlang.Runtime/bin/Debug/net10.0/Erlang.Runtime.dll').read_bytes()
for path in sorted(tool.parent.rglob('*')):
    if path.is_file() and path.suffix in {'.dll', '.json'}:
        pins[path] = sha(path.read_bytes())
assert tool in pins and runtime in pins
pins[full_path] = sha(full_path.read_bytes())
pins[freeze_path] = sha(freeze_path.read_bytes())
pins[closure_path] = sha(closure_path.read_bytes())
pins[peer_path] = sha(peer_path.read_bytes())
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
    attempt = {'Role': role, 'Command': command, 'StartedUnixSeconds': time.time(), 'SourceFreezeSha256': sha(freeze_path.read_bytes()), 'SourceClosureSha256': sha(closure_path.read_bytes()), 'SourcePeerSha256': sha(peer_path.read_bytes()), 'ActualFullGuardSha256': sha(full_path.read_bytes()), 'MainExecution': True, 'Bindings': [{'Path': path.relative_to(root).as_posix(), 'Sha256': expected} for path, expected in pins.items()], 'NativeExecutionAuthorized': True, 'OriginalExecuted': False, 'AutomaticRetryAllowed': False}
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
