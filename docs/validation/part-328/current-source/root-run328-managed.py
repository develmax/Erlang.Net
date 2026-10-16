"""Root-owned bounded execution of the frozen eight-case managed328 recipe."""
from pathlib import Path
import argparse, hashlib, json, os, subprocess, time, traceback
p = argparse.ArgumentParser()
p.add_argument('phase', choices=['build', 'run'])
p.add_argument('--focus-directory', required=True, type=Path)
p.add_argument('--expected-freeze', required=True)
p.add_argument('--mode', choices=['all', 'historical-lambda'], default='all')
args = p.parse_args()
r = Path.cwd().resolve()
d = args.focus_directory.resolve()
assert d.is_relative_to(r / 'artifacts')
recipe = r / 'artifacts/undefined-lambda328-managed-proposal1'
read = lambda q: json.loads(q.read_bytes())
sha = lambda q: hashlib.sha256(q.read_bytes()).hexdigest().upper()
write = lambda q, v: q.write_text(json.dumps(v, indent=2) + '\n', encoding='utf-8')
rf = recipe / 'source-freeze.json'
assert sha(rf) == '3AD72732175D212BA0EC547AB1ACA25532BE619244A086ED771221F71C2D2A62'
for row in read(rf)['Files']:
    q = recipe / row['Path']
    assert q.stat().st_size == row['Bytes'] and sha(q) == row['Sha256']
fp = d / ('formatted-source-freeze.json' if (d / 'formatted-source-freeze.json').exists() else 'source-freeze.json')
assert sha(fp) == args.expected_freeze.upper()
freeze = read(fp)
cl = d / ('formatted-source-resource-closure.json' if (d / 'formatted-source-resource-closure.json').exists() else 'source-resource-closure.json')
assert sha(cl) == freeze.get('SourceClosureSha256', freeze.get('FinalSourceClosureSha256'))
rows = read(cl)['Files']
assert len(rows) == freeze['SourceClosurePaths']
def exact():
    for row in rows:
        q = d / 'candidate' / row['Path']
        assert q.stat().st_size == row['Bytes'] and sha(q) == row['Sha256'], row['Path']
exact()
project = d / 'managed-project'
if args.phase == 'build':
    assert not project.exists()
    project.mkdir()
    for row in read(rf)['Files']:
        if row['Path'].endswith(('.cs', '.csproj')):
            (project / row['Path']).write_bytes((recipe / row['Path']).read_bytes())
else:
    assert project.is_dir()
out = d / ('managed-' + args.phase)
assert not out.exists()
out.mkdir()
report = out / 'actual.json'
settings = ['--disable-build-servers', '-m:1', '-nr:false', '-p:UseSharedCompilation=false', '-p:RestoreDisableParallel=true', '--nologo']
cmd = ['dotnet', 'build', str(project / 'UndefinedLambdaManagedFocus.csproj'), '-p:FocusDifferentialProject=' + str(d / 'candidate/tools/Erlang.Differential/Erlang.Differential.csproj'), *settings] if args.phase == 'build' else ['dotnet', str(project / 'bin/Debug/net10.0/UndefinedLambdaManagedFocus.dll'), str(report), str(fp), sha(fp), args.mode]
limit = 180 if args.phase == 'build' else 120
env = dict(os.environ, DOTNET_PROCESSOR_COUNT='2', DOTNET_CLI_USE_MSBUILD_SERVER='0', MSBUILDDISABLENODEREUSE='1', UseSharedCompilation='false', RestoreDisableParallel='true')
write(out / 'attempt.json', {'Command': cmd, 'SourceFreezeSha256': sha(fp), 'Mode': args.mode, 'DeadlineSeconds': limit})
start = time.monotonic()
child = None
timed = settled = passed = False
cleanup = {}
stdout = stderr = b''
failure = None
try:
    child = subprocess.Popen(cmd, cwd=r, stdout=subprocess.PIPE, stderr=subprocess.PIPE, env=env)
    try:
        stdout, stderr = child.communicate(timeout=limit)
        settled = True
    except subprocess.TimeoutExpired:
        timed = True
        try:
            killed = subprocess.run(['taskkill', '/PID', str(child.pid), '/T', '/F'], capture_output=True, timeout=10)
            cleanup['TaskkillExitCode'] = killed.returncode
        except subprocess.TimeoutExpired:
            cleanup['TaskkillTimedOut'] = True
        try:
            stdout, stderr = child.communicate(timeout=10)
            settled = child.poll() is not None and cleanup.get('TaskkillExitCode') == 0
        except subprocess.TimeoutExpired as e:
            stdout, stderr = e.stdout or b'', e.stderr or b''
            cleanup['FinalPipeDrainTimedOut'] = True
            child.kill()
            try:
                child.wait(timeout=2)
            except subprocess.TimeoutExpired:
                cleanup['FinalRootWaitTimedOut'] = True
    assert not timed and settled and child.returncode == 0
    if args.phase == 'run':
        v = read(report)
        assert v['RecipePassed'] and v['Executed'] == (8 if args.mode == 'all' else 1)
        if args.mode == 'all':
            assert v['FixturePassed'] == 8 and v['FixtureFailed'] == 0
        else:
            assert v['HistoricalUndefinedFunctionConfirmed'] == 1 and v['FixturePassed'] == 0
        for identity in v['ActualLoadedAssemblies']:
            binary = d / 'candidate/tools/Erlang.Differential/bin/Debug/net10.0' / (identity['Name'] + '.dll')
            assert binary.is_file() and identity['Sha256'] == sha(binary) == sha(Path(identity['Path']))
    exact()
    passed = True
except Exception:
    failure = traceback.format_exc()
finally:
    (out / 'stdout.log').write_bytes(stdout)
    (out / 'stderr.log').write_bytes(stderr)
    receipt = {'Phase': args.phase, 'Mode': args.mode, 'Command': cmd, 'ExitCode': child.returncode if child else None,
        'TimedOut': timed, 'OwnedProcessSettled': settled, 'OwnedPid': child.pid if child else None,
        'DeadlineSeconds': limit, 'CleanupMaximumSeconds': 22, 'Cleanup': cleanup,
        'ElapsedSeconds': time.monotonic() - start, 'SourceFreezeSha256': sha(fp),
        'SourceClosureSha256': sha(cl), 'SourceClosurePaths': len(rows),
        'RawReportSha256': sha(report) if report.exists() else None, 'Passed': passed,
        'ValidationError': failure, 'OriginalExecuted': False, 'MainEdited': False}
    write(out / 'process.json', receipt)
    print(json.dumps(receipt), flush=True)
if cleanup.get('FinalPipeDrainTimedOut'):
    os._exit(1)
raise SystemExit(0 if passed else 1)
