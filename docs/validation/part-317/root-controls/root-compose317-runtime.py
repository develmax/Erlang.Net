from pathlib import Path
import hashlib,json
r=Path.cwd(); a=r/'artifacts/clr-memory-public317-candidate1'; d=r/'artifacts/root-part317-composed1'; c=d/'candidate'
assert not d.exists(); c.mkdir(parents=True)
h=lambda b:hashlib.sha256(b).hexdigest().upper()
read=lambda p:json.loads(p.read_bytes())
assert h((a/'source-freeze.json').read_bytes())=='20F71DC931205AF6BB1C4879418198DD5E767B8636399B1746AAEF511C0D0A6F'
f=read(a/'source-freeze.json'); cl=read(a/'source-resource-closure.json')
assert h((a/'source-resource-closure.json').read_bytes())==f['FinalSourceClosureSha256']
for e in cl['Files']:
 raw=(a/'candidate'/e['Path']).read_bytes(); assert len(raw)==e['Bytes'] and h(raw)==e['Sha256'],e['Path']
 p=c/e['Path']; p.parent.mkdir(parents=True,exist_ok=True); p.write_bytes(raw)
paths=[str((c/e['Path']).resolve()) for e in f['Files']]
(d/'token-inputs.json').write_bytes((json.dumps(paths,indent=2)+'\n').encode())
(d/'format-includes.rsp').write_text('\n'.join('"'+p+'"' for p in paths)+'\n')
print('Copied2257 verified inputs; 28 CSharp paths ready for token-preserving formatting.')
