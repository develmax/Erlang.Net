from pathlib import Path
import hashlib,importlib.util,json,os
r=Path.cwd();out=r/'artifacts/library445-style-readiness1';assert not out.exists();out.mkdir()
p=r/'docs/mfa-compatibility.json';data=json.loads(p.read_bytes());entries=json.dumps(data['Entries'],sort_keys=True)
target='src/Erlang.Runtime/RuntimeDdllParent422.cs'
rows=[row for row in data['VerifiedSourceFiles'] if row['Path']==target];assert len(rows)==1
previous=rows[0]['Sha256'];assert previous=='E290340C18D91308DDEEFFF9DDC24CE9DF465BBC99D3EEB3151D8D36B4E4060C'
actual=hashlib.sha256((r/target).read_bytes()).hexdigest().upper();assert actual=='CD1BAAF05B59F5D1F2EE20C4186C16D617AC7D5CB326B93EAB1C0E80639256AE'
rows[0]['Sha256']=actual
assert len(data['Entries'])==708 and len(data['VerifiedSourceFiles'])==1618 and json.dumps(data['Entries'],sort_keys=True)==entries
for row in data['VerifiedSourceFiles']:assert hashlib.sha256((r/row['Path']).read_bytes()).hexdigest().upper()==row['Sha256'],row['Path']
p.write_text(json.dumps(data,indent=2)+'\n',encoding='utf-8')
owned=r/'artifacts/spawn-priority351-preparation/execute-root.py';s=importlib.util.spec_from_file_location('guard445owned',owned);m=importlib.util.module_from_spec(s);s.loader.exec_module(m)
out.joinpath('source-guard-delta.json').write_text(json.dumps(dict(Path=target,Before=previous,After=actual,SourceGuards=1618,Apis=708,FullContracts=118),indent=2)+'\n')
dotnet='C:/Program Files/dotnet/dotnet.exe'
m.owned([dotnet,'tools/Erlang.Style/bin/Debug/net10.0/Erlang.Style.dll','--check','.'],out,'style',120,dict(os.environ),str(r))
m.owned([dotnet,'tools/Erlang.Tool/bin/Debug/net10.0/Erlang.Tool.dll','readiness','docs','docs/validation/part-440/readiness-publication/ci-tests.json',str(out/'readiness')],out,'readiness',120,dict(os.environ),str(r))
for name in ['module-readiness.json','MODULE_READINESS.md','supported-mfas.json','supported-mfas.md','supported-mfas.tsv']:
    src=out/'readiness'/name
    if src.exists():(r/'docs'/name).write_bytes(src.read_bytes())
(out/'completion.json').write_text(json.dumps(dict(Passed=True,SourceGuards=1618,Entries=708,FullContracts=118,ChangedSourceGuard=target,ClassificationUnchanged=True,Historical8813AuthorityOnly=True,CurrentFull8823Claim=False),indent=2)+'\n')
print('Style/readiness pass; exactly one reviewed source guard changed, no API promotion or fresh full-run claim.')
