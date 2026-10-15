"""Artifact proposal only. Root executes after terminal source/full/readiness gates."""
from pathlib import Path
import hashlib,json,re,subprocess
r=Path.cwd().resolve();a=r/'artifacts';o=r/'docs/validation/part-297';j=lambda p:json.loads(p.read_bytes());h=lambda b:hashlib.sha256(b).hexdigest().upper();git=lambda *x:subprocess.check_output(['git',*x],cwd=r)
parent='1d98eade866ff83ed3e1496169520fcf3d1e0c6b';assert git('rev-parse','HEAD').decode().strip()==parent
if o.exists():assert [q.relative_to(o).as_posix() for q in sorted(o.rglob('*')) if q.is_file()]==['tests.json'] and (o/'tests.json').read_bytes()==(a/'tests.json').read_bytes()
f=j(a/'root-part297-final-source-freeze.json');assert f['ParentHead']==parent and f['SourcePaths']==len(f['Files'])==16 and f['TokensExact'] and f['InitialNamedFocusGroupsPassed']==50 and not f['PublicLoadNifRegistered'] and not f['ActualNativeExecuted']
paths={x['Path'] for x in f['Files']};assert len(paths)==16
for x in f['Files']:
 b=(r/x['Path']).read_bytes();assert len(b)==x['Bytes'] and h(b)==x['Sha256']
assert set(git('diff','HEAD','--name-only','-z','--','src','tests','tools','Erlang.Net.slnx','.github/workflows').decode().split('\0'))-{''}<=paths
assert j(a/'root-part297-full-exit.json')['ExitCode']==0 and b'Validation passed' in (a/'root-part297-full-validation.log').read_bytes()
t=j(a/'tests.json');old=j(r/'docs/validation/part-296/tests.json');assert (t['Passed'],t['Failed'])==(6609,0)
names=[x['Name'] for x in t['Tests']];oldnames=[x['Name'] for x in old['Tests']];added='runtime/nif-beam-exact-image-entry-and-closure-routing'
assert len(names)==len(set(names))==6609 and len(oldnames)==len(set(oldnames))==6608 and all(x['Status']=='Passed' for x in t['Tests'])
assert [n for n in names if n in set(oldnames)]==oldnames and [n for n in names if n not in set(oldnames)]==[added]
assert j(a/'integration-results.json')==j(r/'docs/validation/part-296/integration-results.json') and len(j(a/'integration-results.json'))==11
registry=j(r/'docs/supported-mfas.json');assert len(registry)==502 and registry==json.loads(git('show','HEAD:docs/supported-mfas.json'))
ready=j(r/'docs/module-readiness.json');assert ready['LocalPassedTests']==6609 and ready['RegisteredMfas']==502 and ready['RegisteredMfaDelta']==0 and ready['ComparisonTestReport']=='validation/part-297/tests.json'
focuslog=(a/'root-nif-beam297-isolated/focus50-lambda-revised.log').read_text(encoding='utf-8-sig');focused=re.findall(r'^PASS ([^\s]+)\s*$',focuslog,re.M);assert len(focused)==len(set(focused))==50 and 'NifBeamEntryHostChecks' in focused
focusguard=j(a/'root-nif-beam297-isolated/focus50-guard.json');assert focusguard['Status']=='PASS' and focusguard['GroupCount']==50 and focusguard['Groups']==['PASS '+name for name in focused] and focusguard['InitialCountingGuardRejected']==43
assert j(a/'root-nif-beam297-isolated/tokens-before.json')==j(a/'root-nif-beam297-isolated/tokens-after.json')
assert paths==set(j(a/'root-nif-beam297-isolated/paths16.json'))
rev=j(a/'root-nif-beam297-isolated/host-constructor-type-revision.json');assert rev['InitialCompileErrors']==4 and not rev['ProductionChanged'] and rev['AssertionsRemoved']==0
ci=j(a/'pack296-ci-38710728913-independent-guard.json');assert ci['Head']==parent and ci['Status']=='PASS_EXACT_HEAD296_CI6608_FULL' and ci['Passed']==6608 and ci['Failed']==0 and ci['OrderedNamesExact'] and len(ci['Integration'])==11
prior=j(a/'pack295-original-38708186925-semantic-guard.json');assert prior['Status']=='PASS_EXACT_HEAD295_ORIGINAL5846_FD8_PREDECLARED_MAP_STDOUT_VARIANCE' and prior['Passed']==5846 and prior['Failed']==0 and prior['FdPassed']==8
assert not prior['Historical4903Module778Expression165DiagnosticRowsExact'] and prior['HistoricalSourceExpectedSdkGeneratedAndPoliciesExact'] and prior['AllHistoricalExpressionAndDiagnosticRowsExact'] and prior['PredeclaredNativeMapStdoutVarianceRows']==1 and prior['HistoricalRawDifferencesPreserved'] and not prior['RawNormalization'] and prior['SixExclusionsContextPolicyExact']
closed='114B40E0985AEF79089330BAA00B445D955423CC8C81DB00DE5D160D981EC4C0';assert h((r/'docs/changelog/0044.md').read_bytes())==closed
volume=r/'docs/changelog/0045.md';entry=(a/'part297-changelog-draft.md').read_text(encoding='utf-8-sig');combined=volume.read_bytes()+b'\r\n'+entry.replace('\r\n','\n').replace('\n','\r\n').encode();assert b'## Part 297' not in volume.read_bytes() and len(combined)<=32768 and len(combined.splitlines())<=400
index=r/'CHANGELOG.md';ib=index.read_bytes();label='parts295–296 (active)'.encode();assert ib.count(label)==1
metadata=['AGENTS.md','docs/PROGRESS.md','docs/NEXT_STEPS.md','docs/COMPATIBILITY.md','docs/ERLANG_COMPATIBILITY.md','docs/DECISIONS.md']
for name in metadata:
 b=(r/name).read_bytes();assert b'Part297:' not in b and re.search(br'\r?\n\r?\n',b)
