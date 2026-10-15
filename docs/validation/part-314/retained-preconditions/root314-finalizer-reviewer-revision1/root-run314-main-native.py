"""Root-only post-full actual Main native checks; no original VM execution."""
from pathlib import Path
import hashlib
import importlib.util
import json
import os
import subprocess
import sys
import time

sys.dont_write_bytecode = True
root = Path.cwd()
art = root / 'artifacts'
proof = art / 'root-part314-main1'
read = lambda p: json.loads(p.read_bytes())
sha = lambda b: hashlib.sha256(b).hexdigest().upper()
freeze = read(proof / 'source-freeze.json')
closure = read(proof / 'source-resource-closure.json')
assert sha((proof / 'source-resource-closure.json').read_bytes()) == freeze['FinalSourceClosureSha256']
assert read(proof / 'full-exit.json')['ExitCode'] == 0
assert read(art / 'tests.json')['Passed'] == 6641 and read(art / 'tests.json')['Failed'] == 0
assert len(read(art / 'integration-results.json')) == 11

def source_exact():
    for row in closure['Files']:
        raw = (root / row['Path']).read_bytes()
        assert len(raw) == row['Bytes'] and sha(raw) == row['Sha256'], row['Path']

source_exact()
native = art / 'root-part313-native-windows-build1/reallocation_fixture.dll'
old = art / 'spawn-protocol-nif-290-38688662194-terminal/artifact-11951535100/nif-fixture-build/fixture.dll'
layout = art / 'root-part313-native-windows-layout1/layout.json'
pins = {native: 'EAFD8CECED541CCFC0C02CE04EC19428448BD64630F343D28C360E19DE848E21',
        old: '235D9E8997B7760E120ECD5CFACA29F01D0936A8FD89EF8B23DDF7D0DB43883F',
        layout: '35231D9DA206E7A3E3BB07F9CA59BA67FE91AE3AF34B64C920D0FE841CE30D45'}
for path, digest in pins.items():
    assert sha(path.read_bytes()) == digest, path
assert read(layout.with_name('guard.json'))['Status'] == 'ACTUAL_WINDOWS_X64_PINNED_HEADER_LAYOUT_SEVEN_FIELDS_PASS'
assert sha((art / 'nif-reallocation-native309-windows-static-peer1/static-review.json').read_bytes()) == 'EC1E721C6A0D49BA065EC509ADA3AAFA2691C191A45BD4F78DBDBEE05CCFABED'
tool = root / 'tools/Erlang.NifNativeBackend/bin/Debug/net10.0/Erlang.NifNativeBackend.dll'
runtime = tool.with_name('Erlang.Runtime.dll')
assert runtime.read_bytes() == (root / 'src/Erlang.Runtime/bin/Debug/net10.0/Erlang.Runtime.dll').read_bytes()
pins[tool] = sha(tool.read_bytes())
pins[runtime] = sha(runtime.read_bytes())
out = art / 'root-part314-main-windows-native1'
assert not out.exists()
out.mkdir()
env = os.environ.copy()
env['DOTNET_PROCESSOR_COUNT'] = '2'
for name in ['DOTNET_STARTUP_HOOKS', 'DOTNET_ADDITIONAL_DEPS', 'DOTNET_SHARED_STORE']:
    env.pop(name, None)

for role, arguments in [('legacy', [str(old), pins[old]]),
                        ('reallocation', ['--reallocation-probe', str(native), pins[native],
                                          str(old), pins[old], str(layout), pins[layout]])]:
    source_exact()
    command = ['dotnet', str(tool), *arguments]
    attempt = {'Role': role, 'Command': command, 'StartedUnixSeconds': time.time(),
               'FreshMainFullExitCode': 0, 'FullPassed': 6641,
               'SourceFreezeSha256': sha((proof / 'source-freeze.json').read_bytes()),
               'SourceClosureSha256': freeze['FinalSourceClosureSha256'],
               'OriginalExecuted': False, 'AutomaticRetryAllowed': False,
               'Bindings': [{'Path': p.relative_to(root).as_posix(), 'Sha256': digest}
                            for p, digest in pins.items()]}
    (out / (role + '.attempt.json')).write_text(json.dumps(attempt, indent=2) + '\n', encoding='utf-8')
    try:
        result = subprocess.run(command, cwd=root, env=env, capture_output=True, timeout=120)
        stdout, stderr, code, timed = result.stdout, result.stderr, result.returncode, False
    except subprocess.TimeoutExpired as failure:
        stdout, stderr, code, timed = failure.stdout or b'', failure.stderr or b'', None, True
    (out / (role + '.stdout')).write_bytes(stdout)
    (out / (role + '.stderr')).write_bytes(stderr)
    receipt = {'Role': role, 'ExitCode': code, 'TimedOut': timed,
               'ElapsedSeconds': time.time() - attempt['StartedUnixSeconds'],
               'NativeExecuted': True, 'OriginalExecuted': False, 'MainExecution': True}
    (out / (role + '.process.json')).write_text(json.dumps(receipt, indent=2) + '\n', encoding='utf-8')
    assert code == 0 and not timed and not stderr, receipt
    for path, digest in pins.items():
        assert sha(path.read_bytes()) == digest, path
    source_exact()
    print(json.dumps(receipt))

# Passive semantic review is a separate step; these are execution receipts only.
print('Actual Main legacy and reallocation processes completed; passive review pending')
