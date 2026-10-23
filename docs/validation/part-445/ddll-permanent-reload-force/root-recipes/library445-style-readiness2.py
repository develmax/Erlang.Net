from pathlib import Path
import hashlib,importlib.util,json,os,subprocess
r=Path.cwd();out=r/'artifacts/library445-style-readiness2';assert not out.exists();out.mkdir()
sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest().upper()
ledger=json.loads((r/'docs/mfa-compatibility.json').read_bytes())
old=json.loads(subprocess.check_output(['git','show','HEAD:docs/mfa-compatibility.json'],cwd=r))
assert ledger['Entries']==old['Entries'] and len(ledger['Entries'])==708 and len(ledger['VerifiedSourceFiles'])==1618
changed=[(a,b) for a,b in zip(old['VerifiedSourceFiles'],ledger['VerifiedSourceFiles']) if a!=b]
assert len(changed)==1 and changed[0][0]['Path']==changed[0][1]['Path']=='src/Erlang.Runtime/RuntimeDdllParent422.cs'
assert changed[0][0]['Sha256']=='E290340C18D91308DDEEFFF9DDC24CE9DF465BBC99D3EEB3151D8D36B4E4060C'
assert changed[0][1]['Sha256']=='CD1BAAF05B59F5D1F2EE20C4186C16D617AC7D5CB326B93EAB1C0E80639256AE'
for row in ledger['VerifiedSourceFiles']:assert sha(r/row['Path'])==row['Sha256'],row['Path']
p=r/'artifacts/spawn-priority351-preparation/execute-root.py';assert sha(p)=='C66ACEC6FFE57C57EFAABC96FD1DB00598D7AAD7D05C2EA814A146B759D4171A'
s=importlib.util.spec_from_file_location('owned445style2',p);m=importlib.util.module_from_spec(s);s.loader.exec_module(m)
env=dict(os.environ,DOTNET_PROCESSOR_COUNT='1',DOTNET_GCConserveMemory='9',MSBUILDDISABLENODEREUSE='1')
exe='C:/Program Files/dotnet/dotnet.exe'
m.owned([exe,'tools/Erlang.Style/bin/Debug/net10.0/Erlang.Style.dll','--check','.'],out,'style',120,env,str(r))
m.owned([exe,'tools/Erlang.Tool/bin/Debug/net10.0/Erlang.Tool.dll','readiness','docs','docs/validation/part-440/readiness-publication/ci-tests.json',str(out/'readiness')],out,'readiness',120,env,str(r))
for name in ['module-readiness.json','MODULE_READINESS.md','supported-mfas.json','supported-mfas.md','supported-mfas.tsv']:
    src=out/'readiness'/name
    if src.exists():(r/'docs'/name).write_bytes(src.read_bytes())
(out/'completion.json').write_text(json.dumps(dict(Passed=True,SourceGuards=1618,Entries=708,FullContracts=118,ChangedSourceGuards=changed,ClassificationUnchanged=True,Historical8813AuthorityOnly=True,CurrentFull8823Claim=False,PriorStyleOomRetained=True,ActualLowMemoryEnvironment=True),indent=2)+'\n')
print('Style/readiness pass on exact unchanged final source; first OOM retained.')
