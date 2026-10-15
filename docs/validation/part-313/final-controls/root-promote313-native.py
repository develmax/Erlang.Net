"""Root-reviewed final native integration promotion; requires exact peer digest."""
from pathlib import Path
import hashlib
import json
import subprocess
import sys

root = Path.cwd()
art = root / 'artifacts'
candidate = art / 'nif-reallocation-native309-remote-candidate1'
out = art / 'root-part313-main1'
sha = lambda raw: hashlib.sha256(raw).hexdigest().upper()
read = lambda path: json.loads(path.read_bytes())
assert len(sys.argv) == 3, 'Root-reviewed peer path and digest required'
peer = (root / sys.argv[1]).resolve()
assert peer.is_relative_to(art.resolve()) and sha(peer.read_bytes()) == sys.argv[2]
freeze_path = candidate / 'source-freeze.json'
assert sha(freeze_path.read_bytes()) == '11DBF72C617B9BAAD1BC6A42BDA9408D81E5A8BF80B0AB971F062861379D7200'
f = read(freeze_path)
head = subprocess.check_output(['git', '--no-optional-locks', 'rev-parse', 'HEAD'], text=True).strip()
assert head == f['ParentPublished312Head'] == '710996472fdb4a68b6b59103ce351247cf78e7a3'
assert not subprocess.check_output(['git', '--no-optional-locks', 'diff', '--name-only'])
assert not subprocess.check_output(['git', '--no-optional-locks', 'diff', '--cached', '--name-only'])
assert not out.exists()
assert f['SourcePaths'] == len(f['Files']) == 45 and f['CSharpPaths'] == 19
assert f['SourcePlanFiles'] == 1074 and f['SourcePlanSha256'] == 'EE514F42467BF1A3FD4764D07F696CA4F2BFDA2126157F3A0C06E07DD065209A'
assert f['PureControlsPassed'] == 54 and f['Root19CSharpRawExact'] and f['Native6RawExact']
for row in f['EvidenceFiles']:
    raw = (candidate / row['Path']).read_bytes()
    assert len(raw) == row['Bytes'] and sha(raw) == row['Sha256'], row['Path']

baseline = read(art / 'root-part312-revision2/source-resource-closure.json')
assert len(baseline['Files']) == 2171
payloads = {}
for row in baseline['Files']:
    raw = (root / row['Path']).read_bytes()
    assert len(raw) == row['Bytes'] and sha(raw) == row['Sha256'], row['Path']
    payloads[row['Path']] = raw
formatted = read(art / 'root-part313-native-isolated1/formatted-source-freeze.json')
cs = {row['Path']: row for row in formatted['Files'] if row['Path'].endswith('.cs')}
assert len(cs) == 19
for row in f['Files']:
    path = Path(row['Path'])
    assert not path.is_absolute() and '..' not in path.parts
    raw = (candidate / 'overlay' / path).read_bytes()
    assert len(raw) == row['Bytes'] and sha(raw) == row['Sha256'], path
    if path.suffix == '.cs':
        assert row['Bytes'] == cs[row['Path']]['Bytes'] and row['Sha256'] == cs[row['Path']]['Sha256']
    payloads[row['Path']] = raw
assert len(payloads) == f['ComposedAuthoredFiles'] == 2205
authored_closure = read(candidate / 'evidence/source-resource-closure.json')['Files']
assert len(authored_closure) == 2205
for row in authored_closure:
    raw = payloads[row['Path']]
    assert len(raw) == row['Bytes'] and sha(raw) == row['Sha256'], row['Path']
assert set(payloads) == {row['Path'] for row in authored_closure}

# All parent, source, evidence and composed bytes are checked before Main writes.
out.mkdir()
for row in f['Files']:
    path = root / row['Path']
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(payloads[row['Path']])
for row in authored_closure:
    raw = (root / row['Path']).read_bytes()
    assert len(raw) == row['Bytes'] and sha(raw) == row['Sha256'], row['Path']
closure = {'Status': 'FROZEN_ACTUAL_MAIN_SOURCE_BEFORE_FULL_EXECUTION', 'ParentHead': head,
           'SourceFiles': 2205, 'Files': authored_closure}
closure_raw = (json.dumps(closure, indent=2) + '\n').encode()
(out / 'source-resource-closure.json').write_bytes(closure_raw)
result = {'Status': 'ROOT_REVIEWED_45_SOURCE_19_CSHARP_FROZEN_PENDING_ACTUAL_MAIN_FULL',
          'ParentHead': head, 'SourcePaths': 45, 'CSharpPaths': 19,
          'SourceClosurePaths': 2205, 'FinalSourceClosureSha256': sha(closure_raw),
          'AuthorFreezeSha256': sha(freeze_path.read_bytes()),
          'IndependentPeerPath': peer.relative_to(root).as_posix(),
          'IndependentPeerSha256': sha(peer.read_bytes()),
          'RootFormattedIsolatedSourceFreezeSha256': '2F96402E39B6234D33EC66371C7A4961222BAFBEF026AD89F23C1236AB97981E',
          'All19CSharpBytesExactFormattedTokenProvenSource': True,
          'TokensExactForCSharp': True, 'ExpectedFullPassed': 6640,
          'ExpectedRegistry': 505, 'ExpectedOptionalIoRegistry': 507,
          'ExpectedErlangPresence': 347, 'ReviewedSourceInputs': 1049,
          'ReviewedSourcePlanSha256': f['Legacy1049UnchangedSha256'],
          'NewReallocationSourceInputs': 1074, 'NewReallocationPlanSha256': f['SourcePlanSha256'],
          'PublicMfaDelta': 0, 'DefaultNativeLoadInstructionHandlerInstalled': False,
          'GeneralNifAbiComplete': False, 'Files': f['Files']}
(out / 'source-freeze.json').write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
print(json.dumps({'SourcePaths': 45, 'CSharpPaths': 19, 'SourceClosurePaths': 2205,
                  'FreezeSha256': sha((out / 'source-freeze.json').read_bytes()),
                  'ClosureSha256': sha(closure_raw), 'MainSourcePromoted': True, 'BuildExecuted': False}))
