from pathlib import Path
import hashlib
import json
import zipfile

root = Path.cwd()
art = root / 'artifacts'
out = art / 'root-part313-isolated-portable1'
assert not out.exists(), 'Immutable archive already exists'
sha = lambda raw: hashlib.sha256(raw).hexdigest().upper()
read = lambda p: json.loads(p.read_bytes())
logical = {}
blobs = {}

def add(role, path, expected=None):
    path = path.resolve()
    assert path.is_relative_to(root.resolve()), path
    raw = path.read_bytes()
    digest = sha(raw)
    if expected is not None:
        assert len(raw) == expected['Bytes'] and digest == expected['Sha256'], path
    key = role + '/' + path.relative_to(root).as_posix()
    assert key not in logical, key
    logical[key] = {'Role': role, 'Path': path.relative_to(root).as_posix(),
                    'Bytes': len(raw), 'Sha256': digest, 'Blob': 'blobs/' + digest}
    if digest in blobs:
        assert blobs[digest] == raw
    blobs[digest] = raw

isolated = art / 'root-part313-native-isolated1'
freeze = isolated / 'formatted-source-freeze.json'
closure = isolated / 'formatted-source-resource-closure.json'
assert sha(freeze.read_bytes()) == '2F96402E39B6234D33EC66371C7A4961222BAFBEF026AD89F23C1236AB97981E'
assert sha(closure.read_bytes()) == '61B0FE3D6A9A418867692EDC1DE78630B87DEB3D196424E461FE27D9CB11C22E'
for row in read(closure)['Files']:
    add('actual-isolated-2200-source', isolated / 'candidate' / row['Path'], row)
assert len(logical) == 2200
for p in sorted(isolated.iterdir()):
    if p.is_file():
        add('actual-isolated-receipts', p)

directories = {
    'author-rejected-or-superseded-source': [
        'nif-reallocation-native309-proposal1', 'nif-reallocation-native309-proposal2',
        'nif-reallocation-native309-complete-candidate2',
        'nif-reallocation-native309-complete-candidate3',
        'nif-reallocation-native309-complete-candidate4'],
    'actual-windows-build-layout-sdk-legacy': [
        'nif-reallocation-native309-windows-recipe1',
        'nif-reallocation-native309-windows-static-peer1',
        'nif-reallocation-native309-windows-sdk-peer1',
        'root-part313-native-windows-build1', 'root-part313-native-windows-layout1',
        'root-part313-native-windows-sdk1', 'root-part313-native-legacy-windows1'],
    'source-only-runtime-adapter': ['pinned-runtime-adapter309-candidate1',
                                   'pinned-runtime-adapter309-candidate2',
                                   'pinned-runtime-adapter309-candidate3'],
}
for role, names in directories.items():
    for name in names:
        directory = art / name
        assert directory.is_dir(), directory
        for p in sorted(directory.rglob('*')):
            if p.is_file() and not {'bin', 'obj', '__pycache__'}.intersection(p.relative_to(directory).parts):
                add(role, p)

for project in ['tools/Erlang.NifNativeBackend', 'tests/Erlang.Tests']:
    directory = isolated / 'candidate' / project / 'bin/Debug/net10.0'
    assert directory.is_dir()
    for p in sorted(directory.iterdir()):
        if p.is_file() and p.suffix in {'.dll', '.pdb', '.json'}:
            add('actual-isolated-compiled-output', p)
old = art / 'spawn-protocol-nif-290-38688662194-terminal/artifact-11951535100/nif-fixture-build/fixture.dll'
assert sha(old.read_bytes()) == '235D9E8997B7760E120ECD5CFACA29F01D0936A8FD89EF8B23DDF7D0DB43883F'
add('actual-legacy-trusted-fixture', old)
for p in sorted(art.iterdir()):
    if p.is_file() and (p.name.startswith(('root-build313-', 'root-compose313-', 'root-focus313-',
                                         'root-format313-', 'root-run313-', 'root-seal313-',
                                         'root-review-reallocation-windows312'))):
        add('root-executed-or-passive-controls', p)

index = {'Status': 'IMMUTABLE_ISOLATED_SOURCE_AND_ACTUAL_WINDOWS_EVIDENCE_ARCHIVE',
         'ParentHead': '710996472fdb4a68b6b59103ce351247cf78e7a3',
         'ActualIsolatedSourceFiles': 2200, 'ActualIsolatedOwnedPaths': 40,
         'ActualIsolatedCSharpPaths': 19, 'OriginalExecutedForNewThreeCases': False,
         'LinuxNewExtensionExecuted': False, 'MainExecutionClaimed': False,
         'RolesRemainSeparate': True, 'NativeExecutedByArchiver': False,
         'Files': [logical[k] for k in sorted(logical)]}
index_raw = (json.dumps(index, indent=2) + '\n').encode()
out.mkdir()
archive = out / 'immutable-evidence.zip'
with zipfile.ZipFile(archive, 'x', compression=zipfile.ZIP_DEFLATED, compresslevel=9) as z:
    z.writestr('index.json', index_raw)
    for digest, raw in sorted(blobs.items()):
        z.writestr('blobs/' + digest, raw)
with zipfile.ZipFile(archive) as z:
    assert z.read('index.json') == index_raw
    for row in index['Files']:
        raw = z.read(row['Blob'])
        assert len(raw) == row['Bytes'] and sha(raw) == row['Sha256'], row['Path']
receipt = {k: v for k, v in index.items() if k != 'Files'}
receipt.update({'LogicalFiles': len(logical), 'UniqueBlobs': len(blobs),
                'ArchiveBytes': archive.stat().st_size, 'ArchiveSha256': sha(archive.read_bytes()),
                'IndexSha256': sha(index_raw), 'ArchiveOnlyAllRowsReplayed': True})
(out / 'archive-receipt.json').write_text(json.dumps(receipt, indent=2) + '\n', encoding='utf-8')
print(json.dumps(receipt))
