from pathlib import Path
import hashlib
import json
import subprocess

root = Path.cwd()
package = root / 'artifacts/distribution-owner306-combined-formatted2'
sha = lambda data: hashlib.sha256(data).hexdigest().upper()
parent = '0494375635061b4952d40a3c7667234db321a931'
assert subprocess.check_output(['git', 'rev-parse', 'HEAD']).decode().strip() == parent
assert not subprocess.check_output(['git', 'diff', '--name-only', '-z'])
assert not subprocess.check_output(['git', 'diff', '--cached', '--name-only', '-z'])
freeze = package / 'validation-freeze.json'
assert sha(freeze.read_bytes()) == '66D792EFB54E2E68DECE2C531CE49A800B58DFC6E3138CB79D953FAF6ECFC697'
for row in json.loads(freeze.read_bytes())['Files']:
    data = (package / row['Path']).read_bytes()
    assert len(data) == row['Bytes'] and sha(data) == row['Sha256'], row['Path']
mapping_path = package / 'promotion-map.json'
assert sha(mapping_path.read_bytes()) == '2F042B7AC21FFF9D90BA5BD45966E72C837C79249CE6260A621065E44E908DBE'
mapping = json.loads(mapping_path.read_bytes())
assert len(mapping['Paths']) == 22
writes = []
for row in mapping['Paths']:
    target = root / row['Path']
    assert (sha(target.read_bytes()) if target.exists() else None) == row['ParentSha256'], row['Path']
    data = (package / 'overlay' / row['Path']).read_bytes()
    assert len(data) == row['ResultBytes'] and sha(data) == row['ResultSha256'], row['Path']
    writes.append((target, data))
receipt = {'Status': 'EXACT_22_FORMATTED_SOURCE_OVERLAY_APPLIED_FULL_PENDING', 'Parent': parent, 'CandidateFreezeSha256': sha(freeze.read_bytes()), 'Files': mapping['Paths'], 'RuntimeFullRun': False, 'NativeExecuted': False, 'PublicMfaDeltaExpected': 1}
out = root / 'artifacts/root-part306-runtime-promotion.json'
assert not out.exists()
for target, data in writes:
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_bytes(data)
out.write_text(json.dumps(receipt, indent=2) + '\n', encoding='utf-8')
print(receipt['Status'])
