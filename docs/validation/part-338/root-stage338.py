from pathlib import Path
import hashlib
import json
import subprocess

r = Path.cwd()
a = r / 'artifacts'
j = lambda p: json.loads(p.read_bytes())
plan = j(a / 'root-part338-stage-plan.json')
paths = plan['Paths']
assert not subprocess.check_output(['git', 'diff', '--cached', '--name-only']).strip()
for row in j(r / 'docs/validation/part-338/final-candidate/source-freeze.json')['Files']:
    raw = (r / row['Path']).read_bytes()
    assert len(raw) == row['Bytes'] and hashlib.sha256(raw).hexdigest().upper() == row['Sha256']
for row in j(r / 'docs/validation/part-338/evidence-index.json'):
    assert hashlib.sha256((r / row['Path']).read_bytes()).hexdigest().upper() == row['Sha256']
subprocess.run(['git', 'add', '--pathspec-from-file=' + str(a / 'root-part338-stage-paths.txt')], check=True, capture_output=True)
actual = subprocess.check_output(['git', 'diff', '--cached', '--name-only', '-z']).decode().rstrip('\0').split('\0')
assert sorted(actual) == paths
raw = subprocess.check_output(['git', 'cat-file', '--batch'], input=''.join(':' + p + '\n' for p in paths).encode())
offset = 0
rows = []
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
    rows.append(dict(Path=path, Bytes=size, Sha256=hashlib.sha256(value).hexdigest().upper()))
assert offset == len(raw)
subprocess.run(['git', 'diff', '--cached', '--check'], check=True, capture_output=True)
promoted = j(a / 'root-part338-promotion.json')
guard = dict(Status='EXACT338_CACHED_STAGE_PASS', Paths=len(paths), Source=len(plan['Source']),
             Passed=promoted['Passed'], Registry=576, ProcLib=33, Rows=rows)
(a / 'root-part338-stage-guard.json').write_text(json.dumps(guard, indent=2) + '\n')
print(json.dumps({k: v for k, v in guard.items() if k != 'Rows'}))
