"""Read-only allocator proposal validation. Executes no VM, SDK, Git or network call."""
import argparse
import hashlib
import json
from pathlib import Path
import sys

sys.dont_write_bytecode = True
parser = argparse.ArgumentParser()
parser.add_argument('--repository', required=True, type=Path)
args = parser.parse_args()
here = Path(__file__).resolve().parent
root = args.repository.resolve()
freeze = json.loads((here / 'source-freeze.json').read_bytes())

def record(path, name):
    raw = path.read_bytes()
    return {'Path': name, 'Bytes': len(raw), 'Sha256': hashlib.sha256(raw).hexdigest().upper()}

def require(value, message):
    if not value:
        raise RuntimeError(message)

for row in freeze['PromotedFiles']:
    require(record(root / row['Path'], row['Path']) == row, 'Promoted source differs: ' + row['Path'])
for row in freeze['UnchangedPublishedHelpers']:
    require(record(root / row['Path'], row['Path']) == row, 'Published helper differs: ' + row['Path'])
tool = root / 'tools/Erlang.AllocatorMemoryObservations'
actual = {p.relative_to(root).as_posix() for p in tool.rglob('*') if p.is_file()}
expected = {r['Path'] for r in freeze['PromotedFiles'] if r['Path'].startswith('tools/')}
require(actual == expected, 'Unexpected/missing allocator tool file')
plan = json.loads((tool / 'reviewed-execution-plan.json').read_bytes())
require(plan['ObservedPlanSha256'] == freeze['ObservationPlanSha256'], 'Observation plan binding')
require(plan['OriginalExecuted'] is False and plan['SdkCompared'] is False, 'Source-only seal scope')
require({r['Path'] for r in plan['Files']} == (expected - {'tools/Erlang.AllocatorMemoryObservations/reviewed-execution-plan.json'}) | {'.github/workflows/allocator-memory-original-observations.yml'} | {r['Path'] for r in freeze['UnchangedPublishedHelpers']}, 'Execution source closure')
for row in plan['Files']:
    actual_row = record(root / row['Path'], row['Path'])
    require(actual_row == {k: row[k] for k in actual_row} and row['HashMode'] == 'BinaryExact', 'Execution record drift: ' + row['Path'])
audit = here.parent / 'allocator-memory309-source-audit1/source-freeze.json'
require(hashlib.sha256(audit.read_bytes()).hexdigest().upper() == freeze['SourceAuditFreezeSha256'], 'Source audit seal drift')
for row in json.loads(audit.read_bytes())['Files']:
    require(record(audit.parent / row['Path'], row['Path']) == row, 'Source audit member drift')
print(json.dumps({'Status': 'FROZEN_SOURCE_CLOSURE_PASS', 'PromotedFiles': len(freeze['PromotedFiles']), 'UnchangedPublishedHelpers': len(freeze['UnchangedPublishedHelpers']), 'OriginalExecuted': False, 'SdkCompared': False, 'ReadOnly': True}))
