from pathlib import Path
import hashlib,importlib.util,json,os

r=Path.cwd();prep=r/'artifacts/spawn-priority351-corrected-preparation'
freeze=json.loads((prep/'focus-source-freeze.json').read_bytes());c=Path(freeze['Candidate'])
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
def verify():
 for row in freeze['Files']:
  p=c/row['Path'];assert p.stat().st_size==row['Bytes'] and sha(p)==row['Sha256'],row['Path']
 assert sha(prep/'targets.json')==freeze['TargetsSha256']
verify()
spec=importlib.util.spec_from_file_location('priority_execution',r/'artifacts/spawn-priority351-preparation/execute-root.py')
m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
o=r/'artifacts/spawn-priority351-corrections-focus-actual1';o.mkdir(exist_ok=False)
env=dict(os.environ,DOTNET_PROCESSOR_COUNT='2')
project=c/'tests/Erlang.Priority351.Corrections/Erlang.Priority351.Corrections.csproj'
m.owned(['dotnet','build',str(project),'--nologo','-v:q'],o,'build',150,env,c)
verify()
dll=project.parent/'bin/Debug/net10.0/Erlang.Priority351.Corrections.dll'
targets=json.loads((prep/'targets.json').read_bytes())['Targets'];rows=[]
for i,name in enumerate(targets):
 stem=f'case-{i:03d}';result=o/(stem+'.json');failure=None
 try:m.owned(['dotnet',str(dll),name,str(result)],o,stem,110,env,c)
 except RuntimeError as error:failure=str(error)
 data=json.loads(result.read_bytes()) if result.exists() else None
 rows.append({'Name':name,'Passed':data is not None and data['Passed'] and failure is None,'Result':result.name,'ExecutionFailure':failure})
 print(json.dumps(rows[-1]),flush=True)
verify()
summary={'Passed':sum(x['Passed'] for x in rows),'Failed':sum(not x['Passed'] for x in rows),'Tests':rows,'SourceFreezeSha256':sha(prep/'focus-source-freeze.json'),'InputSourceBytesUnchanged':True,'Scope':'Managed focused correction controls only; no original OTP comparison or full validation claim'}
(o/'summary.json').write_text(json.dumps(summary,indent=2)+'\n',encoding='utf-8')
assert summary['Failed']==0,summary
print(json.dumps({'Passed':summary['Passed'],'Failed':summary['Failed']}))
