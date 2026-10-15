from pathlib import Path
import hashlib
import json
import subprocess

root = Path.cwd()
package = root / 'artifacts/raw-onload-exact-image305-candidate1'
sha = lambda data: hashlib.sha256(data).hexdigest().upper()
assert subprocess.check_output(['git', 'rev-parse', 'HEAD']).decode().strip() == '51994bed995f9dbf31b8b88b3381cad2642012e0'
assert not subprocess.check_output(['git', 'diff', '--name-only', '-z'])
assert not subprocess.check_output(['git', 'diff', '--cached', '--name-only', '-z'])
freeze = package / 'source-freeze.json'
assert sha(freeze.read_bytes()) == '1E0BFF84372B17A5EFEC0259A875E193C48EF5FCCB01E2309456BF9E16807B62'
for row in json.loads(freeze.read_bytes())['Files']:
    data = (package / row['Path']).read_bytes()
    assert len(data) == row['Bytes'] and sha(data) == row['Sha256'], row['Path']
mapping = json.loads((package / 'promotion-map.json').read_bytes())
assert len(mapping['Paths']) == 8
writes = []
for row in mapping['Paths']:
    target = root / row['Path']
    assert (sha(target.read_bytes()) if target.exists() else None) == row['ParentSha256'], row['Path']
    data = (package / 'overlay' / row['Path']).read_bytes()
    assert len(data) == row['ResultBytes'] and sha(data) == row['ResultSha256'], row['Path']
    writes.append((target, data))
for target, data in writes:
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_bytes(data)
receipt = {'Status': 'EXACT_EIGHT_PARENT_CHECKED_IMAGE_OVERLAY_APPLIED', 'Parent': '51994bed995f9dbf31b8b88b3381cad2642012e0', 'Paths': mapping['Paths'], 'BuildRun': False, 'NativePlanResealed': False, 'Committed': False}
out = root / 'artifacts/root-part305-image-promotion.json'
assert not out.exists()
out.write_text(json.dumps(receipt, indent=2) + '\n', encoding='utf-8')
print(receipt['Status'])
