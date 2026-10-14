from pathlib import Path, PurePosixPath
import hashlib, json, subprocess
r = Path.cwd()
sha = lambda b: hashlib.sha256(b).hexdigest().upper()
head = '2c1f6dadcf97b7301b60ea153dd6895a6a1a4687'
assert subprocess.check_output(['git', 'rev-parse', 'HEAD']).decode().strip() == head
specs = [('atom-name-validation-source-freeze-final-revision5.json', '425FB07728D36B50B3562BE7B9F2FB3BD7119D15298D1B8BB3F0D0FA3FE7FCCB', 20), ('packet-pending-correction-source-freeze-final.json', 'DBD42F4EBE4E39A7C3D25BCEFE1608737E3251E2CE95CF1258F332FDC3046939', 5)]
writes = []
for name, digest, count in specs:
    p = r / 'artifacts' / name
    assert sha(p.read_bytes()) == digest
    rows = json.loads(p.read_bytes())['Files']
    assert len(rows) == count
    for row in rows:
        relative = PurePosixPath(row['Path'])
        assert not relative.is_absolute() and '..' not in relative.parts
        target = r.joinpath(*relative.parts)
        assert target.resolve().is_relative_to(r.resolve())
        before = target.read_bytes() if target.exists() else None
        assert (sha(before) if before is not None else None) == row['BeforeSha256'], row['Path']
        raw = row['SourceContent'].encode()
        assert sha(raw) == row['Sha256']
        writes.append((target, raw, row))
assert len(writes) == len({v[2]['Path'] for v in writes}) == 25
for target, raw, row in writes:
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_bytes(raw)
assert all(sha(p.read_bytes()) == v['Sha256'] for p, raw, v in writes)
receipt = {'Status': 'PASS', 'Head': head, 'Imported': 25, 'NativeExecuted': False, 'MainFullExecuted': False, 'Files': [{'Path': v['Path'], 'BeforeSha256': v['BeforeSha256'], 'AfterSha256': v['Sha256']} for p, raw, v in writes]}
(r / 'artifacts/part272-main-import-guard.json').write_bytes((json.dumps(receipt, indent=2) + '\n').encode())
print('PASS: all 25 before/after bindings checked before mutation; imported 25 disjoint paths')
