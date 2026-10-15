"""Root-only exact path/index guard proposal; author never executes."""
from pathlib import Path
import hashlib,json,subprocess
r=Path.cwd().resolve();a=r/'artifacts';o=r/'docs/validation/part-299';j=lambda p:json.loads(p.read_bytes());h=lambda b:hashlib.sha256(b).hexdigest().upper();git=lambda *x:subprocess.check_output(['git',*x],cwd=r)
assert git('rev-parse','HEAD').decode().strip()=='e037631759438f944eda934005432ca04fb516e5'
v=j(o/'evidence-receipt.json');f=j(a/'root-part299-final-source-freeze.json');assert (v['Passed'],v['Failed'],v['SourcePaths'])==(6610,0,55) and len(f['Files'])==55 and f['CSharpPaths']==31 and f['TokensExactForCSharp'] and v['IntegrationAndPackagesPassed']
assert v['ActualPostFullMainNativeWindowsPassed'] and v['ActualGeneratedCSharpRoutePassed'] and not v['PublicLoadNifRegistered'] and not v['GeneralNifAbiComplete'] and not v['LinuxNativeSdkExecuted'] and not v['Original13Executed']
sources=[x['Path'] for x in f['Files']]
for x in f['Files']:
 b=(r/x['Path']).read_bytes();assert len(b)==x['Bytes'] and h(b)==x['Sha256']
proofs=[p.relative_to(r).as_posix() for p in sorted(o.rglob('*')) if p.is_file()]
assert set(proofs)=={'docs/validation/part-299/'+x['Path'] for x in v['Files']}|{'docs/validation/part-299/evidence-receipt.json'}
for x in v['Files']:
 b=(o/x['Path']).read_bytes();assert len(b)==x['Bytes'] and h(b)==x['Sha256']
metadata=['.gitattributes','AGENTS.md','CHANGELOG.md','docs/PROGRESS.md','docs/NEXT_STEPS.md','docs/COMPATIBILITY.md','docs/ERLANG_COMPATIBILITY.md','docs/DECISIONS.md','docs/changelog/0045.md','docs/module-readiness-scope.json','docs/module-readiness.json','docs/MODULE_READINESS.md']
changed=set(git('diff','HEAD','--name-only','-z').decode().split('\0'))-{''};assert changed<=set(sources)|set(metadata)|set(proofs),sorted(changed-set(sources)-set(metadata)-set(proofs))
expected=sources+[p for p in metadata if p in changed]+proofs;assert len(expected)==len(set(expected)) and not any('LICENSE.local' in p or '__pycache__' in p for p in expected)
assert all('docs/changelog/'+str(i).zfill(4)+'.md' not in expected for i in range(1,45))
assert h((r/'docs/changelog/0044.md').read_bytes())=='114B40E0985AEF79089330BAA00B445D955423CC8C81DB00DE5D160D981EC4C0'
assert j(r/'docs/supported-mfas.json')==json.loads(git('show','HEAD:docs/supported-mfas.json')) and len(j(r/'docs/supported-mfas.json'))==502
pathspec=a/'root-part299-stage-pathspec';pathspec.write_bytes(b'\0'.join(p.encode() for p in expected)+b'\0')
subprocess.run(['git','add','--force','--pathspec-from-file='+str(pathspec),'--pathspec-file-nul'],cwd=r,check=True)
actual=set(git('diff','--cached','--name-only','-z').decode().split('\0'))-{''};assert actual==set(expected),(actual-set(expected),set(expected)-actual)
for p in proofs:assert git('show',':'+p)==(r/p).read_bytes(),p
for p in sources+[x for x in metadata if x in changed]:
 binary_exact=p.endswith('.cs') or '/native/' in p or '/original/' in p
 staged=git('show',':'+p);working=(r/p).read_bytes()
 assert (staged==working if binary_exact else staged.replace(b'\r\n',b'\n')==working.replace(b'\r\n',b'\n')),p
subprocess.run(['git','diff','--cached','--check'],cwd=r,check=True);print(json.dumps({'Passed':6610,'SourcePaths':55,'ProofFiles':len(proofs),'StagedPaths':len(expected),'Closed0044Exact':True,'Original297ExactHistoricalRows':True,'LicenseAndCacheExcluded':True}))
