"""Root-only proposal. Every archive/metadata write follows terminal actual gates."""
from pathlib import Path
import hashlib,json,re,subprocess
r=Path.cwd().resolve();a=r/'artifacts';o=r/'docs/validation/part-298';j=lambda p:json.loads(p.read_bytes());h=lambda b:hashlib.sha256(b).hexdigest().upper();git=lambda *x:subprocess.check_output(['git',*x],cwd=r)
parent='26e158fc1440002b9269896d0ba1a9cf5b1bbdd5';assert git('rev-parse','HEAD').decode().strip()==parent
if o.exists():assert [q.relative_to(o).as_posix() for q in sorted(o.rglob('*')) if q.is_file()]==['tests.json'] and (o/'tests.json').read_bytes()==(a/'tests.json').read_bytes()
f=j(a/'root-part298-final-source-freeze.json');assert f['ParentHead']==parent and f['SourcePaths']==len(f['Files'])==6 and f['TokensExact'] and not f['PublicLoadNifRegistered'] and not f['ActualNativeExecuted']
paths={x['Path'] for x in f['Files']};assert paths==set(j(a/'root-nif-inventory298-isolated/paths6.json'))
for x in f['Files']:
 b=(r/x['Path']).read_bytes();assert len(b)==x['Bytes'] and h(b)==x['Sha256']
assert set(git('diff','HEAD','--name-only','-z','--','src','tests','tools','Erlang.Net.slnx','.github/workflows').decode().split('\0'))-{''}<=paths
assert j(a/'root-part298-full-exit.json')['ExitCode']==0 and b'Validation passed' in (a/'root-part298-full-validation.log').read_bytes()
t=j(a/'tests.json');old=j(r/'docs/validation/part-297/tests.json');assert (t['Passed'],t['Failed'])==(6610,0)
names=[x['Name'] for x in t['Tests']];oldnames=[x['Name'] for x in old['Tests']];added='runtime/nif-installed-current-image-inventory-and-cache'
assert len(names)==len(set(names))==6610 and len(oldnames)==len(set(oldnames))==6609 and all(x['Status']=='Passed' for x in t['Tests'])
assert [n for n in names if n in set(oldnames)]==oldnames and [n for n in names if n not in set(oldnames)]==[added]
assert j(a/'integration-results.json')==j(r/'docs/validation/part-297/integration-results.json') and len(j(a/'integration-results.json'))==11
registry=j(r/'docs/supported-mfas.json');assert len(registry)==502 and registry==json.loads(git('show','HEAD:docs/supported-mfas.json'))
ready=j(r/'docs/module-readiness.json');assert ready['LocalPassedTests']==6610 and ready['RegisteredMfas']==502 and ready['RegisteredMfaDelta']==0 and ready['ComparisonTestReport']=='validation/part-298/tests.json'
isolated=a/'root-nif-inventory298-isolated';assert j(isolated/'initial-target-exit.json')['ExitCode']==0 and j(isolated/'focus51-exit.json')['ExitCode']==0 and j(isolated/'focus51-exit.json')['ExpectedGroups']==51
focused=re.findall(r'^PASS ([^\s]+)\s*$',(isolated/'focus51.log').read_text(encoding='utf-8-sig'),re.M);assert len(focused)==len(set(focused))==51 and 'NifInventoryHostChecks' in focused
assert j(isolated/'tokens-before.json')==j(isolated/'tokens-after.json')
format_guard=j(isolated/'format-guard.json');assert format_guard['Status']=='PASS' and format_guard['SourcePaths']==6 and format_guard['NamedFocusGroups']==51 and format_guard['TokensExact']
prior=j(a/'pack296-original-38710732869-semantic-guard.json');assert prior['Status']=='PASS_EXACT_HEAD296_ORIGINAL5846_FD8_PREDECLARED_MAP_STDOUT_VARIANCE' and prior['Passed']==5846 and prior['Failed']==0 and prior['FdPassed']==8
assert not prior['Historical4903Module778Expression165DiagnosticRowsExact'] and prior['HistoricalSourceExpectedSdkGeneratedAndPoliciesExact'] and prior['AllHistoricalExpressionAndDiagnosticRowsExact'] and prior['PredeclaredNativeMapStdoutVarianceRows']==1 and prior['HistoricalRawDifferencesPreserved'] and not prior['RawNormalization'] and prior['SixExclusionsContextPolicyExact']
closed='114B40E0985AEF79089330BAA00B445D955423CC8C81DB00DE5D160D981EC4C0';assert h((r/'docs/changelog/0044.md').read_bytes())==closed
volume=r/'docs/changelog/0045.md';entry=(a/'part298-changelog-draft.md').read_text(encoding='utf-8-sig');combined=volume.read_bytes()+b'\r\n'+entry.replace('\r\n','\n').replace('\n','\r\n').encode();assert b'## Part 298' not in volume.read_bytes() and len(combined)<=32768 and len(combined.splitlines())<=400
index=r/'CHANGELOG.md';ib=index.read_bytes();label='parts295–297 (active)'.encode();assert ib.count(label)==1
metadata=['AGENTS.md','docs/PROGRESS.md','docs/NEXT_STEPS.md','docs/COMPATIBILITY.md','docs/ERLANG_COMPATIBILITY.md','docs/DECISIONS.md']
for name in metadata:
 b=(r/name).read_bytes();assert b'Part298:' not in b and re.search(br'\r?\n\r?\n',b)
