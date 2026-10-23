from pathlib import Path
import hashlib,json,shutil
r=Path.cwd();old=r/'artifacts/library444-ddll-modes-proposal1';new=r/'artifacts/library445-retained444-proposal1'
sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest().upper()
save=lambda p,v:p.write_text(json.dumps(v,indent=2)+'\n',encoding='utf-8')
assert not new.exists() and sha(old/'manifest.json')=='7BF96E306DB2F0FC11DC4CA19BA3579CC8B333E37A26B6F29C20D76716C205E8'
for row in json.loads((old/'manifest.json').read_bytes())['Files']:assert sha(old/row['Path'])==row['Sha256']
shutil.copytree(old,new)
bindings=json.loads((new/'source-bindings.json').read_bytes());target='src/Erlang.Runtime/RuntimeDdllParent422.cs'
assert bindings[target]=='E290340C18D91308DDEEFFF9DDC24CE9DF465BBC99D3EEB3151D8D36B4E4060C'
assert sha(r/target)=='CD1BAAF05B59F5D1F2EE20C4186C16D617AC7D5CB326B93EAB1C0E80639256AE'
for path,digest in bindings.items():
    if path!=target:assert sha(r/path)==digest,path
bindings[target]=sha(r/target);save(new/'source-bindings.json',bindings)
with (new/'README.md').open('a',encoding='utf-8') as f:f.write('\nRoot Part445 retained successor changes exactly the reviewed RuntimeDdllParent422.cs binding for three ordered permanent notification passes (E290340C to CD1BAAF0); all other binding assertions and actual consumer/worker sources are unchanged. This replays the actual historical original72 against final445 assemblies; no new original VM or old228 rerun is claimed.\n')
rows=[dict(Path=p.relative_to(new).as_posix(),Bytes=p.stat().st_size,Sha256=sha(p)) for p in sorted(new.rglob('*')) if p.is_file() and p.name not in {'manifest.json','freeze.json'}]
save(new/'manifest.json',dict(Files=rows));save(new/'freeze.json',dict(ManifestSha256=sha(new/'manifest.json'),PayloadFiles=len(rows),Immutable=True,RootReviewedSemanticDelta=True))
print('Retained444 successor prepared:',sha(new/'manifest.json'))
