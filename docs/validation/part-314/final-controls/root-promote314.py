import hashlib
import json
import subprocess
import sys
from pathlib import Path

root = Path.cwd().resolve()
candidate, freeze_path, closure_path, peer_path = [Path(value).resolve() for value in sys.argv[1:]]
out = root / 'artifacts/root-part314-main1'
assert not out.exists(), 'Fresh Main authority required'

def digest(raw):
    return hashlib.sha256(raw).hexdigest().upper()

def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))

freeze = read(freeze_path)
closure = read(closure_path)
peer = read(peer_path)
head = subprocess.check_output(['git', 'rev-parse', 'HEAD'], text=True).strip()
assert head == freeze['ParentHead'] == 'a44f82a35ff95775941147ff9848a95a5247d76e'
assert len(freeze['Files']) == freeze['SourcePaths'] == 17
assert sum(row['Path'].endswith('.cs') for row in freeze['Files']) == 12
assert len(closure['Files']) == 2215
assert digest(closure_path.read_bytes()) == freeze['SourceClosureSha256']
assert peer['Status'].startswith('PASS'), peer
assert digest(freeze_path.read_bytes()) in peer_path.read_text(encoding='utf-8-sig'), 'Peer must bind this final freeze'
assert not subprocess.check_output(['git', 'diff', '--name-only'], text=True).strip()
assert not subprocess.check_output(['git', 'diff', '--cached', '--name-only'], text=True).strip()
owned = {row['Path']: row for row in freeze['Files']}
assert len(owned) == 17 and owned.keys() <= {row['Path'] for row in closure['Files']}
for row in closure['Files']:
    relative = row['Path']
    source = candidate / relative
    assert source.is_relative_to(candidate)
    raw = source.read_bytes()
    assert len(raw) == row['Bytes'] and digest(raw) == row['Sha256'], relative
    main = root / relative
    if relative in owned:
        binding = owned[relative]
        assert (binding['Bytes'], binding['Sha256']) == (row['Bytes'], row['Sha256'])
        assert main.exists() != binding['Added'], relative
        if main.exists():
            assert digest(main.read_bytes()) == binding['ParentSha256'], relative
    else:
        assert main.read_bytes() == raw, relative
out.mkdir()
for row in freeze['Files']:
    destination = root / row['Path']
    destination.parent.mkdir(parents=True, exist_ok=True)
    destination.write_bytes((candidate / row['Path']).read_bytes())
for row in closure['Files']:
    raw = (root / row['Path']).read_bytes()
    assert len(raw) == row['Bytes'] and digest(raw) == row['Sha256'], row['Path']
(out / 'source-resource-closure.json').write_bytes(closure_path.read_bytes())
authority = dict(freeze)
authority.update({
    'Status': 'ROOT_PROMOTED_EXACT_17_PATHS_PENDING_ACTUAL_MAIN_FULL',
    'ExpectedFullPassed': 6641,
    'ExpectedRegistry': 505,
    'ExpectedErlangPresence': 347,
    'PublicMfaDelta': 0,
    'FinalSourceClosureSha256': digest(closure_path.read_bytes()),
    'AuthorFinalFreeze': {'Path': str(freeze_path.relative_to(root)), 'Sha256': digest(freeze_path.read_bytes())},
    'IndependentPeer': {'Path': str(peer_path.relative_to(root)), 'Sha256': digest(peer_path.read_bytes())},
    'RootPromotionScriptSha256': digest(Path(__file__).read_bytes()),
    'OriginalOrNativeExecutedByPromotion': False
})
(out / 'source-freeze.json').write_text(json.dumps(authority, indent=2) + '\n', encoding='utf-8')
print(json.dumps({'Status': authority['Status'], 'MainSourcePaths': 17, 'CompleteClosure': 2215, 'SourceFreezeSha256': digest((out / 'source-freeze.json').read_bytes())}))
