from pathlib import Path
import json,hashlib
r=Path.cwd();b=r/'artifacts/root-part317-composed1';d=r/'artifacts/root-part317-composed2';c=d/'candidate';h=lambda v:hashlib.sha256(v).hexdigest().upper();read=lambda p:json.loads(p.read_bytes())
before=read(d/'tokens-before.json');after=read(d/'tokens-after.json');assert before==after and len(before)==33
v=read(b/'focus-source-freeze.json')
for e in v['Files']:
 raw=(c/e['Path']).read_bytes();e['CompiledFocusSha256']=e['Sha256'];e.update(Bytes=len(raw),Sha256=h(raw))
v.update(Status='ROOT_FINAL_FORMATTED317_PENDING_NATIVE_PLAN_RESEAL',FocusFreezeSha256=h((b/'focus-source-freeze.json').read_bytes()),All33CompiledFocusTokenStreamsPreserved=True,ActualFiveHostGroupsSha256=h((b/'actual-focus.json').read_bytes()),ActualElevenGeneratedSdkCasesSha256=h((b/'managed-actual-results.json').read_bytes()),ActualManagedProcessReceiptSha256=h((b/'managed-process.json').read_bytes()))
p=d/'formatted-source-freeze.json';p.write_bytes((json.dumps(v,indent=2)+'\n').encode());print(h(p.read_bytes()))
