from pathlib import Path
import subprocess, shutil, hashlib, json

r = Path(__file__).resolve().parents[1]
root = r / 'artifacts/part269-node-main-cli-controls'
assert not root.exists()
root.mkdir()
tool = r / 'tools/Erlang.NodeIdentityContrasts/bin/Debug/net10.0'
rows = []
for name in ['wrong-pin', 'wrong-version', 'wrong-build-log-hash', 'wrong-source-hash', 'duplicate-cases', 'wrong-plan-pin', 'existing-output']:
    d = root / name
    shutil.copytree(tool, d / 'tool')
    oracle = d / 'oracle'
    release = oracle / 'releases/29'
    release.mkdir(parents=True)
    (release / 'OTP_VERSION').write_text('29.1.1\n')
    log = d / 'build.log'
    log.write_bytes(b'owned verification-only fixture\n')
    evidence = dict(SourceCommit='ad05823719d77c8faee87348ea39513d4e2f99c5', SourceTag='OTP-29.1.1',
                    SourceBuildCompleted=True, InstalledOtpRoot=str(oracle), SourceBuildLog=str(log),
                    SourceBuildLogSha256=hashlib.sha256(log.read_bytes()).hexdigest().upper())
    if name == 'wrong-pin':
        evidence['SourceCommit'] = 'wrong'
    elif name == 'wrong-version':
        (release / 'OTP_VERSION').write_text('wrong\n')
    elif name == 'wrong-build-log-hash':
        evidence['SourceBuildLogSha256'] = '0' * 64
    elif name == 'wrong-source-hash':
        source = d / 'tool/node_identity_probe.erl'
        source.write_bytes(source.read_bytes() + b'\n')
    elif name in ['duplicate-cases', 'wrong-plan-pin']:
        plan = d / 'tool/source-plan.json'
        x = json.loads(plan.read_text())
        if name == 'duplicate-cases':
            x['Scenarios'] = ['start', 'start', 'start-stop-restart']
        else:
            x['Pin'] = 'wrong'
        plan.write_text(json.dumps(x))
    output = d / 'output'
    if name == 'existing-output':
        output.mkdir()
        (output / 'sentinel').write_bytes(b'owned-existing-output\x00\xff')
    before = {p.name: p.read_bytes() for p in output.glob('*')} if output.exists() else None
    proof = d / 'evidence.json'
    proof.write_text(json.dumps(evidence))
    invocation = ['dotnet', str(d / 'tool/Erlang.NodeIdentityContrasts.dll'), str(oracle), str(output), str(proof)]
    actual = subprocess.run(invocation, stdout=subprocess.PIPE, stderr=subprocess.PIPE, timeout=15)
    (d / 'stdout').write_bytes(actual.stdout)
    (d / 'stderr').write_bytes(actual.stderr)
    (d / 'exit').write_text(str(actual.returncode) + '\n')
    after = {p.name: p.read_bytes() for p in output.glob('*')} if output.exists() else None
    assert actual.returncode != 0 and after == before, name
    rows.append(dict(Name=name, Exit=actual.returncode, OutputUnchanged=True, NativeProcessStarted=False))
(root / 'control.json').write_text(json.dumps(dict(Status='PASS', NativeExecuted=False, ActualCliRefusals=7, Rows=rows), indent=2) + '\n')
print('Seven actual Node CLI refusal gates preserve output; no native executable exists in controlled oracle.')
