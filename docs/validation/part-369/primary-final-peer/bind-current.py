"""Passive current source/DLL applicability binding, no contract status changes."""
from pathlib import Path
import hashlib,json

ROOT=Path(__file__).resolve().parents[2]
OUT=Path(__file__).resolve().parent
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest().upper()
acceptance=OUT/'acceptance.json'
data=json.loads(acceptance.read_bytes())
assert data['Status']=='PASS_INDEPENDENT_PASSIVE_PRIMARY_FINAL2_WHOLE_FIELDS'
ledger=ROOT/'docs/mfa-compatibility.json'
document=json.loads(ledger.read_bytes())
guards=document['VerifiedSourceFiles'];assert len(guards)==450
assert len({r['Path'] for r in guards})==450
for row in guards:assert sha(ROOT/row['Path'])==row['Sha256'],row['Path']
copies=data['RetainedActualDlls'];assert len(copies)==5
current=[]
for row in copies:
    actual=sha(ROOT/row['Path'])
    assert sha(ROOT/row['CopiedTo'])==row['Sha256']
    current.append(dict(Path=row['Path'],CapturedSha256=row['Sha256'],CurrentSha256=actual,Equal=actual==row['Sha256']))
controls=ROOT/'artifacts/binary369-comparison-successor1/controls.json'
record=json.loads(controls.read_bytes());assert record['Count']==18 and all(r['Passed'] for r in record['Controls'])
result=dict(Status='PASS_CURRENT_SOURCE450_AND_CAPTURED_DLL_APPLICABILITY' if all(r['Equal'] for r in current) else 'CAPTURED_DLLS_ACCEPTED_CURRENT_DLL_IDENTITY_DIFFERS',PrimaryAcceptanceSha256=sha(acceptance),GuardLedgerSha256=sha(ledger),GuardCount=450,GuardRows=guards,CurrentDlls=current,CurrentDllsMatchCapture=all(r['Equal'] for r in current),ReviewedControlsSha256=sha(controls),ReviewedControls=18,MainClassificationChanged=False,FullRegressionAcceptanceClaimed=False,Limit='Source byte applicability and primary585 oracle cohort only; no new semantic classification or full-suite claim. Test-only memory HOST edits after capture are outside these production guards.')
(OUT/'current-source-binding.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
seal=OUT/'source-results-seal.json';assert not seal.exists()
seal.write_text(json.dumps(dict(Status='IMMUTABLE_FINAL_PRIMARY_PASSIVE_REVIEW',Files=[dict(Path=p.name,Bytes=p.stat().st_size,Sha256=sha(p)) for p in sorted(OUT.iterdir()) if p.is_file() and p.name!='source-results-seal.json']),indent=2)+'\n',encoding='utf-8')
print(json.dumps(dict(Status=result['Status'],AcceptanceSha256=sha(acceptance),SourceBindingSha256=sha(OUT/'current-source-binding.json'),SealSha256=sha(seal))))
