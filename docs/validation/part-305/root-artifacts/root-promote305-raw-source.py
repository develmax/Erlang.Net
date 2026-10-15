import argparse
import hashlib
import json
from pathlib import Path
import subprocess

parser = argparse.ArgumentParser()
parser.add_argument('--approved-freeze-sha256', required=True)
args = parser.parse_args()
root = Path.cwd()
package = root / 'artifacts/raw-onload-deferred-exports306-candidate1'
sha = lambda data: hashlib.sha256(data).hexdigest().upper()
assert subprocess.check_output(['git', 'rev-parse', 'HEAD']).decode().strip() == '51994bed995f9dbf31b8b88b3381cad2642012e0'
assert not subprocess.check_output(['git', 'diff', '--cached', '--name-only', '-z'])
freeze = package / 'source-freeze.json'
assert sha(freeze.read_bytes()) == args.approved_freeze_sha256.upper()
for row in json.loads(freeze.read_bytes())['Files']:
    data = (package / row['Path']).read_bytes()
    assert len(data) == row['Bytes'] and sha(data) == row['Sha256'], row['Path']
mapping = json.loads((package / 'promotion-map.json').read_bytes())
assert len(mapping['Paths']) == 21
image = json.loads((root / 'artifacts/raw-onload-exact-image305-candidate1/promotion-map.json').read_bytes())
for row in image['Paths']:
    assert sha((root / row['Path']).read_bytes()) == row['ResultSha256'], row['Path']
program = 'tests/Erlang.Tests/Program.cs'
base = subprocess.check_output(['git', 'show', 'HEAD:' + program])
raw_row = b'Test(Erlang.Tests.RawOnLoadExportsHostNames.Test, Erlang.Tests.RawOnLoadExportsHostChecks.Run);\r\n'
image_row = b'Test(Erlang.Tests.ExactImageCodeHostNames.Test, Erlang.Tests.ExactImageCodeHostChecks.Run);\r\n'
anchor = b'Test(Erlang.Tests.CodePurgeHostNames.Test, Erlang.Tests.CodePurgeHostChecks.Run);\r\n'
writes = []
for row in mapping['Paths']:
    target = root / row['Path']
    data = (package / 'overlay' / row['Path']).read_bytes()
    assert len(data) == row['ResultBytes'] and sha(data) == row['ResultSha256'], row['Path']
    if row['Path'] == program:
        # Preserve the two independently reviewed additive registrations exactly.
        current = target.read_bytes()
        line = raw_row if raw_row in data else raw_row.replace(b'\r\n', b'\n')
        image_line = image_row if image_row in current else image_row.replace(b'\r\n', b'\n')
        assert data.count(line) == current.count(image_line) == 1
        assert data.replace(line, b'', 1) == current.replace(image_line, b'', 1)
        assert data.replace(line, b'', 1).replace(b'\r\n', b'\n') == base.replace(b'\r\n', b'\n')
        insertion = anchor if anchor in data else anchor.replace(b'\r\n', b'\n')
        assert data.count(insertion) == 1 and image_line not in data
        data = data.replace(insertion, insertion + image_line, 1)
    else:
        assert (sha(target.read_bytes()) if target.exists() else None) == row['ParentSha256'], row['Path']
    writes.append((target, data))
receipt = {'Status': 'EXACT_RAW21_OVERLAY_PLUS_TWO_ADDITIVE_PROGRAM_ROWS_APPLIED', 'Parent': '51994bed995f9dbf31b8b88b3381cad2642012e0', 'CandidateFreezeSha256': args.approved_freeze_sha256.upper(), 'Files': [{'Path': target.relative_to(root).as_posix(), 'Bytes': len(data), 'Sha256': sha(data)} for target, data in writes], 'MainFullRun': False, 'NativeExecuted': False, 'Committed': False}
out = root / 'artifacts/root-part305-raw-promotion.json'
assert not out.exists()
for target, data in writes:
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_bytes(data)
out.write_text(json.dumps(receipt, indent=2) + '\n', encoding='utf-8')
print(receipt['Status'])
