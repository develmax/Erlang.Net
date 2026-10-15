from pathlib import Path
import json,hashlib
r=Path.cwd();d=r/'artifacts/root-part317-composed1';c=d/'candidate';a=r/'artifacts/clr-memory-public317-oracle1'
read=lambda p:json.loads(p.read_bytes());h=lambda b:hashlib.sha256(b).hexdigest().upper()
assert read(d/'tokens-before.json')==read(d/'tokens-after.json')
assert h((a/'source-freeze.json').read_bytes())=='761B9C1EFE11D4CE3F262DB52B9DD51D1CC07B60CD065E9FF28CFFC48D772EC1'
f=read(a/'source-freeze.json')
for e in f['Files']:
 p=c/e['Path']; assert not p.exists() if e['Added'] else h(p.read_bytes())==e['ParentSha256'],e['Path']
 raw=(a/'overlay'/e['Path']).read_bytes();assert len(raw)==e['Bytes'] and h(raw)==e['Sha256'];p.parent.mkdir(parents=True,exist_ok=True);p.write_bytes(raw)
paths=[str((c/e['Path']).resolve()) for e in f['Files']]
(d/'oracle-token-inputs.json').write_bytes((json.dumps(paths,indent=2)+'\n').encode())
(d/'oracle-includes.rsp').write_text('\n'.join('"'+p+'"' for p in paths)+'\n')
print('Runtime28 token streams preserved; five oracle paths overlaid.')
