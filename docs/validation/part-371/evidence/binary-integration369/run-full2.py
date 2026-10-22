from pathlib import Path
import hashlib,importlib.util,json,os
r=Path.cwd();o=r/'artifacts/binary-integration369/full2';o.mkdir()
h=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
rows=[]
for base in ['src','tests','tools']:
    for p in sorted((r/base).rglob('*')):
        if p.is_file() and p.suffix in ['.cs','.csproj','.erl','.json','.ps1','.py'] and not set(p.relative_to(r).parts)&{'bin','obj','__pycache__','artifacts'}:
            rows.append(dict(Path=p.relative_to(r).as_posix(),Bytes=p.stat().st_size,Sha256=h(p)))
(o/'source-before.json').write_text(json.dumps(dict(Files=rows),indent=2)+'\n')
ledger=json.loads((r/'docs/mfa-compatibility.json').read_bytes())
assert len(ledger['Entries'])==678 and len(ledger['VerifiedSourceFiles'])==450
for row in ledger['VerifiedSourceFiles']:assert h(r/row['Path'])==row['Sha256'],row['Path']
p=r/'artifacts/spawn-priority351-preparation/execute-root.py';assert h(p)=='C66ACEC6FFE57C57EFAABC96FD1DB00598D7AAD7D05C2EA814A146B759D4171A'
s=importlib.util.spec_from_file_location('binary369_full',p);m=importlib.util.module_from_spec(s);s.loader.exec_module(m)
env=dict(os.environ,DOTNET_PROCESSOR_COUNT='2',MSBUILDDISABLENODEREUSE='1',DOTNET_CLI_TELEMETRY_OPTOUT='1')
ok=True
try:m.owned(['pwsh','-NoProfile','-File',str(r/'tools/validate.ps1')],o,'canonical-full',1800,env,r)
except Exception as error:
    ok=False;(o/'failure.json').write_text(json.dumps(dict(Error=repr(error),RawRetained=True),indent=2)+'\n')
changed=[row['Path'] for row in rows if not(r/row['Path']).exists() or h(r/row['Path'])!=row['Sha256']]
v=dict(Status='PASS_CANONICAL_PIPELINE' if ok and not changed else 'FAIL_RAW_RETAINED',SourceRows=len(rows),SourceChanges=changed,MainPublished=False)
(o/'completion.json').write_text(json.dumps(v,indent=2)+'\n');print(json.dumps(v))
if not ok or changed:raise SystemExit(1)
