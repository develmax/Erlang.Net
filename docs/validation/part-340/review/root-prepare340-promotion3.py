from pathlib import Path
import hashlib,json,subprocess
r=Path.cwd();a=r/'artifacts';d=a/'connected-remote-completion-full-actual4';c=a/'connected-remote-completion-candidate'
h=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
j=lambda p:json.loads(p.read_bytes())
head='36e6da6dfc2696adce6bff8808ca96bbd46596b6'
assert subprocess.check_output(['git','rev-parse','HEAD']).decode().strip()==head
f=j(d/'source-freeze.json');assert h(d/'source-freeze.json')=='F9E57045C8DB34127E9C611A34E3D43FD93274446613B1E4E264ED834A836384'
owned=[x['Path'] for x in f['Files']];assert len(owned)==len(set(owned))==95
assert not any('Focus' in p for p in owned)
closure=j(d/'source-resource-closure.json')['Files'];parents=[];variants=[]
allpaths=sorted(set(owned)|{x['Path'] for x in closure})
for name in allpaths:
 p=r/name
 if p.exists():parents.append(dict(Path=name,Bytes=p.stat().st_size,Sha256=h(p)))
 elif name not in owned:raise AssertionError(name)
for row in closure:
 name=row['Path'];p=c/name;assert p.stat().st_size==row['Bytes'] and h(p)==row['Sha256']
 if name in owned:continue
 m=r/name
 if m.read_bytes()!=p.read_bytes():
  assert not name.endswith('.cs') and m.read_bytes().replace(b'\r\n',b'\n')==p.read_bytes().replace(b'\r\n',b'\n'),name
  variants.append(dict(Path=name,ParentRawSha256=h(m),TestedRawSha256=h(p)))
out=a/'root-part340-promotion-plan3.json';assert not out.exists()
out.write_text(json.dumps(dict(ParentHead=head,SourceFreezeSha256=h(d/'source-freeze.json'),SourceClosureSha256=h(d/'source-resource-closure.json'),PromotionPaths=owned,ParentFiles=parents,UnownedLineEndingVariances=variants,PreparationOnly=True,MainEdited=False),indent=2)+'\n')
print(json.dumps(dict(Owned=len(owned),ExistingParentInputs=len(parents),UnownedLineEndingVariances=len(variants),PlanSha256=h(out))))
