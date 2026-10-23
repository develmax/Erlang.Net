from pathlib import Path
import hashlib,importlib.util,json,os,subprocess
r=Path.cwd();out=r/'artifacts/library448-style-readiness1';assert not out.exists();out.mkdir()
sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest().upper()
ledger=json.loads((r/'docs/mfa-compatibility.json').read_bytes());old=json.loads(subprocess.check_output(['git','show','HEAD:docs/mfa-compatibility.json']))
assert ledger['Entries']==old['Entries'] and len(ledger['Entries'])==708 and len(ledger['VerifiedSourceFiles'])==1621
delta=json.loads((r/'artifacts/library448-source-guard-delta.json').read_bytes());assert len(delta['Changed'])==4 and sum(x['Added'] for x in delta['Changed'])==3
before={x['Path']:x['Sha256'] for x in old['VerifiedSourceFiles']};after={x['Path']:x['Sha256'] for x in ledger['VerifiedSourceFiles']}
assert {x for x in after if before.get(x)!=after[x]}=={x['Path'] for x in delta['Changed']}
for row in ledger['VerifiedSourceFiles']:assert sha(r/row['Path'])==row['Sha256'],row['Path']
p=r/'artifacts/spawn-priority351-preparation/execute-root.py';assert sha(p)=='C66ACEC6FFE57C57EFAABC96FD1DB00598D7AAD7D05C2EA814A146B759D4171A';s=importlib.util.spec_from_file_location('owned448style',p);m=importlib.util.module_from_spec(s);s.loader.exec_module(m)
env=dict(os.environ,DOTNET_PROCESSOR_COUNT='1',DOTNET_GCConserveMemory='9',MSBUILDDISABLENODEREUSE='1');exe='C:/Program Files/dotnet/dotnet.exe'
assert sha(r/'tests/Erlang.Tests/bin/Debug/net10.0/Erlang.Runtime.dll')==sha(r/'tools/Erlang.Tool/bin/Debug/net10.0/Erlang.Runtime.dll')
m.owned([exe,'tools/Erlang.Style/bin/Debug/net10.0/Erlang.Style.dll','--check','.'],out,'style',120,env,str(r))
m.owned([exe,'tools/Erlang.Tool/bin/Debug/net10.0/Erlang.Tool.dll','readiness','docs','docs/validation/part-440/readiness-publication/ci-tests.json',str(out/'readiness')],out,'readiness',120,env,str(r))
for name in ['module-readiness.json','MODULE_READINESS.md','supported-mfas.json','supported-mfas.md','supported-mfas.tsv']:
 src=out/'readiness'/name
 if src.exists():(r/'docs'/name).write_bytes(src.read_bytes())
(out/'completion.json').write_text(json.dumps(dict(Passed=True,SourceGuards=1621,Entries=708,FullContracts=118,SourceGuardDelta=delta,EntriesUnchanged=True,Historical8813AuthorityOnly=True,CurrentCatalog8824NotFull=True,RuntimeSha256=sha(r/'tests/Erlang.Tests/bin/Debug/net10.0/Erlang.Runtime.dll')),indent=2)+'\n')
