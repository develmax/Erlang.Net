"""Bounded future312 proof proposal, including terminal failure evidence; no Main/index writes."""
from pathlib import Path
import hashlib,json,sys
r=Path.cwd();a=r/'artifacts';kind=sys.argv[1];j=lambda p:json.loads(p.read_bytes());mapping=j(a/f'pack311-{kind}-run-map.json');d=a/f'pack311-watch-{mapping["Run"]}-terminal';envelope=j(a/f'pack311-{kind}-terminal-envelope-guard.json');assert envelope['Head']==mapping['Head'] and envelope['Attempt']==1
files=[p for p in d.rglob('*') if p.is_file()]
for name in ['pack311-read-two-runs.ps1','pack311-audit-common.py','pack311-terminal-envelope.py','pack311-verify-terminals.py','pack311-detached-staging.py',f'pack311-{kind}-run-map.json']:
 p=a/name;assert p.exists();files.append(p)
for p in a.glob('pack311-*'):
 if not p.is_file() or 'staging-manifest' in p.name:continue
 if p.name.startswith(f'pack311-{kind}-') or (kind=='native' and p.name.startswith('pack311-original13-')):files.append(p)
if kind=='native':
 for name in ['pack311-original13-source-contracts','pack311-sdk-source-contracts']:files.extend(p for p in (a/name).rglob('*') if p.is_file())
rows=[]
for p in sorted(set(files)):
 assert not {'.git','bin','obj','__pycache__','cache','.cache'}.intersection(p.parts) and not any(x.lower().startswith('license') for x in p.parts)
 data=p.read_bytes();rows.append({'SourcePath':p.relative_to(r).as_posix(),'Destination':f'docs/validation/part-312/previous-head311-{kind}/'+p.relative_to(a).as_posix(),'Bytes':len(data),'Sha256':hashlib.sha256(data).hexdigest().upper(),'HashMode':'BinaryExact'})
out=a/f'pack311-{kind}-part312-detached-staging-manifest.json';assert not out.exists();out.write_text(json.dumps(rows,indent=2)+'\n');print(json.dumps({'Files':len(rows),'Conclusion':envelope['Conclusion'],'MainEdited':False,'IndexEdited':False,'PostExecuted':False}))
