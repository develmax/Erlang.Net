from pathlib import Path
import json,hashlib
r=Path.cwd();d=r/'artifacts/root-part317-composed1';c=d/'candidate';a=r/'artifacts/clr-memory-public317-candidate1';o=r/'artifacts/clr-memory-public317-oracle1'
read=lambda p:json.loads(p.read_bytes());h=lambda b:hashlib.sha256(b).hexdigest().upper();write=lambda p,v:p.write_bytes((json.dumps(v,indent=2)+'\n').encode())
assert read(d/'tokens-before.json')==read(d/'tokens-after.json')
assert read(d/'oracle-tokens-before.json')==read(d/'oracle-tokens-after.json')
af=read(a/'source-freeze.json');of=read(o/'source-freeze.json');owned={e['Path'] for e in af['Files']}|{e['Path'] for e in of['Files']}
keys={e['Path'] for e in read(a/'source-resource-closure.json')['Files']}|owned
assert len(owned)==33 and len(keys)==2260
row=lambda n:{'Path':n,'Bytes':(c/n).stat().st_size,'Sha256':h((c/n).read_bytes())}
rows=[row(n) for n in sorted(keys)]
for e in rows:
 if e['Path'] not in owned:assert h((r/e['Path']).read_bytes())==e['Sha256'],e['Path']
write(d/'focus-source-resource-closure.json',{'Files':rows})
files=[]
for n in sorted(owned):
 e=row(n);e.update(Added=not(r/n).exists(),ParentSha256=h((r/n).read_bytes()) if (r/n).exists() else None);files.append(e)
write(d/'focus-source-freeze.json',{'ParentHead':'6d7a430f55a43e488257f654e72a53b3b7f96ada','Files':files,'SourcePaths':33,'CSharpPaths':33,'SourceClosurePaths':2260,'SourceClosureSha256':h((d/'focus-source-resource-closure.json').read_bytes()),'NativePlansIntentionallyStalePendingReseal':True,'RuntimeAuthorFreezeSha256':h((a/'source-freeze.json').read_bytes()),'OracleAuthorFreezeSha256':h((o/'source-freeze.json').read_bytes()),'TokenPreservingFormatting':True,'ExplicitPostFormattingSourceCorrection':{'Path':'src/Erlang.Runtime/ClrMemorySnapshot.cs','Reason':'First actual build CS1673: record struct lambda cannot capture this. Explicit fixed-size array and loop retains ordered keys and per-snapshot values.','FirstFailedBuildSha256':h((d/'target-build-first-failed.log').read_bytes())}})
print(json.dumps({'FocusFreezeSha256':h((d/'focus-source-freeze.json').read_bytes()),'Closure':len(keys),'Owned':len(owned)}))
