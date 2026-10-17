from pathlib import Path
import hashlib, json

r=Path.cwd();old=r/'artifacts/spawn-priority351-full-candidate'
new=r/'artifacts/spawn-priority351-corrected-candidate';new.mkdir(exist_ok=False)
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
closure=json.loads((r/'artifacts/spawn-priority351-full-source2/source-resource-closure.json').read_bytes())['Files']
for row in closure:
 p=old/row['Path'];assert sha(p)==row['Sha256'],row['Path']
 d=new/row['Path'];d.parent.mkdir(parents=True,exist_ok=True);d.write_bytes(p.read_bytes())
(new/'docs/supported-mfas.json').write_bytes((r/'artifacts/spawn-priority351-full-source2/registry-before.json').read_bytes())
changes=[]
for folder,expected in [('spawn-priority351-host-corrections-proposal1','2A64376F5EDFF3D54CA14820C97598E3E6D66C1E957E37A323ABAFBCA7BE783C'),('spawn-priority351-port-retired-proposal','6FF4F122A56670065A7BBBFC4E292F7991F385190CEC0DF1C80A03FA52E4A5D6')]:
 prep=r/'artifacts'/folder;manifest=prep/'source-freeze.json';assert sha(manifest)==expected
 for row in json.loads(manifest.read_bytes())['Files']:
  dst=new/row['Path'];src=prep/'source'/row['Path']
  assert sha(dst)==row['ParentSha256'],row
  assert sha(src)==row['Sha256'],row
  dst.write_bytes(src.read_bytes());changes.append({'Path':row['Path'],'Sha256':sha(dst),'ParentSha256':row['ParentSha256']})
for name in ['MfaCompatibilityMarkdown','MfaCompatibilityNames','Readiness','ReadinessText']:
 p='tools/Erlang.Tool/'+name+'.cs';dst=new/p;before=sha(dst);dst.write_bytes((r/p).read_bytes());changes.append({'Path':p,'Sha256':sha(dst),'ParentSha256':before})
out=r/'artifacts/spawn-priority351-corrected-preparation';out.mkdir(exist_ok=False)
(out/'corrections.json').write_text(json.dumps({'Parent':'immutable failed351 actual1 source','Changes':changes,'CopiedClosureCount':len(closure),'Execution':'Not yet built or run'},indent=2)+'\n')
print(json.dumps({'Candidate':str(new),'ChangedFiles':len(changes),'Copied':len(closure)}))
