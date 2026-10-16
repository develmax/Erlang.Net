import argparse
import hashlib
import json
import os
from pathlib import Path
import signal
import subprocess
import time

HERE = Path(__file__).resolve().parent
PIN = 'ad05823719d77c8faee87348ea39513d4e2f99c5'

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()

def save(path, value):
    with path.open('x', encoding='utf-8', newline='\n') as stream:
        json.dump(value, stream, indent=2)
        stream.write('\n')

def binding(path):
    return dict(Path=str(path), Bytes=path.stat().st_size, Sha256=sha(path))

p = argparse.ArgumentParser()
p.add_argument('mode', choices=['sdk', 'original', 'compare'])
p.add_argument('--collector', type=Path)
p.add_argument('--out', type=Path, required=True)
p.add_argument('--subject-sha', required=True)
p.add_argument('--subject-root', type=Path, required=True)
p.add_argument('--verify-subject-git', action='store_true')
p.add_argument('--oracle', type=Path)
p.add_argument('--otp-source', type=Path)
a = p.parse_args()
assert len(a.subject_sha) == 40 and all(x in '0123456789abcdef' for x in a.subject_sha)
a.out.mkdir(parents=True, exist_ok=True)
freeze = json.loads((HERE / 'source-freeze.json').read_bytes())
def exact():
    for row in freeze['Files']:
        path = HERE / row['Path']
        assert path.stat().st_size == row['Bytes'] and sha(path) == row['Sha256'], row['Path']
exact()
if a.verify_subject_git:
    actual = subprocess.check_output(['git', '-C', str(a.subject_root), 'rev-parse', 'HEAD'], text=True).strip()
    assert actual == a.subject_sha
if a.mode == 'compare':
    rows = []
    for name in ['named', 'anonymous']:
        sdk = json.loads((a.out / f'sdk-{name}.json').read_bytes())
        original = json.loads((a.out / f'original-{name}.json').read_bytes())
        assert sdk['Status'] == original['Status'] == 'PASS'
        assert original['OriginalExecuted'] and original['OriginalExitCode'] == 0
        assert sdk['SourceSha256'] == original['SourceSha256'] == sha(HERE / f'{name}.erl')
        assert sdk['Actual'] == original['Actual'] == sdk['Expected'] == original['Expected']
        rows.append(dict(Case=name, Actual=sdk['Actual'], SourceSha256=sdk['SourceSha256'], Sdk=binding(a.out / f'sdk-{name}.json'), Original=binding(a.out / f'original-{name}.json')))
    result = dict(Status='PASS_BOTH_GUEST_OBSERVABLE_RECURSIVE_CLEANUP_COMPARISONS', SubjectCommit=a.subject_sha, OriginalSourceCommit=PIN, SourceFreeze=binding(HERE / 'source-freeze.json'), Cases=rows, Scope='Finite depth256 named and anonymous external-kill cleanup observations. No equivalence claim for host leases, Unwound or physical resources.')
    save(a.out / 'comparison.json', result)
    print(json.dumps(result))
    raise SystemExit(0)
assert a.collector and a.collector.is_file()
if a.mode == 'original':
    assert a.oracle and a.oracle.is_file() and a.otp_source
    actual = subprocess.check_output(['git', '-C', str(a.otp_source), 'rev-parse', 'HEAD'], text=True).strip()
    assert actual == PIN
    resolved_oracle = a.oracle.resolve(strict=True)
    installed_root = resolved_oracle.parent.parent
    version_files = list((installed_root / 'releases').glob('*/OTP_VERSION'))
    assert any(path.read_text().strip() == '29.1.1' for path in version_files)
    save(a.out / 'original-provenance.json', dict(SourceCommit=actual, Oracle=binding(a.oracle), ResolvedOracle=binding(resolved_oracle), InstalledOtpRoot=str(installed_root), OtpVersionFiles=[binding(path) for path in version_files], Collector=binding(a.collector), SubjectCommit=a.subject_sha))
binaries = [binding(path) for path in sorted(a.collector.parent.glob('*.dll'))]
subject_binaries = a.subject_root / 'tools/Erlang.Differential/bin/Debug/net10.0'
subject_pins = []
for path in sorted(subject_binaries.glob('*.dll')):
    copied = a.collector.parent / path.name
    assert copied.is_file() and sha(copied) == sha(path), path.name
    subject_pins.append(binding(path))
assert len(subject_pins) >= 8
for name in ['named', 'anonymous']:
    report = a.out / f'{a.mode}-{name}.json'
    assert not report.exists()
    command = ['dotnet', str(a.collector.resolve()), a.mode, str(HERE / f'{name}.erl'), str(report.resolve())]
    if a.mode == 'original':
        command.append(str(a.oracle.resolve()))
    save(a.out / f'{a.mode}-{name}.attempt.json', dict(Command=command, SubjectCommit=a.subject_sha, SourceFreeze=binding(HERE / 'source-freeze.json'), Source=binding(HERE / f'{name}.erl'), Binaries=binaries, SubjectBinaries=subject_pins, DeadlineSeconds=60))
    start = time.monotonic()
    env = dict(os.environ, DOTNET_PROCESSOR_COUNT='2', DOTNET_CLI_USE_MSBUILD_SERVER='0', MSBUILDDISABLENODEREUSE='1')
    process = subprocess.Popen(command, stdout=subprocess.PIPE, stderr=subprocess.PIPE, env=env, start_new_session=os.name != 'nt')
    timed_out = False
    try:
        output, error = process.communicate(timeout=60)
    except subprocess.TimeoutExpired:
        timed_out = True
        if os.name == 'nt':
            subprocess.run(['taskkill', '/PID', str(process.pid), '/T', '/F'], capture_output=True, timeout=10)
        else:
            os.killpg(process.pid, signal.SIGKILL)
        output, error = process.communicate(timeout=10)
    (a.out / f'{a.mode}-{name}.stdout').write_bytes(output)
    (a.out / f'{a.mode}-{name}.stderr').write_bytes(error)
    exact()
    for row in binaries:
        assert sha(Path(row['Path'])) == row['Sha256']
    for row in subject_pins:
        assert sha(Path(row['Path'])) == row['Sha256']
    result = dict(ExitCode=process.returncode, TimedOut=timed_out, Settled=process.poll() is not None, ElapsedSeconds=time.monotonic() - start, Report=binding(report) if report.exists() else None)
    save(a.out / f'{a.mode}-{name}.process.json', result)
    assert not timed_out and process.returncode == 0 and report.exists(), result
    actual = json.loads(report.read_bytes())
    assert actual['Status'] == 'PASS' and actual['SourceSha256'] == sha(HERE / f'{name}.erl')
    print(json.dumps(dict(Case=name, Mode=a.mode, Status=actual['Status'], Actual=actual['Actual'])))
