"""Bind the passive historical report to retained raw evidence and copied DLLs."""
from pathlib import Path
import hashlib, json

ROOT = Path(__file__).resolve().parents[3]
OUT = Path(__file__).resolve().parent
SDK = ROOT / 'artifacts/binary-integration369/capture1/sdk'

def sha(p):
    return hashlib.sha256(p.read_bytes()).hexdigest().upper()

closure = json.loads((SDK.parent / 'main-dll-closure.json').read_bytes())
assert len(closure['Files']) == 5
for row in closure['Files']:
    assert sha(ROOT / row['CopiedTo']) == row['Sha256']
for row in json.loads((SDK / 'capture-seal.json').read_bytes())['Files']:
    path = SDK / row['Path']
    assert path.resolve().is_relative_to(SDK.resolve()) and sha(path) == row['Sha256']
    assert path.stat().st_size == row['Bytes']
comparison = json.loads((OUT / 'comparison.json').read_bytes())
for row in comparison['Inputs']:
    assert sha(Path(row['Path'])) == row['Sha256']
summary = json.loads((OUT / 'summary.json').read_bytes())
fields = json.loads((OUT / 'frame-diagnostics.json').read_bytes())
assert len(fields['FirstDifferingFrameFields']) == summary['OtherDifferences'] == 1006
assert fields['Counts'].get('values_other', 0) == 0
report = dict(Status='PASS_PASSIVE_ACQUISITION_OF_HISTORICAL_DIFFERENCES_NOT_COMPATIBILITY', OriginalAdmissionSha256=sha(ROOT / 'artifacts/binary369-original-admission-proposal2/actual1/acceptance.json'), ExpectedRows=3510, Rows=3510, StrictEqual=2486, OpaqueOnlyUnaccepted=18, OtherFullStackDifferences=1006, NonopaqueValueDifferences=0, CopiedSdkDllCount=5, SdkClosureSha256=sha(SDK.parent / 'main-dll-closure.json'), SdkCaptureSealSha256=sha(SDK / 'capture-seal.json'), PrimaryCause='Native binary BIF errors retain source probe caller; historical SDK RuntimeTailCall unwinds it before invocation. Erlang source wrapper errors additionally miss binary.erl location metadata in historical capture.', CurrentRuntimeAcceptance=False, FieldsProjected=False, RuntimeExecuted=False, NativeExecuted=False)
(OUT / 'receipt.json').write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
path = OUT / 'source-results-seal.json'
assert not path.exists()
path.write_text(json.dumps(dict(Status='IMMUTABLE_HISTORICAL_EXPLORATORY_REVIEW', Files=[dict(Path=p.name, Bytes=p.stat().st_size, Sha256=sha(p)) for p in sorted(OUT.iterdir()) if p.is_file() and p.name != path.name]), indent=2) + '\n', encoding='utf-8')
print(json.dumps(dict(ReceiptSha256=sha(OUT / 'receipt.json'), SealSha256=sha(path))))
