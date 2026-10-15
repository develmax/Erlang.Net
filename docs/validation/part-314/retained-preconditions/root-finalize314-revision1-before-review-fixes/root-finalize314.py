from pathlib import Path
import hashlib
import json
import sys

root = Path.cwd().resolve()
art = root / 'artifacts'
proof = art / 'root-part314-main1'
out = root / 'docs/validation/part-314'
sha = lambda raw: hashlib.sha256(raw).hexdigest().upper()
read = lambda path: json.loads(path.read_text(encoding='utf-8-sig'))
write = lambda path, value: path.write_text(json.dumps(value, indent=2) + '\n', encoding='utf-8')
note = (art / 'part314-runtime-note.txt').read_text(encoding='utf-8').strip()
assert read(proof / 'full-guard.json')['Passed'] == 6641
assert read(art / 'root-part314-main-windows-native1/guard.json')['FullPassed'] == 6641
assert sha((root / 'docs/changelog/0046.md').read_bytes()) == 'CAF1B30E0592EF5502A0642631256E3B4F4A5AD8201D96BBB234DA6F2B163361'
scope_path = root / 'docs/module-readiness-scope.json'
if sys.argv[1] == 'scope':
    assert not (proof / 'readiness-scope-before.json').exists()
    raw = scope_path.read_bytes()
    scope = json.loads(raw)
    assert scope['ComparisonTestReport'] == 'validation/part-312/tests.json'
    runtime = [row for row in scope['Components'] if row['Id'] == 'Runtime']
    assert len(runtime) == 1 and not any('Part314:' in value for value in runtime[0]['Implemented'])
    runtime[0]['Implemented'].append(note)
    scope['ComparisonTestReport'] = 'validation/part-313/final-main/tests.json'
    (proof / 'readiness-scope-before.json').write_bytes(raw)
    write(scope_path, scope)
    write(proof / 'readiness-scope-command.json', {'Phase': 'scope', 'ScopeBeforeSha256': sha(raw), 'ScopeAfterSha256': sha(scope_path.read_bytes()), 'RuntimeNoteSha256': sha((art / 'part314-runtime-note.txt').read_bytes()), 'GuardSourceSha256': sha(Path(__file__).read_bytes())})
    print('Scope updated; actual readiness CLI must run before finalization.')
    sys.exit(0)
assert sys.argv[1] == 'finalize'
assert not out.exists(), 'Fresh checkpoint required'
config_path = Path(sys.argv[2]).resolve()
config = read(config_path)
assert config['Part'] == 314 and config['ActualReadinessExitCode'] == 0
ready = read(root / 'docs/module-readiness.json')
assert (ready['LocalPassedTests'], ready['RegisteredMfas'], ready['PreviousRegisteredMfas'], ready['RegisteredMfaDelta'], ready['ComparisonTestReport']) == (6641, 505, 505, 0, 'validation/part-313/final-main/tests.json')
assert read(scope_path)['ComparisonTestReport'] == ready['ComparisonTestReport']
copies = []
targets = set()
for row in config['Files']:
    source = (root / row['SourcePath']).resolve()
    target = (root / row['Destination']).resolve()
    assert source.is_relative_to(root) and target.is_relative_to(out) and target not in targets
    raw = source.read_bytes()
    assert len(raw) == row['Bytes'] and sha(raw) == row['Sha256'], row['SourcePath']
    assert len(raw) < 100 * 1024 * 1024, 'GitHub file size boundary'
    targets.add(target)
    copies.append((target, raw))
required = {str(path.relative_to(root)).replace('\\', '/') for path in [proof / 'full-guard.json', proof / 'source-freeze.json', proof / 'source-resource-closure.json', art / 'tests.json', art / 'integration-results.json', art / 'root-part314-main-windows-native1/guard.json', root / 'docs/module-readiness.json']}
assert required <= {row['SourcePath'] for row in config['Files']}
entry_path = art / 'part314-english-entry.md'
assert sha(entry_path.read_bytes()) == config['EnglishEntrySha256']
entry = entry_path.read_text(encoding='utf-8').strip()
assert entry.startswith('## Part 314')
volume = root / 'docs/changelog/0047.md'
old = volume.read_bytes()
assert sha(old) == config['VolumeBeforeSha256'] and b'## Part 314' not in old
newline = b'\r\n' if b'\r\n' in old else b'\n'
combined = old.rstrip(b'\r\n') + newline * 2 + entry.replace('\r\n', '\n').replace('\n', newline.decode()).encode() + newline
assert len(combined) <= 32768 and len(combined.splitlines()) <= 400
metadata = ['AGENTS.md', 'docs/PROGRESS.md', 'docs/NEXT_STEPS.md', 'docs/COMPATIBILITY.md', 'docs/ERLANG_COMPATIBILITY.md', 'docs/DECISIONS.md', 'docs/OTP-PORTS.md']
changes = {volume: combined}
for relative in metadata:
    path = root / relative
    raw = path.read_bytes()
    assert sha(raw) == config['MetadataBefore'][relative] and b'Part314:' not in raw
    newline = b'\r\n' if b'\r\n' in raw else b'\n'
    marker = newline * 2
    position = raw.index(marker) + len(marker)
    changes[path] = raw[:position] + note.encode() + marker + raw[position:]
index_path = root / 'CHANGELOG.md'
index = index_path.read_bytes()
assert sha(index) == config['ChangelogBeforeSha256'] and index.count('parts312–313 (active)'.encode()) == 1
changes[index_path] = index.replace('parts312–313 (active)'.encode(), 'parts312–314 (active)'.encode())
for path, raw in copies:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(raw)
for path, raw in changes.items():
    path.write_bytes(raw)
write(out / 'evidence-index.json', config['Files'])
(out / 'final-bound-config.json').write_bytes(config_path.read_bytes())
receipt = {'Status': 'PASS_ACTUAL_MAIN314_FULL_NATIVE_READINESS_PREVIOUS313_REMOTE_WITH_FIRST_REALLOCATION_FAILURE', 'Passed': 6641, 'Failed': 0, 'Registry': 505, 'RegistryDelta': 0, 'ErlangPresence': 347, 'IntegrationResults': 11, 'SourcePaths': 17, 'CSharpPaths': 12, 'SourceClosurePaths': 2215, 'SourceFreezeSha256': sha((proof / 'source-freeze.json').read_bytes()), 'SourceClosureSha256': sha((proof / 'source-resource-closure.json').read_bytes()), 'EvidenceIndexSha256': sha((out / 'evidence-index.json').read_bytes()), 'ConfigSha256': sha(config_path.read_bytes()), 'ComparisonTestReport': ready['ComparisonTestReport'], 'Current314RemotePending': True, 'NewOriginalReallocationCompared': False, 'PublicAllocatorApisRegistered': False, 'Files': config['Files']}
write(out / 'evidence-receipt.json', receipt)
print(json.dumps({'CopiedProofs': len(copies), 'Passed': 6641, 'RegistryDelta': 0, 'VolumeBytes': len(combined)}))
