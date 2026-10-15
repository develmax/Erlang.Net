"""Future313 evidence manifest only; never copies to Main or changes Git/index."""
from pathlib import Path
import hashlib,json,sys
sys.dont_write_bytecode=True
r=Path.cwd();a=r/'artifacts';kind=sys.argv[1];assert kind in ['ci','native','ordinary'];j=lambda p:json.loads(p.read_bytes());mp=a/f'pack312-{kind}-run-map.json';mapping=j(mp);d=a/f'pack312-watch-{mapping["Run"]}-terminal';envelope=j(a/f'pack312-{kind}-terminal-envelope-guard.json');assert envelope['Head']==mapping['Head'] and envelope['Attempt']==1
files=[p for p in d.rglob('*') if p.is_file()]
for name in ['pack312-audit-common.py','pack312-terminal-envelope.py','pack312-detached-staging.py',mp.name]:files.append(a/name)
files.extend(p for p in (a/'part312-readonly-monitor').iterdir() if p.is_file())
for p in a.glob('pack312-*'):
 if not p.is_file() or 'staging-manifest' in p.name:continue
 if p.name.startswith(f'pack312-{kind}-') or kind=='native' and p.name.startswith('pack312-original13-'):files.append(p)
if kind=='ci':files.append(a/'root-part312-stage-index-guard.json')
if kind=='native':
 for name in ['pack312-original13-source-contracts','pack312-sdk-source-contracts']:files.extend(p for p in (a/name).rglob('*') if p.is_file())
rows=[]
for p in sorted(set(files)):
 assert p.is_file() and not {'.git','bin','obj','__pycache__','cache','.cache'}.intersection(p.parts) and not any(x.lower().startswith('license') for x in p.parts)
 data=p.read_bytes();rows.append({'SourcePath':p.relative_to(r).as_posix(),'Destination':f'docs/validation/part-313/previous-head312-{kind}/'+p.relative_to(a).as_posix(),'Bytes':len(data),'Sha256':hashlib.sha256(data).hexdigest().upper(),'HashMode':'BinaryExact'})
out=a/f'pack312-{kind}-part313-detached-staging-manifest.json';assert not out.exists();out.write_text(json.dumps(rows,indent=2)+'\n');print(json.dumps({'Files':len(rows),'RawBytes':sum(x['Bytes'] for x in rows),'Conclusion':envelope['Conclusion'],'MainEdited':False,'IndexEdited':False,'PostExecuted':False}))
