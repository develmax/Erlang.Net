"""Root-reviewed exact-path staging proposal; author never executes."""
from pathlib import Path
import hashlib,json,subprocess
r=Path.cwd().resolve();a=r/'artifacts';o=r/'docs/validation/part-292';h=lambda b:hashlib.sha256(b).hexdigest().upper();j=lambda p:json.loads(p.read_bytes());git=lambda *x:subprocess.check_output(['git',*x],cwd=r)
assert git('rev-parse','HEAD').decode().strip()=='9c5b8a809b2a6bcd76965cdeae2d9dbd2ec873e2'
v=j(o/'evidence-receipt.json');assert v['SourcePaths']==0 and v['CI291Passed']==6599 and v['Original291Passed']==5846 and v['Original291FdPassed']==8 and not v['LocalMainFullRepeated']
assert not git('diff','HEAD','--name-only','--','src','tests','tools','Erlang.Net.slnx','docs/supported-mfas.json').strip()
freeze=j(o/'published291-source-freeze.json');assert len(freeze['Files'])==21
for x in freeze['Files']:assert h((r/x['Path']).read_bytes())==x['Sha256']
proofs=[q.relative_to(r).as_posix() for q in sorted(o.rglob('*')) if q.is_file()]
assert set(proofs)=={'docs/validation/part-292/'+x['Path'] for x in v['Files']}|{'docs/validation/part-292/evidence-receipt.json'}
for x in v['Files']:
 b=(o/x['Path']).read_bytes();assert len(b)==x['Bytes'] and h(b)==x['Sha256']
metadata=['.gitattributes','AGENTS.md','CHANGELOG.md','docs/PROGRESS.md','docs/NEXT_STEPS.md','docs/COMPATIBILITY.md','docs/ERLANG_COMPATIBILITY.md','docs/DECISIONS.md','docs/changelog/0044.md','docs/module-readiness-scope.json']
changed=set(git('diff','HEAD','--name-only','-z').decode().split('\0'))-{''};assert changed==set(metadata),(changed-set(metadata),set(metadata)-changed)
expected=metadata+proofs;assert len(expected)==len(set(expected)) and not any('LICENSE.local' in x or '__pycache__' in x for x in expected)
assert all('docs/changelog/'+str(i).zfill(4)+'.md' not in expected for i in range(1,44))
assert h((r/'docs/changelog/0043.md').read_bytes())=='1246C10CA856935B004FA7ACB7E177E5E6A9B6F1250E32EBB893BE9739B6F0E1'
assert j(r/'docs/supported-mfas.json')==json.loads(git('show','HEAD:docs/supported-mfas.json'))
for name in ['module-readiness.json','MODULE_READINESS.md']:
 assert (r/'docs'/name).read_bytes()==(o/'reused-published291-readiness'/name).read_bytes()
pathspec=a/'root-part292-stage-pathspec';pathspec.write_bytes(b'\0'.join(x.encode() for x in expected)+b'\0')
subprocess.run(['git','add','--force','--pathspec-from-file='+str(pathspec),'--pathspec-file-nul'],cwd=r,check=True)
actual=set(git('diff','--cached','--name-only','-z').decode().split('\0'))-{''};assert actual==set(expected)
for path in proofs:assert git('show',':'+path)==(r/path).read_bytes(),path
for path in metadata:assert git('show',':'+path).replace(b'\r\n',b'\n')==(r/path).read_bytes().replace(b'\r\n',b'\n'),path
subprocess.run(['git','diff','--cached','--check'],cwd=r,check=True)
print(json.dumps({'StagedPaths':len(expected),'SourcePaths':0,'ProofFiles':len(proofs),'MetadataPaths':len(metadata),'Registry':502,'LicenseAndCacheExcluded':True}))
