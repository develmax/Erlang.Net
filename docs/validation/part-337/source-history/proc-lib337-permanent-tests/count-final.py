from pathlib import Path
import json,runpy,hashlib
out=Path('artifacts/proc-lib337-permanent-tests');p=out/'passive-check.json';j=json.loads(p.read_bytes());decode=runpy.run_path('artifacts/root-formatter-compare.py')['decode'];prov=json.loads((out/'original-expected-provenance.json').read_bytes());n=0
for r in prov['Rows']:
 if r['Track']=='report':
  v=decode(Path(r['OriginalPath']).read_bytes());n+=int(v[0]=='tuple' and v[1][0]==('atom','raw'))
j['RawReportRows']=n;j['RawReportCountDerivedFromDecodedOriginalTag']=True;assert n==12;p.write_text(json.dumps(j,indent=2)+'\n');print(hashlib.sha256(p.read_bytes()).hexdigest().upper())
