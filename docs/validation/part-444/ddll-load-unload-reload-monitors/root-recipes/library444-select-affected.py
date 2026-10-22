from pathlib import Path
import importlib.util,json,os,sys
r=Path.cwd();sys.path.insert(0,str(r/'artifacts/library413-final-regression-proposal1'))
from common import snapshot,write,catalog,sha
out=r/'artifacts/library444-affected-selection1';assert not out.exists();out.mkdir()
p=r/'artifacts/spawn-priority351-preparation/execute-root.py';s=importlib.util.spec_from_file_location('owned444catalog',p);m=importlib.util.module_from_spec(s);s.loader.exec_module(m)
m.owned(['C:/Program Files/dotnet/dotnet.exe',str(r/'tests/Erlang.Tests/bin/Debug/net10.0/Erlang.Tests.dll'),'--list','--report',str(out/'catalog.json')],out,'catalog',120,dict(os.environ),str(r))
names=json.loads((r/'artifacts/library443-affected-selection1/selection.json').read_text())['ExactCases'];all_names=catalog(out/'catalog.json')
assert len(names)==len(set(names))==195 and set(names)<=set(all_names)
write(out/'selection.json',dict(ExactCases=names,Count=len(names),CatalogCount=len(all_names),Bindings=snapshot(r),Shards=[dict(Name=f'block-{i//20+1:02}',ExactCases=names[i:i+20]) for i in range(0,len(names),20)],KnownFailure='none',MfaDocumentSha256=sha(r/'docs/mfa-compatibility.json'),CanonicalFullRegression=False,Retained443Cases=195))
print('Affected selection:',len(names),'of',len(all_names))
