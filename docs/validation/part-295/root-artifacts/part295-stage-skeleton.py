"""Root-only exact staging skeleton; author never executes."""
from pathlib import Path
import hashlib,json,subprocess
r=Path.cwd().resolve();a=r/'artifacts';o=r/'docs/validation/part-295';j=lambda p:json.loads(p.read_bytes());h=lambda b:hashlib.sha256(b).hexdigest().upper();git=lambda *x:subprocess.check_output(['git',*x],cwd=r)
assert git('rev-parse','HEAD').decode().strip()=='200ef8550d0b533f2a59eb872ac3949179994d7c'
v=j(o/'evidence-receipt.json');f=j(a/'root-part295-final-source-freeze.json');assert v['Passed']>6603 and v['Failed']==0 and v['SourcePaths']==len(f['Files']) and f['TokensExact'] and v['IntegrationAndPackagesPassed']
sources=[x['Path'] for x in f['Files']]
for x in f['Files']:
 b=(r/x['Path']).read_bytes();assert len(b)==x['Bytes'] and h(b)==x['Sha256']
proofs=[q.relative_to(r).as_posix() for q in sorted(o.rglob('*')) if q.is_file()]
assert set(proofs)=={'docs/validation/part-295/'+x['Path'] for x in v['Files']}|{'docs/validation/part-295/evidence-receipt.json'}
for x in v['Files']:
 b=(o/x['Path']).read_bytes();assert len(b)==x['Bytes'] and h(b)==x['Sha256']
metadata=['.gitattributes','AGENTS.md','CHANGELOG.md','docs/PROGRESS.md','docs/NEXT_STEPS.md','docs/COMPATIBILITY.md','docs/ERLANG_COMPATIBILITY.md','docs/DECISIONS.md','docs/changelog/0045.md','docs/module-readiness-scope.json','docs/module-readiness.json','docs/MODULE_READINESS.md']
changed=set(git('diff','HEAD','--name-only','-z').decode().split('\0'))-{''};assert changed<=set(sources)|set(metadata)|set(proofs),sorted(changed-set(sources)-set(metadata)-set(proofs))
expected=sources+[x for x in metadata if x in changed or x=='docs/changelog/0045.md']+proofs;assert len(expected)==len(set(expected)) and not any('LICENSE.local' in x or '__pycache__' in x for x in expected)
assert all('docs/changelog/'+str(i).zfill(4)+'.md' not in expected for i in range(1,45))
assert h((r/'docs/changelog/0044.md').read_bytes())=='114B40E0985AEF79089330BAA00B445D955423CC8C81DB00DE5D160D981EC4C0'
assert j(r/'docs/supported-mfas.json')==json.loads(git('show','HEAD:docs/supported-mfas.json')) and len(j(r/'docs/supported-mfas.json'))==502
pathspec=a/'root-part295-stage-pathspec';pathspec.write_bytes(b'\0'.join(x.encode() for x in expected)+b'\0')
subprocess.run(['git','add','--force','--pathspec-from-file='+str(pathspec),'--pathspec-file-nul'],cwd=r,check=True)
actual=set(git('diff','--cached','--name-only','-z').decode().split('\0'))-{''};assert actual==set(expected),(actual-set(expected),set(expected)-actual)
for x in proofs:assert git('show',':'+x)==(r/x).read_bytes(),x
for x in sources+[p for p in metadata if p in changed]:assert git('show',':'+x).replace(b'\r\n',b'\n')==(r/x).read_bytes().replace(b'\r\n',b'\n'),x
subprocess.run(['git','diff','--cached','--check'],cwd=r,check=True);print(json.dumps({'Passed':v['Passed'],'SourcePaths':len(sources),'ProofFiles':len(proofs),'StagedPaths':len(expected),'Closed0044Exact':True,'LicenseAndCacheExcluded':True}))
