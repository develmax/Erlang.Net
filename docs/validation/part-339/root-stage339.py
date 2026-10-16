from pathlib import Path
import hashlib
import json
import shutil
import subprocess

r = Path.cwd()
a = r / 'artifacts'
b = r / 'docs/validation/part-339'
j = lambda p: json.loads(p.read_bytes())
h = lambda raw: hashlib.sha256(raw).hexdigest().upper()
head = 'c8f46a6b60df1d0921df55855e617f8453adbb31'
assert subprocess.check_output(['git', 'rev-parse', 'HEAD']).decode().strip() == head
assert not subprocess.check_output(['git', 'diff', '--cached', '--name-only']).strip()
freeze = j(a / 'connected-spawn-original-collector-proposal2/source-freeze.json')
for row in freeze['Files']:
    raw = (r / row['Path']).read_bytes()
    assert len(raw) == row['Bytes'] and h(raw) == row['Sha256']
assert len(j(r / 'docs/supported-mfas.json')) == 576
assert j(r / 'docs/module-readiness.json')['RegisteredMfas'] == 576
cli = dict(Status='ACTUAL_SOURCE_VERIFY_CLI_OBSERVED_EXIT0',
           Command=['python', '-B', 'tools/Erlang.ConnectedSpawnOriginalObservations/collect-original.py', 'verify', '--repository', '.', '--manifest-sha256', freeze['SourceManifestSha256'], '--expected-head', head],
           ObservedToolResult=dict(Status='SOURCE_VERIFIED', Head=head, Cases=12, SdkExecuted=False, OriginalExecuted=False),
           Provenance='Structured record of the successful preceding exec tool result; not a raw process-output file.',
           RepeatedForReceipt=False)
(b / 'cli-observation.json').write_text(json.dumps(cli, indent=2) + '\n')
shutil.copyfile(a / 'root-stage339.py', b / 'root-stage339.py')
rows = [dict(Path=p.relative_to(r).as_posix(), Bytes=p.stat().st_size, Sha256=h(p.read_bytes())) for p in sorted(b.rglob('*')) if p.is_file() and p.name not in ['evidence-index.json', 'evidence-receipt.json']]
(b / 'evidence-index.json').write_text(json.dumps(rows, indent=2) + '\n')
receipt = j(b / 'evidence-receipt.json')
receipt.update(Status='SOURCE_ONLY339_COLLECTOR12_CLI_AND_SOURCE_CONTROLS_COMPLETE', SourceCliExit=0, ProofFiles=len(rows), ProofBytes=sum(x['Bytes'] for x in rows), EvidenceIndexSha256=h((b / 'evidence-index.json').read_bytes()))
(b / 'evidence-receipt.json').write_text(json.dumps(receipt, indent=2) + '\n')
plan = j(a / 'root-part339-stage-plan.json')
plan['Proof'] = [p.relative_to(r).as_posix() for p in sorted(b.rglob('*')) if p.is_file()]
plan['Paths'] = sorted(plan['Source'] + plan['Metadata'] + plan['Proof'])
paths = plan['Paths']
assert len(paths) == len(set(paths))
(a / 'root-part339-stage-plan.json').write_text(json.dumps(plan, indent=2) + '\n')
(a / 'root-part339-stage-paths.txt').write_text('\n'.join(paths) + '\n')
subprocess.run(['git', 'add', '--pathspec-from-file=' + str(a / 'root-part339-stage-paths.txt')], check=True, capture_output=True)
actual = subprocess.check_output(['git', 'diff', '--cached', '--name-only', '-z']).decode().rstrip('\0').split('\0')
assert sorted(actual) == paths
raw = subprocess.check_output(['git', 'cat-file', '--batch'], input=''.join(':' + p + '\n' for p in paths).encode())
offset = 0
staged = []
for path in paths:
    end = raw.index(b'\n', offset)
    fields = raw[offset:end].split()
    assert len(fields) == 3 and fields[1] == b'blob'
    size = int(fields[2])
    value = raw[end + 1:end + 1 + size]
    assert raw[end + 1 + size:end + 2 + size] == b'\n'
    offset = end + size + 2
    expected = (r / path).read_bytes()
    if path in plan['Metadata']:
        expected = expected.replace(b'\r\n', b'\n')
    assert value == expected, path
    staged.append(dict(Path=path, Bytes=size, Sha256=h(value)))
assert offset == len(raw)
subprocess.run(['git', 'diff', '--cached', '--check'], check=True, capture_output=True)
guard = dict(Status='EXACT339_CACHED_STAGE_PASS', Paths=len(paths), Source=6, RuntimeChanged=False,
             Registry=576, ProcLib=33, Scenarios=12, SourceCliObservedExit=0, PassiveControls=12, Rows=staged)
(a / 'root-part339-stage-guard.json').write_text(json.dumps(guard, indent=2) + '\n')
print(json.dumps({k: v for k, v in guard.items() if k != 'Rows'}))
