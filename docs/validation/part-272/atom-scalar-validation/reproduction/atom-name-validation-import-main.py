from pathlib import Path, PurePosixPath
import json, hashlib, subprocess, sys
main = Path(__file__).resolve().parents[1]
expected_head = '2c1f6dadcf97b7301b60ea153dd6895a6a1a4687'
expected_freeze = '425FB07728D36B50B3562BE7B9F2FB3BD7119D15298D1B8BB3F0D0FA3FE7FCCB'
freeze_path = main / 'artifacts/atom-name-validation-source-freeze-final-revision5.json'
def sha(raw): return hashlib.sha256(raw).hexdigest().upper()
assert sha(freeze_path.read_bytes()) == expected_freeze
freeze = json.loads(freeze_path.read_text())
assert subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=main).decode().strip() == expected_head
assert freeze['Receipt']['Status'] == 'PASS_SCOPED_PROMOTION'
assert not freeze['Receipt']['NativeExecuted']
assert len(freeze['Files']) == len({row['Path'] for row in freeze['Files']}) == 20
writes = []
for row in freeze['Files']:
    relative = PurePosixPath(row['Path'])
    assert not relative.is_absolute() and '..' not in relative.parts
    path = main.joinpath(*relative.parts)
    assert path.resolve().is_relative_to(main.resolve())
    before = path.read_bytes() if path.exists() else None
    assert (sha(before) if before is not None else None) == row['BeforeSha256'], row['Path']
    original = subprocess.run(['git', 'show', expected_head + ':' + row['Path']], cwd=main, capture_output=True)
    if before is None:
        assert original.returncode != 0, row['Path']
    else:
        assert original.returncode == 0, row['Path']
        assert original.stdout.decode().replace('\r\n', '\n') == before.decode().replace('\r\n', '\n'), row['Path']
    raw = row['SourceContent'].encode()
    assert sha(raw) == row['Sha256'], row['Path']
    writes.append((path, raw))
if '--verify-only' in sys.argv:
    print(json.dumps({'Status': 'PASS', 'GuardedPaths': 20, 'Mutation': False}))
    sys.exit(0)
# Validate all paths and source bindings before the first write. Root owns execution.
for path, raw in writes:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(raw)
assert all(sha(path.read_bytes()) == sha(raw) for path, raw in writes)
receipt = {'Status': 'PASS', 'Head': expected_head, 'FreezeSha256': expected_freeze, 'Imported': 20, 'NativeExecuted': False, 'MainFullExecuted': False}
(main / 'artifacts/atom-name-validation-main-import-receipt.json').write_text(json.dumps(receipt, indent=2))
print(json.dumps(receipt, indent=2))