attrs=r/'.gitattributes';assert b'docs/validation/part-297/** binary' not in attrs.read_bytes()
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
manifest(a/'pack296-ci-detached-staging-manifest.json');manifest(a/'pack295-original-detached-staging-manifest-for297.json')
ngpath=a/'pack296-original-38710732869-independent-guard.json';nmpath=a/'pack296-original-detached-staging-manifest.json';native_verified=False;native_raw_exact=False
if ngpath.exists() or nmpath.exists():
 assert ngpath.exists() and nmpath.exists();ng=j(ngpath);assert ng['Head']==parent and ng['Passed']==5846 and ng['Failed']==0 and ng['FdPassed']==8
 assert ng['Status']=='PASS_EXACT_HEAD296_ORIGINAL5846_AND_FD8' and ng['Historical4903Module778Expression165DiagnosticRowsExact'];manifest(nmpath);native_verified=True;native_raw_exact=True
for src in sorted(a.glob('root-part297-*')):
 if src.is_file():select(src,o/'root-artifacts'/src.name)
for name in ['part297-changelog-draft.md','part297-readiness-proposal.py','part297-finalize-proposal.py','part297-stage-guard-proposal.py','root-run297-full.ps1']:select(a/name,o/'root-artifacts'/name)
select(a/'root-guard297.py',o/'root-artifacts/root-guard297.py')
for base in ['nif-beam-entry297-overlay','beam-lambda-publication297-revision']:
 for src in sorted((a/base).iterdir()):
  if src.is_file() and src.suffix in ['.json','.md','.py','.txt']:select(src,o/'candidate-provenance'/base/src.name)
for src in sorted((a/'root-nif-beam297-isolated').iterdir()):
 if src.is_file() and src.suffix in ['.json','.log','.cs']:select(src,o/'isolated-revisions'/src.name)
