from pathlib import Path
import hashlib,json,shutil,subprocess
r=Path.cwd()
sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest().upper()
save=lambda p,v:p.write_text(json.dumps(v,indent=2)+'\n',encoding='utf-8')
old=r/'artifacts/library445-ddll-final-modes-proposal2'
new=r/'artifacts/library445-ddll-final-modes-proposal3'
assert not new.exists()
assert sha(old/'manifest.json')=='E0ADFBF485435C77EE652DF434F2D12CA1BDDFA0A1AC5765C877955D98B270F0'
for row in json.loads((old/'manifest.json').read_bytes())['Files']:assert sha(old/row['Path'])==row['Sha256']
target='src/Erlang.Runtime/RuntimeDdllParent422.cs'
assert subprocess.check_output(['git','diff','--name-only','HEAD','--','src','tests','tools'],cwd=r).decode().splitlines()==[target]
before='E290340C18D91308DDEEFFF9DDC24CE9DF465BBC99D3EEB3151D8D36B4E4060C'
after='CD1BAAF05B59F5D1F2EE20C4186C16D617AC7D5CB326B93EAB1C0E80639256AE'
assert sha(r/target)==after
shutil.copytree(old,new)
bindings=json.loads((new/'source-bindings.json').read_bytes());assert bindings[target]==before
for path,digest in bindings.items():
    if path!=target:assert sha(r/path)==digest,path
bindings[target]=after;save(new/'source-bindings.json',bindings)
save(new/'reviewed-source-delta.json',dict(ChangedPath=target,Before=before,After=after,
    SourceAuthority='OTP erl_bif_ddll.c:1022-1043',
    SemanticDelta='Notify AwaitingLoad, AwaitingUnload, AwaitingUnloadOnly in three ordered passes; clear/process/permanent state unchanged.',
    PrefixedActualWholeMatches=54,PrefixedActualWholeDifferences=6,
    OriginalNativeWorkerAndCaptureUnchanged=True,OtherBindingsUnchanged=True))
with (new/'README.md').open('a',encoding='utf-8') as f:f.write('\nRoot successor3 changes exactly one reviewed source binding after the authentic pre-fix54/60: RuntimeDdllParent422.cs three notification passes in pinned order. All other source bindings, observer/native bytes, capture subset and consumer bytes remain identical. Original60 is reused; final fresh real package and matching current assemblies are required. reviewed-source-delta.json records the exact old/new source SHA. This is no arbitrary input refresh.\n')
rows=[dict(Path=p.relative_to(new).as_posix(),Bytes=p.stat().st_size,Sha256=sha(p)) for p in sorted(new.rglob('*')) if p.is_file() and p.name not in {'manifest.json','freeze.json'}]
save(new/'manifest.json',dict(Files=rows));save(new/'freeze.json',dict(ManifestSha256=sha(new/'manifest.json'),PayloadFiles=len(rows),Immutable=True,RootReviewedSemanticDelta=True))
source=(r/'artifacts/library443-prepare-package.py').read_text();assert source.count('artifacts/library443-driver-package1')==1
source=source.replace('artifacts/library443-driver-package1','artifacts/library445-driver-package2')
(r/'artifacts/library445-prepare-package2.py').write_text("from pathlib import Path\n"+"source=Path('artifacts/library443-prepare-package.py').read_text()\nassert source.count('artifacts/library443-driver-package1')==1\nsource=source.replace('artifacts/library443-driver-package1','artifacts/library445-driver-package2')\nexec(compile(source,str(Path(__file__).resolve()),'exec'))\n",encoding='utf-8')
print(json.dumps(dict(Passed=True,Manifest=sha(new/'manifest.json'),ChangedSource=after)))
