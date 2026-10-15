from pathlib import Path
import json,hashlib
r=Path.cwd();b=r/'artifacts/root-part317-composed1';d=r/'artifacts/root-part317-composed2';c=d/'candidate';assert not d.exists();c.mkdir(parents=True)
read=lambda p:json.loads(p.read_bytes());h=lambda v:hashlib.sha256(v).hexdigest().upper()
f=read(b/'focus-source-freeze.json');cl=read(b/'focus-source-resource-closure.json');assert read(b/'actual-focus.json')['Status']=='PASS' and read(b/'managed-process.json')['Status']=='PASS'
for e in cl['Files']:
 raw=(b/'candidate'/e['Path']).read_bytes();assert h(raw)==e['Sha256'];p=c/e['Path'];p.parent.mkdir(parents=True,exist_ok=True);p.write_bytes(raw)
paths=[str((c/e['Path']).resolve()) for e in f['Files']]
(d/'token-inputs.json').write_bytes((json.dumps(paths,indent=2)+'\n').encode());(d/'includes.rsp').write_text('\n'.join('"'+p+'"' for p in paths)+'\n')
print('Finalcopy2260 inputs/33 owned; compiledfocuscandidate remains immutable.')