attrs=r/'.gitattributes';assert b'docs/validation/part-298/** binary' not in attrs.read_bytes()
copies={}
def select(src,dst,expected=None,size=None):
 src=src.resolve();dst=dst.resolve();assert src.is_file() and dst.is_relative_to(o) and '__pycache__' not in src.parts and 'LICENSE.local.txt' not in src.parts
 b=src.read_bytes()
 if expected:assert h(b)==expected
 if size is not None:assert len(b)==size
 if dst in copies:assert copies[dst][1]==h(b)
 copies[dst]=(src,h(b))
def manifest(mp):
 for x in j(mp):select(r/x['SourcePath'],r/x['Destination'],x['Sha256'],x['Bytes'])
 select(mp,o/'staging-manifests'/mp.name)
manifest(a/'pack296-original-detached-staging-manifest-for298.json')
ci_verified=False;ci_path=a/'pack297-ci-38714602504-independent-guard.json';ci_manifest=a/'pack297-ci-detached-staging-manifest.json'
if ci_path.exists() or ci_manifest.exists():
 assert ci_path.exists() and ci_manifest.exists();ci=j(ci_path);assert ci['Head']==parent and ci['Status']=='PASS_EXACT_HEAD297_CI6609_FULL' and ci['Passed']==6609 and ci['Failed']==0 and ci['OrderedNamesExact'] and len(ci['Integration'])==11;manifest(ci_manifest);ci_verified=True
ngpaths=[p for p in [a/'pack297-original-38714645985-independent-guard.json',a/'pack297-original-38714645985-semantic-guard.json'] if p.exists()];nmpath=a/'pack297-original-detached-staging-manifest.json';native_verified=False;native_raw_equal=False;native_variances=0
if ngpaths or nmpath.exists():
 assert len(ngpaths)==1 and nmpath.exists();ng=j(ngpaths[0]);assert ng['Head']==parent and ng['Passed']==5846 and ng['Failed']==0 and ng['FdPassed']==8
 assert ng['Status'] in ['PASS_EXACT_HEAD297_ORIGINAL5846_AND_FD8','PASS_EXACT_HEAD297_ORIGINAL5846_FD8_PREDECLARED_MAP_STDOUT_VARIANCE']
 native_raw_equal=ng['Historical4903Module778Expression165DiagnosticRowsExact']
 if not native_raw_equal:assert ng['PredeclaredNativeMapStdoutVarianceRows']==1 and not ng['RawNormalization'] and ng['HistoricalSourceExpectedSdkGeneratedAndPoliciesExact'];native_variances=1
 manifest(nmpath);native_verified=True
for src in sorted(a.glob('root-part298-*')):
 if src.is_file():select(src,o/'root-artifacts'/src.name)
for name in ['part298-changelog-draft.md','part298-readiness-proposal.py','part298-finalize-proposal.py','part298-stage-guard-proposal.py','root-run298-full.ps1','root-guard298.py','root-previous296-for298-guard.py','root-previous297-ci-for298-guard.py']:select(a/name,o/'root-artifacts'/name)
for src in sorted((a/'nif-inventory298-overlay').iterdir()):
 if src.is_file() and src.suffix in ['.json','.md','.py','.txt']:select(src,o/'candidate-provenance'/src.name)
