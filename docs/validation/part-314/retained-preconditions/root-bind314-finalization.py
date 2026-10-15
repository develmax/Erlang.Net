from pathlib import Path
import hashlib
import json

root = Path.cwd().resolve()
art = root / 'artifacts'
out = root / 'docs/validation/part-314'
bound = art / 'root-part314-finalization-bound'
assert not bound.exists()
sha = lambda raw: hashlib.sha256(raw).hexdigest().upper()
read = lambda path: json.loads(path.read_text(encoding='utf-8-sig'))
assert read(art / 'root-part314-main1/full-guard.json')['Passed'] == 6641
assert read(art / 'root-part314-main-windows-native1/guard.json')['FullPassed'] == 6641
assert read(art / 'root-part314-main1/readiness-command.json')['ExitCode'] == 0
rows = {}

def add(source, suffix, expected=None):
    source = source.resolve()
    assert source.is_relative_to(root) and source.is_file()
    raw = source.read_bytes()
    if expected:
        assert len(raw) == expected['Bytes'] and sha(raw) == expected['Sha256'], source
    target = out / suffix
    assert target.is_relative_to(out)
    key = target.relative_to(root).as_posix()
    assert key not in rows, key
    rows[key] = {'SourcePath': source.relative_to(root).as_posix(), 'Destination': key, 'Bytes': len(raw), 'Sha256': sha(raw), 'HashMode': 'BinaryExact'}

portable = art / 'root-part314-portable1'
portable_receipt = read(portable / 'archive-replay-receipt.json')
assert portable_receipt['Status'] == 'PASS_PASSIVE_ARCHIVE_AND_SINGLE_PASS_MEMBER_REHASH'
assert sha((portable / 'input-plan.json').read_bytes()) == '57C965EA4186A2427FB16B4AC359B4FEBAD6324D18C10B2D4D0DD2EEE3916481'
for name in ['input-plan.json', 'remote-separate-copy-index.json', 'remote-finaldocs-copy-manifest.json', 'remote-finaldocs-copy-manifest-guard.json', 'archive-reviewed.py', 'prepare-input-plan.py', 'archive-replay-receipt.json', portable_receipt['Archive']]:
    add(portable / name, 'portable/' + name)
remote_path = portable / 'remote-separate-copy-index.json'
assert sha(remote_path.read_bytes()) == 'AA86E9D1256B180FA338B29315CCA58F9F318E14112F7366C01C155A549496EA'
remote = read(remote_path)
assert remote['Head'] == 'a44f82a35ff95775941147ff9848a95a5247d76e'
for row in remote['RawApiZipRows'] + remote['RawJsonLogsAndReviewRows']:
    source = root / row['SourcePath']
    assert source.is_relative_to(art)
    add(source, 'previous-head313/' + source.relative_to(art).as_posix(), row)
ordinary_manifest = art / 'pack313-ordinary-part314-terminal-supplement-copyrows.json'
assert sha(ordinary_manifest.read_bytes()) == '39C2184D970D4AC9B5B5378B88A418A122D4BAC5C9E92ADA6D035FCA5AB7E33E'
for row in read(ordinary_manifest):
    target = root / row['Destination']
    assert target.is_relative_to(out)
    add(root / row['SourcePath'], target.relative_to(out).as_posix(), row)
for name in ['pack313-ordinary-part314-terminal-supplement-copyrows.json', 'pack313-ordinary-part314-terminal-supplement-receipt.json']:
    add(art / name, 'previous-head313/' + name)
ordinary = read(art / 'pack313-ordinary-semantic-independent-guard.json')
assert ordinary['Head'] == remote['Head'] and ordinary['Passed'] == 5861 and ordinary['Failed'] == 0 and ordinary['FdPassed'] == 8
for directory in ['root-part314-main1', 'root-part314-main-windows-native1']:
    for path in sorted((art / directory).iterdir()):
        if path.is_file():
            add(path, 'final-main/' + directory + '/' + path.name)
for path in sorted((art / 'root-part314-main-binaries1').iterdir()):
    if path.is_file():
        add(path, 'final-main/root-part314-main-binaries1/' + path.name)
for name in ['tests.json', 'integration-results.json']:
    add(art / name, 'final-main/' + name)
for name in ['module-readiness.json', 'module-readiness-scope.json', 'MODULE_READINESS.md', 'supported-mfas.json']:
    add(root / 'docs' / name, 'readiness/' + name)
scripts = ['root-promote314.py', 'root-run314-main-full.ps1', 'root-guard314-full.py', 'root-run314-main-native.py', 'root-guard314-main-native.py', 'root-finalize314.py', 'root-bind314-finalization.py', 'root-stage314.py', 'root-archive314-main-binaries.py', 'part314-english-entry.md', 'part314-runtime-note.txt', 'root314-postfull-scripts-beam310-source-peer.json', 'root314-finalizer-beam310-source-review1.json', 'root314-finalizer-beam310-source-review2.json', 'root314-binder-beam310-source-review1.json', 'allocator-query-mailbox314-import-policy2-beam310-source-peer.json']
for name in scripts:
    add(art / name, 'final-controls/' + name)
for directory in ['root314-finalizer-reviewer-revision1', 'root-finalize314-revision1-before-review-fixes', 'root-part313-stage-first-attempt1']:
    for path in sorted((art / directory).iterdir()):
        if path.is_file():
            add(path, 'retained-preconditions/' + directory + '/' + path.name)
assert (out / 'previous-head313/pack313-reallocation-independent-failure-guard.json').relative_to(root).as_posix() in rows
metadata = ['AGENTS.md', 'docs/PROGRESS.md', 'docs/NEXT_STEPS.md', 'docs/COMPATIBILITY.md', 'docs/ERLANG_COMPATIBILITY.md', 'docs/DECISIONS.md', 'docs/OTP-PORTS.md']
config = {'Part': 314, 'ActualReadinessExitCode': 0, 'RuntimeNoteSha256': sha((art / 'part314-runtime-note.txt').read_bytes()), 'EnglishEntrySha256': sha((art / 'part314-english-entry.md').read_bytes()), 'VolumeBeforeSha256': sha((root / 'docs/changelog/0047.md').read_bytes()), 'ChangelogBeforeSha256': sha((root / 'CHANGELOG.md').read_bytes()), 'MetadataBefore': {name: sha((root / name).read_bytes()) for name in metadata}, 'HistoricalPortableSnapshotDeferredRuns': remote['DeferredRuns'], 'Previous313OrdinaryNowVerified': True, 'Files': [rows[key] for key in sorted(rows)]}
bound.mkdir()
target = bound / 'config-finalize.json'
target.write_text(json.dumps(config, indent=2) + '\n', encoding='utf-8')
print(json.dumps({'Config': target.relative_to(root).as_posix(), 'Sha256': sha(target.read_bytes()), 'CopyRows': len(rows)}))
