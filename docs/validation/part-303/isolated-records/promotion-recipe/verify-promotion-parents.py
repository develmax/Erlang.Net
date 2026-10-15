import argparse
import hashlib
import json
from pathlib import Path
import re
import subprocess

MAP_SHA = 'C9021AB548A56D0EFCD331E7003EB5383EC616C83120ACEA93D97FFBEDC02A55'


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()


def require(value, message):
    if not value:
        raise RuntimeError(message)


parser = argparse.ArgumentParser(description='Read-only exact-parent promotion preflight; never copies sources or edits Main/Git.')
parser.add_argument('--repository', type=Path, required=True)
parser.add_argument('--expected-head', required=True)
parser.add_argument('--output', type=Path, required=True)
args = parser.parse_args()
repository = args.repository.resolve()
recipe = Path(__file__).resolve().parent
require(re.fullmatch('[0-9a-fA-F]{40}', args.expected_head), 'Caller must explicitly supply the reviewed future checkout HEAD')
head = subprocess.check_output(['git', '-C', str(repository), 'rev-parse', 'HEAD'], timeout=30).decode().strip()
require(head == args.expected_head, 'Actual checkout does not match caller expected HEAD')
require(sha(recipe / 'promotion-map.json') == MAP_SHA, 'Frozen promotion map drift')
manifest = json.loads((recipe / 'promotion-map.json').read_bytes())
records = list(manifest['SourceFiles'])
for kind in ('NativePlan', 'NativeWorkflow'):
    row = manifest[kind]
    records.append({'Path': row['Path'], 'ParentSha256': row['ParentSha256'], 'Sha256': row['Sha256']})
require(len(records) == 13 and len({row['Path'] for row in records}) == 13, 'Expected exact eleven source plus plan/workflow map')
checked = []
for row in records:
    relative = Path(row['Path'])
    require(not relative.is_absolute() and '..' not in relative.parts, 'Unsafe mapped path')
    actual = (repository / relative).resolve()
    require(actual.is_relative_to(repository), 'Mapped parent escapes repository')
    parent = row['ParentSha256']
    require(not actual.exists() if parent is None else actual.is_file() and sha(actual) == parent, 'Exact parent drift: ' + row['Path'])
    proposed = recipe / 'source-overlay' / relative
    require(proposed.is_file() and sha(proposed) == row['Sha256'], 'Proposed source drift: ' + row['Path'])
    checked.append(row['Path'])
output = args.output.resolve()
require(output.is_relative_to(repository / 'artifacts') and not output.exists(), 'Fresh artifact-only preflight report required')
result = {'Status': 'EXACT_PARENTS_READY_NO_MAIN_PROMOTION_PERFORMED', 'ExplicitExpectedHead': args.expected_head, 'ActualHead': head, 'HistoricalCandidateBaseline': manifest['HistoricalIsolatedPublishedBaseline'], 'Actual301SourceFreezeSha256': manifest['Actual301SourceFreezeSha256'], 'CheckedPaths': checked, 'CheckedPathCount': 13, 'PublicMfaDelta': 0, 'NativeExecuted': False, 'MainEdited': False, 'GitMutated': False}
output.write_bytes((json.dumps(result, indent=2) + '\n').encode())
print(json.dumps({'Status': result['Status'], 'Head': head, 'Paths': len(checked)}))