for name in ['root-dispatch296-ordinary.ps1','root-part296-ordinary-dispatch-attempt.json','root-part296-ordinary-dispatch-result.json','pack296-read-two-runs.ps1']:
 if (a/name).is_file():select(a/name,o/'previous296-dispatch-and-reader'/name)
for src,dst in [(a/'tests.json',o/'tests.json'),(a/'integration-results.json',o/'integration-results.json'),(r/'docs/supported-mfas.json',o/'supported-mfas.json')]:select(src,dst)
for name in ['module-readiness.json','MODULE_READINESS.md']:select(r/'docs'/name,o/name)
for dst,(src,digest) in copies.items():
 b=src.read_bytes();assert h(b)==digest;dst.parent.mkdir(parents=True,exist_ok=True);dst.write_bytes(b)
volume.write_bytes(combined);index.write_bytes(ib.replace(label,'parts295–297 (active)'.encode()))
note='Part297: actual Main6609/full integration passes, all6608 historical ordered names plus one BEAM exact-image entry/closure HOST; source16/token exact and50 isolated groups. Stable prepared/standalone image owners and complete declared-lambda publication validation replace blanket FunT rejection. Managed fake bindings only; pending closure uses injected managed metadata, no original on_load-closure/native ABI/load_nif proof. Initial4CS8752 and lambda rejection/fixes preserved. Registry502/erlang344 of351 presence unchanged. Original2955846/fd8 semantic PASS has one PREDECLARED native mapstdout rawvariance, strict failure retained; never allrawexact. CI2966608/full archived; original296 state explicit in receipt. New same-head CI/ordinary pending; broader lifecycle/physical/upstream/performance incomplete.'
for name in metadata:
 q=r/name;b=q.read_bytes();cut=re.search(br'\r?\n\r?\n',b).end();nl=b'\r\n' if b'\r\n' in b else b'\n';q.write_bytes(b[:cut]+note.encode()+nl+nl+b[cut:])
attrs.write_bytes(attrs.read_bytes()+b'docs/validation/part-297/** binary\n');assert h((r/'docs/changelog/0044.md').read_bytes())==closed
receipt={'Status':'ACTUAL_MAIN6609_FULL_BEAM_IMAGE_ENTRY_AND_ALL_LAMBDA_VALIDATION_NEW_NATIVE_AND_CI_PENDING','ParentHead':parent,'SourcePaths':16,'Passed':6609,'Failed':0,'Historical6608OrderedNamesExact':True,'Added':[added],'Registry':502,'ErlangPresence':344,'ErlangReference':351,'IntegrationAndPackagesPassed':True,'InitialIsolatedNamedGroupsPassed':50,'InitialFixtureCompileErrorsRetained':4,'Original295Passed':5846,'Original295FdPassed':8,'Original295StrictRawHistoryEqual':False,'Original295PredeclaredNativeMapStdoutVarianceRows':1,'Original295RawNormalized':False,'CI296Passed':6608,'Original296Verified':native_verified,'Original296StrictRawHistoryEqual':native_raw_exact,'ActualNativeExecuted':False,'LoadNifRegistered':False,'HostBindingsAreManagedFakes':True,'PendingClosureMetadataInjected':True,'GenuineOriginalPendingClosureFixtureCompared':False,'SameHeadCI297Verified':False,'ChangedSourceOriginalVerified':False,'WholeCompatibility':False,'Closed0044Sha256':closed}
receipt['Files']=[{'Path':q.relative_to(o).as_posix(),'Bytes':q.stat().st_size,'Sha256':h(q.read_bytes())} for q in sorted(o.rglob('*')) if q.is_file()]
(o/'evidence-receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8');print(json.dumps({'Passed':6609,'SourcePaths':16,'Original296Verified':native_verified,'ProofFiles':len(receipt['Files']),'VolumeBytes':len(combined)}))
