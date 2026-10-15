from pathlib import Path
import hashlib,json
root=Path.cwd();directory=root/'artifacts/allocator-public-sys-profile316-design1'
for item in json.loads((directory/'source-bindings.json').read_text())['PinnedSources']:
    assert hashlib.sha256((root/item['Path']).read_bytes()).hexdigest().upper()==item['Sha256']
print('Pinned source copies hash verified; no Git or runtime execution')
