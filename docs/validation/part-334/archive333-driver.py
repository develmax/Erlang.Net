from pathlib import Path
import hashlib
import json

root = Path.cwd()
sha = lambda value: hashlib.sha256(value).hexdigest().upper()
summary_path = root / 'artifacts/part333-all-five-terminal-independent-summary.json'
assert sha(summary_path.read_bytes()) == 'B26E9F6E8E12C06D8EF8BAE4D8420A2C8D9C5EC31DB251F3F84DE92E50C34A27'
summary = json.loads(summary_path.read_bytes())
assert summary['Passed'] and summary['Head'] == 'e9725c4b2eef68014aeb70afd0abcf9f646065fd'
assert (summary['CiPassed'], summary['OriginalOrdinaryPassed'], summary['FdPassed']) == (6871, 6042, 8)
assert not summary['StrictOrdinaryRawEqualityPassed'] and summary['OrdinaryMapStdoutVarianceRows'] == 1
rows_path = root / 'artifacts/part333-five-terminal-lean-copy-rows.json'
assert sha(rows_path.read_bytes()) == 'ACF6FC1DD931F53233F66714101D6F18B2C8F9DA43EE770B57180512F6C2EFF8'
rows = json.loads(rows_path.read_bytes())
assert len(rows) == 702
for row in rows:
    source = root / row['SourcePath']
    value = source.read_bytes()
    assert len(value) == row['Bytes'] and sha(value) == row['Sha256'], source
    target = root / row['TargetPath']
    assert target.resolve().is_relative_to((root / 'docs/validation/part-334/prior333').resolve())
    target.parent.mkdir(parents=True, exist_ok=True)
    if target.exists():
        assert target.read_bytes() == value
    else:
        target.write_bytes(value)
base = root / 'docs/validation/part-334'
(base / '.gitattributes').write_bytes(b'* binary\n')
(base / 'prior333-copy-index.json').write_bytes(rows_path.read_bytes())
handoff = root / 'artifacts/part333-remote-new-review/handoff.json'
assert sha(handoff.read_bytes()) == 'C5B369E5478B393A1ACF85C7E6E230FEC65433748C0E28A0E2F94FBC27902908'
(base / 'prior333/handoff.json').write_bytes(handoff.read_bytes())
print(json.dumps({'Copied': len(rows), 'Bytes': sum(row['Bytes'] for row in rows), 'Original': 6042, 'Fd': 8, 'Ci': 6871, 'StrictRawEquality': False}))
