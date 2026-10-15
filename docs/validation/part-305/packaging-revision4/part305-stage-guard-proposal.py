"""Root-only exact path/index guard proposal; author never executes."""
from pathlib import Path
import hashlib,json,subprocess
r=Path.cwd().resolve();a=r/'artifacts';o=r/'docs/validation/part-305';j=lambda p:json.loads(p.read_bytes());h=lambda b:hashlib.sha256(b).hexdigest().upper();git=lambda *x:subprocess.check_output(['git',*x],cwd=r)
assert git('rev-parse','HEAD').decode().strip()=='51994bed995f9dbf31b8b88b3381cad2642012e0'
v=j(o/'evidence-receipt.json');f=j(a/'root-part305-final-source-freeze3.json');assert (v['Passed'],v['Failed'],v['SourcePaths'])==(6624,0,32) and len(f['Files'])==32 and f['CSharpPaths']==26 and f['TokensExactForCSharp'] and v['IntegrationAndPackagesPassed']
assert v['ActualPostFullMainNativeWindowsPassed'] and v['Actual503RawExporterOutputPreserved'] and v['OnlyExistingProcessInfo1LimitsRestored'] and v['RegistryDelta']==0 and not v['PublicLoadNifActivated']

sources=[x['Path'] for x in f['Files']]
for x in f['Files']:
 b=(r/x['Path']).read_bytes();assert len(b)==x['Bytes'] and h(b)==x['Sha256']
proofs=[p.relative_to(r).as_posix() for p in sorted(o.rglob('*')) if p.is_file()]
assert set(proofs)=={'docs/validation/part-305/'+x['Path'] for x in v['Files']}|{'docs/validation/part-305/evidence-receipt.json'}
for x in v['Files']:
 b=(o/x['Path']).read_bytes();assert len(b)==x['Bytes'] and h(b)==x['Sha256']
metadata=['.gitattributes','AGENTS.md','CHANGELOG.md','docs/PROGRESS.md','docs/NEXT_STEPS.md','docs/COMPATIBILITY.md','docs/ERLANG_COMPATIBILITY.md','docs/DECISIONS.md','docs/changelog/0046.md','docs/module-readiness-scope.json','docs/module-readiness.json','docs/MODULE_READINESS.md','docs/supported-mfas.json']
changed=set(git('diff','HEAD','--name-only','-z').decode().split('\0'))-{''};assert changed<=set(sources)|set(metadata)|set(proofs),sorted(changed-set(sources)-set(metadata)-set(proofs))
expected=sources+[p for p in metadata if p in changed]+proofs;assert len(expected)==len(set(expected)) and not any('LICENSE.local' in p or '__pycache__' in p for p in expected)
assert all('docs/changelog/'+str(i).zfill(4)+'.md' not in expected for i in range(1,46))
assert h((r/'docs/changelog/0045.md').read_bytes())=='9EB82D1D39B01ABB618DFD3698D656E0ADE6F3511506604F33038C9A3B381CB6'
raw=j(o/'supported-mfas-raw-export.json');derived=j(r/'docs/supported-mfas.json');old=json.loads(git('show','HEAD:docs/supported-mfas.json'));key=lambda row:(row['Module'],row['Function'],row['Arity']);new=('erlang','process_info',1)
assert len(raw)==len(derived)==503 and derived==old and [key(row) for row in raw]==[key(row) for row in derived]
for before,after in zip(raw,derived):
 assert (before==after if key(before)!=new else {k:v for k,v in before.items() if k!='Limits'}=={k:v for k,v in after.items() if k!='Limits'})
pathspec=a/'root-part305-stage-pathspec';pathspec.write_bytes(b'\0'.join(p.encode() for p in expected)+b'\0')
subprocess.run(['git','add','--force','--pathspec-from-file='+str(pathspec),'--pathspec-file-nul'],cwd=r,check=True)
actual=set(git('diff','--cached','--name-only','-z').decode().split('\0'))-{''};assert actual==set(expected),(actual-set(expected),set(expected)-actual)
for p in proofs:assert git('show',':'+p)==(r/p).read_bytes(),p
for p in sources+[x for x in metadata if x in changed]:
 binary_exact=p.endswith(('.cs','.beam','.etf')) or '/native/' in p or '/original/' in p or '/Resources/RawOnLoadDeferredExports/' in p
 staged=git('show',':'+p);working=(r/p).read_bytes()
 assert (staged==working if binary_exact else staged.replace(b'\r\n',b'\n')==working.replace(b'\r\n',b'\n')),p
subprocess.run(['git','diff','--cached','--check'],cwd=r,check=True);print(json.dumps({'Passed':6624,'SourcePaths':32,'ProofFiles':len(proofs),'StagedPaths':len(expected),'Closed0045Exact':True,'Raw503ExportAndDerivedSubsetLimitsVerified':True,'LicenseAndCacheExcluded':True}))
