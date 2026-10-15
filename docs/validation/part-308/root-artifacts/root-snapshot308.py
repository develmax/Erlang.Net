from pathlib import Path
import hashlib,json,subprocess
r=Path.cwd();base=r/'artifacts/root-part308-combined-isolated';dest=base/'candidate';assert not base.exists()
head=subprocess.check_output(['git','rev-parse','HEAD']).decode().strip();assert head=='8b83560372b63b4760604ac088802fb74ac1c112'
assert not subprocess.check_output(['git','diff','--name-only','-z','--','src','tests','tools','build','global.json','Directory.*'])
paths=subprocess.check_output(['git','ls-files','-z','--','src','tests','tools','build','examples','benchmarks','.github','global.json','Directory.*','.editorconfig','Erlang.Net.slnx','NuGet.Config','nuget.config']).decode().split('\0')
paths=sorted(set(paths)-{''});sha=lambda b:hashlib.sha256(b).hexdigest().upper();rows=[]
for name in paths:
 p=r/name;assert p.is_file() and not {'bin','obj','__pycache__','artifacts','.git'}.intersection(Path(name).parts),name
 b=p.read_bytes();q=dest/name;q.parent.mkdir(parents=True,exist_ok=True);q.write_bytes(b);rows.append({'Path':name,'Bytes':len(b),'Sha256':sha(b)})
v={'Status':'EXACT_PUBLISHED307_SOURCE_RESOURCE_BASELINE_ONLY','ParentHead':head,'Files':rows,'CandidateOverlaysApplied':False,'BuildExecuted':False,'MainEdited':False}
out=base/'baseline-snapshot.json';out.write_text(json.dumps(v,indent=2)+'\n',encoding='utf-8');print(json.dumps({'SnapshotFiles':len(rows),'ReceiptSha256':sha(out.read_bytes())}))
