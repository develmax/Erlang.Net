from pathlib import Path
import hashlib,importlib.util,json,os
r=Path.cwd()
p=r/'docs/mfa-compatibility.json';data=json.loads(p.read_bytes());entries=json.dumps(data['Entries'],sort_keys=True)
target='src/Erlang.Runtime/RuntimeDriverFrontend422.Retirement.cs'
rows=[row for row in data['VerifiedSourceFiles'] if row['Path']==target];assert len(rows)==1
previous=rows[0]['Sha256'];rows[0]['Sha256']=hashlib.sha256((r/target).read_bytes()).hexdigest().upper()
assert len(data['Entries'])==708 and len(data['VerifiedSourceFiles'])==1618 and json.dumps(data['Entries'],sort_keys=True)==entries
p.write_text(json.dumps(data,indent=2)+'\n',encoding='utf-8')
owned=r/'artifacts/spawn-priority351-preparation/execute-root.py';s=importlib.util.spec_from_file_location('guard443owned',owned);m=importlib.util.module_from_spec(s);s.loader.exec_module(m)
out=r/'artifacts/library443-style-readiness1';assert not out.exists();out.mkdir()
out.joinpath('source-guard-delta.json').write_text(json.dumps(dict(Path=target,Before=previous,After=rows[0]['Sha256'],SourceGuards=1618,Apis=708,FullContracts=118),indent=2)+'\n')
dotnet='C:/Program Files/dotnet/dotnet.exe'
m.owned([dotnet,'tools/Erlang.Style/bin/Debug/net10.0/Erlang.Style.dll','--check','.'],out,'style',120,dict(os.environ),str(r))
m.owned([dotnet,'tools/Erlang.Tool/bin/Debug/net10.0/Erlang.Tool.dll','readiness','docs','docs/validation/part-440/readiness-publication/ci-tests.json',str(out/'readiness')],out,'readiness',120,dict(os.environ),str(r))
for name in ['module-readiness.json','MODULE_READINESS.md','supported-mfas.json','supported-mfas.md','supported-mfas.tsv']:
    src=out/'readiness'/name
    if src.exists(): (r/'docs'/name).write_bytes(src.read_bytes())
print('Style/readiness passed; actual historical8813 authority, no full8823 execution claim.')
