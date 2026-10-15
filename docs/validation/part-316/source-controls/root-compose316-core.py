from pathlib import Path
import hashlib,json
r=Path.cwd();d=r/'artifacts/allocator-public-sys-profile316-composed1';c=d/'candidate';assert not d.exists();c.mkdir(parents=True)
h=lambda b:hashlib.sha256(b).hexdigest().upper()
read=lambda p:json.loads(p.read_bytes())
a=r/'artifacts/allocator-public-sys-profile316-candidate1';f=read(a/'source-freeze.json');assert h((a/'source-freeze.json').read_bytes())=='1CE19786E21B11FCE6551ABA0F34E758AC6995CABCFDCFDD63B25572841019D8'
cl=a/'source-resource-closure.json';assert h(cl.read_bytes())==f['SourceClosureSha256']
for e in read(cl)['Files']:
 b=(a/'candidate'/e['Path']).read_bytes();assert len(b)==e['Bytes'] and h(b)==e['Sha256'];p=c/e['Path'];p.parent.mkdir(parents=True,exist_ok=True);p.write_bytes(b)
o=r/'artifacts/allocator-public-sys-profile316-oracle1';of=read(o/'source-freeze.json');assert h((o/'source-freeze.json').read_bytes())=='B0A2DEC2CC4E8DE5518EF5E0D248C81D2F370645CB69C0209EA13453E3275046'
owned={e['Path'] for e in f['Files']}
for e in of['Files']:
 p=c/e['Path'];assert not p.exists() if e['Added'] else h(p.read_bytes())==e['ParentSha256'];b=(o/'overlay'/e['Path']).read_bytes();assert len(b)==e['Bytes'] and h(b)==e['Sha256'];p.parent.mkdir(parents=True,exist_ok=True);p.write_bytes(b);owned.add(e['Path'])
assert len(owned)==15
(d/'pre-format-token-inputs.json').write_text(json.dumps([str((c/n).resolve()) for n in sorted(owned)],indent=2)+'\n')
rows=[{'Path':n,'Bytes':(c/n).stat().st_size,'Sha256':h((c/n).read_bytes())} for n in sorted(owned)]
(d/'pre-format-source-freeze.json').write_text(json.dumps({'ParentHead':f['ParentHead'],'Files':rows,'RuntimeAuthorFreezeSha256':h((a/'source-freeze.json').read_bytes()),'OracleAuthorFreezeSha256':h((o/'source-freeze.json').read_bytes()),'NativePlansStalePendingReseal':True},indent=2)+'\n');print('Composed15CSharp; native plans intentionally stale before final reseal.')
