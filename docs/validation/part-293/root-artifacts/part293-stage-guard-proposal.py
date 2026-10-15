"""Root-reviewed proposal only. No execution by proposal author."""
from pathlib import Path
import hashlib,json,subprocess
r=Path.cwd().resolve();a=r/'artifacts';o=r/'docs/validation/part-293';h=lambda b:hashlib.sha256(b).hexdigest().upper();j=lambda p:json.loads(p.read_bytes());git=lambda *x:subprocess.check_output(['git',*x],cwd=r)
assert git('rev-parse','HEAD').decode().strip()=='a527bb63924c2e2106d2c774c42523436d61384e'
v=j(o/'evidence-receipt.json');f=j(a/'root-part293-final-source-freeze.json');assert v['Passed']==6603 and v['Failed']==0 and v['SourcePaths']==len(f['Files'])==37 and v['Historical6599OrderedNamesExact'] and v['IntegrationAndPackagesPassed']
assert not v['NewPressureCollectorIncluded'] and not v['ChangedRuntimeOriginalCompared']
sources=[x['Path'] for x in f['Files']]
for x in f['Files']:
 b=(r/x['Path']).read_bytes();assert len(b)==x['Bytes'] and h(b)==x['Sha256']
proofs=[p.relative_to(r).as_posix() for p in sorted(o.rglob('*')) if p.is_file()]
assert set(proofs)=={'docs/validation/part-293/'+x['Path'] for x in v['Files']}|{'docs/validation/part-293/evidence-receipt.json'}
for x in v['Files']:
 b=(o/x['Path']).read_bytes();assert len(b)==x['Bytes'] and h(b)==x['Sha256']
metadata=['.gitattributes','AGENTS.md','CHANGELOG.md','docs/PROGRESS.md','docs/NEXT_STEPS.md','docs/COMPATIBILITY.md','docs/ERLANG_COMPATIBILITY.md','docs/DECISIONS.md','docs/changelog/0044.md','docs/module-readiness-scope.json','docs/module-readiness.json','docs/MODULE_READINESS.md']
changed=set(git('diff','HEAD','--name-only','-z').decode().split('\0'))-{''}
assert changed<=set(sources)|set(metadata)|set(proofs),('Unexpected tracked changes',sorted(changed-set(sources)-set(metadata)-set(proofs)))
expected=sources+[p for p in metadata if p in changed]+proofs
assert len(expected)==len(set(expected)) and not any('LICENSE.local' in p or '__pycache__' in p for p in expected)
assert all('docs/changelog/'+str(i).zfill(4)+'.md' not in expected for i in range(1,44))
assert h((r/'docs/changelog/0043.md').read_bytes())=='1246C10CA856935B004FA7ACB7E177E5E6A9B6F1250E32EBB893BE9739B6F0E1'
assert j(r/'docs/supported-mfas.json')==json.loads(git('show','HEAD:docs/supported-mfas.json')) and len(j(r/'docs/supported-mfas.json'))==502
ready=j(r/'docs/module-readiness.json');assert ready['LocalPassedTests']==6603 and ready['RegisteredMfas']==502 and ready['RegisteredMfaDelta']==0
pathspec=a/'root-part293-stage-pathspec';pathspec.write_bytes(b'\0'.join(p.encode() for p in expected)+b'\0')
subprocess.run(['git','add','--force','--pathspec-from-file='+str(pathspec),'--pathspec-file-nul'],cwd=r,check=True)
actual=set(git('diff','--cached','--name-only','-z').decode().split('\0'))-{''};assert actual==set(expected),(actual-set(expected),set(expected)-actual)
for p in proofs:assert git('show',':'+p)==(r/p).read_bytes(),p
for p in sources+[x for x in metadata if x in changed]:assert git('show',':'+p).replace(b'\r\n',b'\n')==(r/p).read_bytes().replace(b'\r\n',b'\n'),p
subprocess.run(['git','diff','--cached','--check'],cwd=r,check=True)
print(json.dumps({'StagedPaths':len(expected),'SourcePaths':37,'ProofFiles':len(proofs),'Passed':6603,'Registry':502,'LicenseAndCacheExcluded':True}))
