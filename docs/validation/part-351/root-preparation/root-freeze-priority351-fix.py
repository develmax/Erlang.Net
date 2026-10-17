from pathlib import Path
import json,hashlib,copy
r=Path.cwd();p=r/'artifacts/spawn-priority351-preparation';c=r/'artifacts/spawn-priority351-candidate';sha=lambda b:hashlib.sha256(b).hexdigest().upper();read=lambda p:json.loads(p.read_bytes())
prior=read(p/'runtime-styled-source-freeze.json');closure=read(p/'runtime-build-closure.json');changed='src/Erlang.Runtime/ProcessSpawnOptions.cs'
old=(p/'runtime-styled-source'/changed).read_bytes();new=(c/changed).read_bytes()
assert old.replace(b'var priority] }:',b'var priorityValue] }:').replace(b'ProcessPriorityValues.Parse(priority);',b'ProcessPriorityValues.Parse(priorityValue);')==new
assert read(r/'artifacts/spawn-priority351-core-build-actual1/exit.json')['ExitCode']==1
for row in closure['Files']:
    if row['Path']==changed:row.update(Bytes=len(new),Sha256=sha(new))
    assert sha((c/row['Path']).read_bytes())==row['Sha256']
for row in prior['Files']:
    b=(c/row['Path']).read_bytes()
    if row['Path']==changed:row.update(Bytes=len(b),Sha256=sha(b),PreviousStyledSha256=sha(old))
    assert sha(b)==row['Sha256'];dst=p/'runtime-styled-source2'/row['Path'];dst.parent.mkdir(parents=True,exist_ok=True);dst.write_bytes(b)
def save(name,v):
    dest=p/name
    with dest.open('x',encoding='utf-8',newline='\n') as f:json.dump(v,f,indent=2);f.write('\n')
save('runtime-build-closure2.json',closure)
prior.update(Status='IMMUTABLE_STYLED_PRODUCTION20_AFTER_ROOT_PATTERN_LOCAL_RENAME',RuntimeBuildClosureSha256=sha((p/'runtime-build-closure2.json').read_bytes()),PreviousStyledFreezeSha256=sha((p/'runtime-styled-source-freeze.json').read_bytes()),CompilerFailure='Actual core build1 CS0136 local priority collision and cascade CS8604; no guest tests executed.',RootFix='Rename only scheduling-priority pattern local priority to priorityValue, distinct from monitor priority flag. No option semantics change.',FixPaths=[changed],RootSdkBuildResult='Pending actual2; original source/style/source-peer evidence remains historical.')
save('runtime-styled-source-freeze2.json',prior)
print(json.dumps(dict(SourceFreezeSha256=sha((p/'runtime-styled-source-freeze2.json').read_bytes()),ClosureSha256=sha((p/'runtime-build-closure2.json').read_bytes()),ChangedSha256=sha(new))))
