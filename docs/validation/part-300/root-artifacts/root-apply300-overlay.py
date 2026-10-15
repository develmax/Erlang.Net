import hashlib
import json
import shutil
import sys
from pathlib import Path

root = Path(__file__).resolve().parent.parent
artifact = root / sys.argv[1]
candidate = root / 'artifacts/root-nif-lifetime300-isolated/candidate'
group = sys.argv[3] if len(sys.argv) > 3 else None
freeze = artifact / ('source-bindings.json' if group else 'source-freeze.json')
if not freeze.exists():
    freeze = artifact / 'source-bindings.json'
document = json.loads(freeze.read_text(encoding='utf-8-sig'))
rows = document.get('Files', [document])
if group:
    rows = [row for row in rows if row['Group'] == group]
    assert rows
source_directory = group or 'overlay'
for row in rows:
    relative = Path(row['Path'])
    assert not relative.is_absolute() and '..' not in relative.parts
    source = artifact / source_directory / relative
    target = candidate / relative
    expected = row['ParentSha256']
    actual = hashlib.sha256(target.read_bytes()).hexdigest().upper() if target.exists() else None
    assert actual == expected, (str(relative), 'parent', actual, expected)
    assert hashlib.sha256(source.read_bytes()).hexdigest().upper() == row['Sha256']
for row in rows:
    relative = Path(row['Path'])
    target = candidate / relative
    target.parent.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(artifact / source_directory / relative, target)
receipt = {
    'MainChanged': False,
    'Overlay': str(artifact.relative_to(root)).replace('\\', '/'),
    'FreezeSha256': hashlib.sha256(freeze.read_bytes()).hexdigest().upper(),
    'FilesApplied': rows,
}
output = root / sys.argv[2]
assert not output.exists(), str(output)
output.write_text(json.dumps(receipt, indent=2) + '\n', encoding='utf-8')
print(json.dumps({'Status': 'EXACT_PARENT_OVERLAY_APPLIED', 'Files': len(rows)}))
