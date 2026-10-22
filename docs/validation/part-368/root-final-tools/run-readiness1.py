from pathlib import Path
import importlib.util,hashlib,json,os,shutil
R=Path.cwd();O=R/'artifacts/queue-integration368/final-readiness1';O.mkdir();D='C:/Program Files/dotnet/dotnet.exe'
P=R/'artifacts/spawn-priority351-preparation/execute-root.py';assert hashlib.sha256(P.read_bytes()).hexdigest().upper()=='C66ACEC6FFE57C57EFAABC96FD1DB00598D7AAD7D05C2EA814A146B759D4171A'
s=importlib.util.spec_from_file_location('owned',P);m=importlib.util.module_from_spec(s);s.loader.exec_module(m)
env=dict(os.environ,DOTNET_PROCESSOR_COUNT='2',MSBUILDDISABLENODEREUSE='1')
m.owned([D,str(R/'tools/Erlang.Tool/bin/Debug/net10.0/Erlang.Tool.dll'),'readiness','docs','artifacts/tests.json','artifacts/readiness-final368'],O,'current',120,env,R)
m.owned([str(R/'artifacts/local-tool/erlang.exe'),'readiness','docs','artifacts/tests.json','artifacts/tool-readiness-final368'],O,'packaged',120,env,R)
names=['MODULE_READINESS.md','API_COMPATIBILITY.md','module-readiness.json','native-infrastructure.json','native-infrastructure.md']
for name in names:
    p=R/'artifacts/readiness-final368'/name;q=R/'artifacts/tool-readiness-final368'/name;assert p.read_bytes()==q.read_bytes(),name
    shutil.copyfile(p,R/'docs/validation/part-368/local-completion'/name)
    if name in ['MODULE_READINESS.md','API_COMPATIBILITY.md','module-readiness.json']:shutil.copyfile(p,R/'docs'/name)
r=json.loads((R/'artifacts/readiness-final368/module-readiness.json').read_bytes())
assert (r['RegisteredMfas'],r['PreviousRegisteredMfas'],r['RegisteredMfaDelta'],r['VerifiedCompatibleMfas'],r['LocalPassedTests'])==(647,617,30,76,7599)
queue=next(x for x in r['Modules']if x['Name']=='queue');assert queue['RegisteredMfas']==queue['ReferenceDeclarations']==queue['VerifiedCompatibleMfas']==38
(O/'acceptance.json').write_text(json.dumps(dict(Status='PASS_CURRENT_PACKAGED_READINESS_BYTE_EQUAL',Files=names,Apis=647,Complete=76,Queue=queue,PreviousApis=617,Delta=30,Tests=7599,Published=False),indent=2)+'\n')
print('Current and installed packaged readiness byte-equal:647 APIs/76complete,queue38/38,delta+30')
