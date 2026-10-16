from pathlib import Path
import json,hashlib,re,base64
out=Path('artifacts/proc-lib337-permanent-tests');f=json.loads((out/'source-freeze.json').read_bytes());prov=json.loads((out/'original-expected-provenance.json').read_bytes());rows=prov['Rows'];assert len(rows)==76
for r in rows:
 b=Path(r['OriginalPath']).read_bytes();assert hashlib.sha256(b).hexdigest().upper()==r['ExpectedEtfSha256'] and len(b)==r['ExpectedEtfBytes']
for r in f['Files']:
 b=(out/'source'/r['Path']).read_bytes();assert len(b)==r['Bytes'] and hashlib.sha256(b).hexdigest().upper()==r['Sha256']
report=dict(Status='PASS_PASSIVE_PERMANENT_EXPECTED_PROVENANCE_AND_FILES',SourceFreezeSha256=hashlib.sha256((out/'source-freeze.json').read_bytes()).hexdigest().upper(),ExpectedRows76=True,ReportRows=sum(r['Track']=='report' for r in rows),PrimitiveRows=sum(r['Track']!='report' for r in rows),RawReportRows=sum(r['Track']=='report' and r['Name'].endswith('-raw') for r in rows),OriginalExpectedEtfAllRehashed=True,SourceFilesRehashed=len(f['Files']),SdkExpectedUsed=False,SdkExecuted=False)
(out/'passive-check.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
