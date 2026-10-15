from pathlib import Path
import hashlib,json,subprocess
r=Path.cwd();a=r/'artifacts';out=a/'root-part311-combined-isolated';c=out/'candidate';assert not out.exists()
head=subprocess.check_output(['git','rev-parse','HEAD']).decode().strip();assert head=='79bfaecaa7d89a03e2248149ae55c3b5bf03e6a8'
assert not subprocess.check_output(['git','diff','HEAD','--name-only','-z'])
paths=subprocess.check_output(['git','ls-files','-z','--','src','tests','tools','build','examples','benchmarks','.github','global.json','Directory.*','.editorconfig','.gitattributes','.gitignore','reference-baseline.json','Erlang.Net.slnx','NuGet.Config','nuget.config']).decode().split('\0')
paths=sorted(set(paths)-{''});assert len(paths)==2150
h=lambda b:hashlib.sha256(b).hexdigest().upper();rows=[]
for name in paths:
 p=r/name;assert p.is_file() and not {'.git','bin','obj','__pycache__','artifacts'}.intersection(Path(name).parts),name
 b=p.read_bytes();q=c/name;q.parent.mkdir(parents=True,exist_ok=True);q.write_bytes(b);rows.append({'Path':name,'Bytes':len(b),'Sha256':h(b)})
v={'Status':'EXACT_PUBLISHED310_AUTHORED_SOURCE_RESOURCE_CONFIGURATION_AND_IGNORE_SNAPSHOT','ParentHead':head,'Files':rows,'OverlaysApplied':False,'BuildExecuted':False,'MainEdited':False}
p=out/'baseline-snapshot.json';p.write_text(json.dumps(v,indent=2)+'\n',encoding='utf-8');print(json.dumps({'BaselineFiles':len(rows),'SnapshotSha256':h(p.read_bytes())}))
