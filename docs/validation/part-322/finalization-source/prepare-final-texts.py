"""Root executes after real gates; writes fresh artifact texts only, never Main."""
from pathlib import Path
import argparse, hashlib, json
p=argparse.ArgumentParser()
p.add_argument('--full-guard-sha256',required=True)
p.add_argument('--native-guard-sha256',required=True)
p.add_argument('--output',required=True,type=Path)
a=p.parse_args();r=Path.cwd().resolve();here=Path(__file__).resolve().parent;out=a.output.resolve()
sha=lambda b:hashlib.sha256(b).hexdigest().upper();read=lambda p:json.loads(p.read_bytes())
assert out.is_relative_to(r/'artifacts') and not out.exists()
fp=r/'artifacts/root-part322-main1/source-freeze.json';fullpath=fp.parent/'full-guard.json';np=r/'artifacts/root-part322-main-windows-native1/guard.json'
assert sha(fp.read_bytes())=='5193716ADE7ACC40D42806BBAADFF28FE384804AFB6FC7B62BCA0E2023C83EA9'
assert sha(fullpath.read_bytes())==a.full_guard_sha256.upper() and sha(np.read_bytes())==a.native_guard_sha256.upper()
full,native=read(fullpath),read(np)
assert (full['Passed'],full['Failed'],full['IntegrationResults'],full['Registry'],full['PublicMfaDelta'])==(6681,0,11,509,0)
assert full['SourceFreezeSha256']==sha(fp.read_bytes()) and full['SourceClosureSha256']=='4D4469862CACFE37DF2B0AFBF4A91BFB3A99BC7B04285192296E459E3FBF1042'
assert native['FullPassed']==6681 and native['SourceFilesRehashed']==2332 and native['OriginalExecuted'] is False
assert native['ActualFullGuard']['Sha256']==sha(fullpath.read_bytes()) and native['SourceFreezeSha256']==sha(fp.read_bytes())
entry=(here/'english-entry.md').read_text();note=(here/'runtime-note.txt').read_text()
actual='Actual Main passed 6681/6681 tests and eleven integration/package checks outside the known native named-pipe sandbox restriction; all 6678 previous ordered names were preserved. Three fresh separate post-full trusted Windows roles passed. Actual readiness CLI exit 0 and the 509/509/delta 0 comparison against immutable part 321 remain required before finalization.'
assert entry.count('[FINAL_GATES_PENDING]')==note.count('[FINAL_GATES_PENDING]')==1
entry=entry.replace('[FINAL_GATES_PENDING]',actual).replace('implementation and isolated SDK validation completed; final Main, native and readiness receipts must be bound before publication.','implementation and local Main/native validation completed; actual readiness must be bound before publication.')
note=note.replace('[FINAL_GATES_PENDING]',actual)
out.mkdir()
for name,text in [('english-entry.md',entry),('runtime-note.txt',note)]:
    (out/name).write_text(text.rstrip()+'\n',encoding='utf-8')
(out/'text-bindings.json').write_text(json.dumps(dict(FullGuardSha256=sha(fullpath.read_bytes()),NativeGuardSha256=sha(np.read_bytes()),MainFreezeSha256=sha(fp.read_bytes()),PreparerSha256=sha(Path(__file__).read_bytes()),OriginalExecuted=False),indent=2)+'\n',encoding='utf-8')
print(str(out))
