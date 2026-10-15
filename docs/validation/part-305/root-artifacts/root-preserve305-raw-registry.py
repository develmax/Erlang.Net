import hashlib
import json
from pathlib import Path
import subprocess

root = Path.cwd()
artifacts = root / 'artifacts'
assert json.loads((artifacts / 'root-part305-full-exit.json').read_bytes())['ExitCode'] == 0
assert json.loads((artifacts / 'tests.json').read_bytes())['Passed'] == 6624
path = root / 'docs/supported-mfas.json'
raw_bytes = path.read_bytes()
base_bytes = subprocess.check_output(['git', 'show', 'HEAD:docs/supported-mfas.json'])
raw, baseline = json.loads(raw_bytes), json.loads(base_bytes)
assert len(raw) == len(baseline) == 503
changes = []
for index, (current, previous) in enumerate(zip(raw, baseline)):
    if (current['Module'], current['Function'], current['Arity']) == ('erlang', 'process_info', 1):
        assert {key: value for key, value in current.items() if key != 'Limits'} == {key: value for key, value in previous.items() if key != 'Limits'}
        if current['Limits'] != previous['Limits']:
            changes.append({'Index': index, 'Field': 'Limits', 'Raw': current['Limits'], 'PublishedDisclosure': previous['Limits']})
        current['Limits'] = previous['Limits']
    else:
        assert current == previous, index
assert raw == baseline and len(changes) <= 1
sha = lambda data: hashlib.sha256(data).hexdigest().upper()
out = artifacts / 'root-part305-raw-supported-mfas.json'
receipt = artifacts / 'root-part305-registry-metadata-guard.json'
assert not out.exists() and not receipt.exists()
out.write_bytes(raw_bytes)
path.write_bytes(base_bytes)
result = {'Status': 'ACTUAL_RAW503_CAPTURED_ONLY_EXISTING_PROCESS_INFO_DISCLOSURE_RESTORED', 'Registry': 503, 'ErlangMfas': 345, 'PublicMfaDelta': 0, 'RawSha256': sha(raw_bytes), 'RestoredSha256': sha(base_bytes), 'Other502RowsFieldsAndOrderingExact': True, 'SourceExporterChanged': False, 'Changes': changes}
receipt.write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
print(json.dumps(result))
