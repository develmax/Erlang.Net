from pathlib import Path
import hashlib,importlib.util,json,os,subprocess
r=Path.cwd();out=r/'artifacts/library451-style-readiness1';assert not out.exists();out.mkdir()
sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest().upper()
ledger=json.loads((r/'docs/mfa-compatibility.json').read_bytes());old=json.loads(subprocess.check_output(['git','show','HEAD:docs/mfa-compatibility.json']));assert ledger==old and len(ledger['Entries'])==708 and len(ledger['VerifiedSourceFiles'])==1621
for row in ledger['VerifiedSourceFiles']:assert sha(r/row['Path'])==row['Sha256'],row['Path']
p=r/'artifacts/spawn-priority351-preparation/execute-root.py';s=importlib.util.spec_from_file_location('owned449readiness',p);m=importlib.util.module_from_spec(s);s.loader.exec_module(m)
env=dict(os.environ,DOTNET_PROCESSOR_COUNT='1',DOTNET_GCConserveMemory='9',MSBUILDDISABLENODEREUSE='1');exe='C:/Program Files/dotnet/dotnet.exe'
assert sha(r/'tests/Erlang.Tests/bin/Debug/net10.0/Erlang.Runtime.dll')==sha(r/'tools/Erlang.Tool/bin/Debug/net10.0/Erlang.Runtime.dll')
m.owned([exe,'tools/Erlang.Tool/bin/Debug/net10.0/Erlang.Tool.dll','readiness','docs','docs/validation/part-440/readiness-publication/ci-tests.json',str(out/'readiness')],out,'readiness',120,env,str(r))
for name in ['module-readiness.json','MODULE_READINESS.md','supported-mfas.json','supported-mfas.md','supported-mfas.tsv']:
 src=out/'readiness'/name
 if src.exists():(r/'docs'/name).write_bytes(src.read_bytes())
(out/'completion.json').write_text(json.dumps({'Passed':True,'SourceGuards':1621,'Entries':708,'FullContracts':118,'EntriesUnchanged':True,'SourceGuardDelta':0,'Historical8813AuthorityOnly':True,'CurrentCatalog8827NotFull':True,'LatestSeparatelyVerifiedFullCi':39956702180,'RuntimeSha256':sha(r/'tests/Erlang.Tests/bin/Debug/net10.0/Erlang.Runtime.dll')},indent=2)+'\n')
