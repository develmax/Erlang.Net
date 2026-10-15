from pathlib import Path
import hashlib
import json
import zipfile

root = Path.cwd()
out = root / 'artifacts/root-part314-main-binaries2'
assert not out.exists()
sha = lambda raw: hashlib.sha256(raw).hexdigest().upper()
read = lambda path: json.loads(path.read_text(encoding='utf-8-sig'))
guard = root / 'artifacts/root-part314-main-windows-native1/guard.json'
assert read(guard)['FullPassed'] == 6641
rows = []
contents = {}
for project in ['tools/Erlang.NifNativeBackend', 'tests/Erlang.Tests']:
    directory = root / project / 'bin/Debug/net10.0'
    for path in sorted(directory.rglob('*')):
        if path.is_file():
            raw = path.read_bytes()
            name = path.relative_to(root).as_posix()
            rows.append({'Path': name, 'Bytes': len(raw), 'Sha256': sha(raw)})
            contents[name] = raw
assert len(rows) == len(contents) and rows
for role in ['legacy', 'reallocation']:
    attempt = read(root / 'artifacts/root-part314-main-windows-native1' / (role + '.attempt.json'))
    for row in attempt['Bindings']:
        if row['Path'] in contents:
            assert sha(contents[row['Path']]) == row['Sha256']
out.mkdir()
target = out / 'actual-main314-binaries.zip'
with zipfile.ZipFile(target, 'x', zipfile.ZIP_DEFLATED) as archive:
    archive.writestr('binary-index.json', json.dumps(rows, indent=2) + '\n')
    for name, raw in contents.items():
        archive.writestr(name, raw)
assert target.stat().st_size < 100 * 1024 * 1024
with zipfile.ZipFile(target) as archive:
    assert set(archive.namelist()) == set(contents) | {'binary-index.json'}
    assert json.loads(archive.read('binary-index.json')) == rows
    for row in rows:
        raw = archive.read(row['Path'])
        assert len(raw) == row['Bytes'] and sha(raw) == row['Sha256']
receipt = {'Status': 'PASS_PASSIVE_FINAL_MAIN_BINARY_ARCHIVE_REPLAY', 'NativeGuardSha256': sha(guard.read_bytes()), 'ArchiveSha256': sha(target.read_bytes()), 'ArchiveBytes': target.stat().st_size, 'Files': rows, 'ScriptSha256': sha(Path(__file__).read_bytes()), 'SdkOrNativeExecutedByArchiver': False}
(out / 'receipt.json').write_text(json.dumps(receipt, indent=2) + '\n', encoding='utf-8')
print(json.dumps({key: value for key, value in receipt.items() if key != 'Files'}))
