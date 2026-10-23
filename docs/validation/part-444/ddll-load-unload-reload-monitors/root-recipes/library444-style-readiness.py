from pathlib import Path
import hashlib,importlib.util,json,os,subprocess
r=Path.cwd();out=r/'artifacts/library444-style-readiness1';assert not out.exists();out.mkdir()
sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest().upper()
ledger=json.loads((r/'docs/mfa-compatibility.json').read_bytes())
old=json.loads(subprocess.check_output(['git','show','HEAD:docs/mfa-compatibility.json'],cwd=r))
assert ledger==old and len(ledger['Entries'])==708 and len(ledger['VerifiedSourceFiles'])==1618
for row in ledger['VerifiedSourceFiles']:assert sha(r/row['Path'])==row['Sha256'],row['Path']
path=r/'artifacts/spawn-priority351-preparation/execute-root.py';assert sha(path)=='C66ACEC6FFE57C57EFAABC96FD1DB00598D7AAD7D05C2EA814A146B759D4171A'
s=importlib.util.spec_from_file_location('owned444final',path);m=importlib.util.module_from_spec(s);s.loader.exec_module(m)
exe='C:/Program Files/dotnet/dotnet.exe'
m.owned([exe,'tools/Erlang.Style/bin/Debug/net10.0/Erlang.Style.dll','--check','.'],out,'style',120,dict(os.environ),str(r))
m.owned([exe,'tools/Erlang.Tool/bin/Debug/net10.0/Erlang.Tool.dll','readiness','docs','docs/validation/part-440/readiness-publication/ci-tests.json',str(out/'readiness')],out,'readiness',120,dict(os.environ),str(r))
(out/'completion.json').write_text(json.dumps(dict(Passed=True,SourceGuards=1618,Entries=708,FullContracts=118,LedgerUnchanged=True,Historical8813AuthorityOnly=True,CurrentFull8823Claim=False),indent=2)+'\n')
print('Style/source/readiness passed; no ledger change or current full-run claim.')