for src in sorted(isolated.iterdir()):
 if src.is_file() and src.suffix in ['.json','.log','.cs']:select(src,o/'isolated-records'/src.name)
for name in ['root-dispatch297-ordinary.ps1','root-part297-ordinary-dispatch-attempt.json','root-part297-ordinary-dispatch-result.json','pack297-read-two-runs.ps1']:
 if (a/name).is_file():select(a/name,o/'previous297-dispatch-and-reader'/name)
for src,dst in [(a/'tests.json',o/'tests.json'),(a/'integration-results.json',o/'integration-results.json'),(r/'docs/supported-mfas.json',o/'supported-mfas.json')]:select(src,dst)
for name in ['module-readiness.json','MODULE_READINESS.md']:select(r/'docs'/name,o/name)
for dst,(src,digest) in copies.items():
 b=src.read_bytes();assert h(b)==digest;dst.parent.mkdir(parents=True,exist_ok=True);dst.write_bytes(b)
volume.write_bytes(combined);index.write_bytes(ib.replace(label,'parts295–298 (active)'.encode()))
note='Part298: actual Main6610/full integration passes, all6609 historical ordered names plus one installed-current-image inventory/cache HOST; source6/token exact and51 isolated groups. Ordered immutable installed inventory publishes atomically with bindings, current-only lookup and identity-based metadata invalidation preserve pending/old isolation. Managed fake owners retire explicitly; no public load_nif/native ABI/taints/nif_version or automatic lifecycle claim. Registry502/erlang344 of351 presence unchanged. Original2965846/fd8 semantic PASS retains1predeclared raw mapstdoutvariance andstrictfailure, no normalization. Prior297 CI/original state explicit in receipt; new same-head checks pending. Physical/upstream/performance/full module remain incomplete.'
for name in metadata:
 q=r/name;b=q.read_bytes();cut=re.search(br'\r?\n\r?\n',b).end();nl=b'\r\n' if b'\r\n' in b else b'\n';q.write_bytes(b[:cut]+note.encode()+nl+nl+b[cut:])
attrs.write_bytes(attrs.read_bytes()+b'docs/validation/part-298/** binary\n');assert h((r/'docs/changelog/0044.md').read_bytes())==closed
receipt={'Status':'ACTUAL_MAIN6610_FULL_CURRENT_IMAGE_INVENTORY_MANAGED_FOUNDATION_NEW_NATIVE_AND_CI_PENDING','ParentHead':parent,'SourcePaths':6,'Passed':6610,'Failed':0,'Historical6609OrderedNamesExact':True,'Added':[added],'Registry':502,'ErlangPresence':344,'ErlangReference':351,'IntegrationAndPackagesPassed':True,'InitialIsolatedNamedGroupsPassed':51,'Original296Passed':5846,'Original296FdPassed':8,'Original296StrictRawHistoryEqual':False,'Original296PredeclaredNativeMapStdoutVarianceRows':1,'Original296RawNormalized':False,'CI297Verified':ci_verified,'Original297Verified':native_verified,'Original297StrictRawHistoryEqual':native_raw_equal,'Original297PredeclaredNativeStdoutVarianceRows':native_variances,'ActualNativeExecuted':False,'LoadNifRegistered':False,'HostOwnersAreManagedFakes':True,'AutomaticNativeOwnerRetirementImplemented':False,'TaintsOrNifVersionImplemented':False,'SameHeadCI298Verified':False,'ChangedSourceOriginalVerified':False,'WholeCompatibility':False,'Closed0044Sha256':closed}
receipt['Files']=[{'Path':q.relative_to(o).as_posix(),'Bytes':q.stat().st_size,'Sha256':h(q.read_bytes())} for q in sorted(o.rglob('*')) if q.is_file()]
(o/'evidence-receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8');print(json.dumps({'Passed':6610,'SourcePaths':6,'CI297Verified':ci_verified,'Original297Verified':native_verified,'ProofFiles':len(receipt['Files']),'VolumeBytes':len(combined)}))
