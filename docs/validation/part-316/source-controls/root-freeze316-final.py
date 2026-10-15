from pathlib import Path
import json,hashlib
r=Path.cwd();d=r/'artifacts/allocator-public-sys-profile316-composed2';c=d/'candidate';h=lambda b:hashlib.sha256(b).hexdigest().upper();read=lambda p:json.loads(p.read_bytes());write=lambda p,x:p.write_bytes((json.dumps(x,indent=2)+'\n').encode())
a=read(d/'tokens-before.json');b=read(d/'tokens-after.json');assert a==b and len(a)==3
f=read(d/'pre-format-source-freeze.json');core=read(r/'artifacts/allocator-public-sys-profile316-composed1/formatted-core-source-freeze.json')
for e in core['Files']:assert h((c/e['Path']).read_bytes())==e['Sha256']
for e in f['Files']:
 p=c/e['Path'];raw=p.read_bytes();e['BeforeFormattingSha256']=e['Sha256'];e.update(Bytes=len(raw),Sha256=h(raw),Added=not(r/e['Path']).exists(),ParentSha256=h((r/e['Path']).read_bytes()) if (r/e['Path']).exists() else None)
cl=read(d/'pre-format-source-resource-closure.json')
for e in cl['Files']:
 raw=(c/e['Path']).read_bytes();e.update(Bytes=len(raw),Sha256=h(raw))
assert len(cl['Files'])==2241
owned={e['Path'] for e in f['Files']}
for e in cl['Files']:
 if e['Path'] not in owned:assert h((r/e['Path']).read_bytes())==e['Sha256'],e['Path']
for plan in f['Plans']:
 p=c/plan['Path'];assert h(p.read_bytes())==plan['Sha256'] and b'\r' not in p.read_bytes()
 for e in read(p)['Files']:
  raw=(c/e['Path']).read_bytes();raw=raw.replace(b'\r\n',b'\n') if e['HashMode']=='TextLF' else raw;assert len(raw)==e['Bytes'] and h(raw)==e['Sha256']
write(d/'formatted-source-resource-closure.json',cl)
f.update(SourceClosureSha256=h((d/'formatted-source-resource-closure.json').read_bytes()),ExpectedFullPassed=6654,ExpectedRegistry=507,ExpectedErlangMfas=349,All15PreviouslyCompiledCoreFilesRawExact=True,ThreeObserverTokenStreamsPreserved=True,ActualCoreFocusGuardSha256=h((r/'artifacts/allocator-public-sys-profile316-composed1/actual-core-focus-guard.json').read_bytes()),ActualTenSdkGeneratedCasesSha256=h((r/'artifacts/allocator-public-sys-profile316-managed-focus/actual-results.json').read_bytes()),Prior315FailureGuardSha256=h((r/'artifacts/pack315-reallocation-independent-failure-guard.json').read_bytes()))
write(d/'formatted-source-freeze.json',f);print(json.dumps({'FreezeSha256':h((d/'formatted-source-freeze.json').read_bytes()),'ClosureSha256':f['SourceClosureSha256'],'Paths':len(f['Files']),'ExpectedFull':6654}))
