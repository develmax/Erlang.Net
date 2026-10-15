"""Root-reviewed proposal only; actual Main gates precede every metadata/archive write."""
from pathlib import Path
import hashlib,json,re,subprocess
r=Path.cwd().resolve();a=r/'artifacts';o=r/'docs/validation/part-296';j=lambda p:json.loads(p.read_bytes());h=lambda b:hashlib.sha256(b).hexdigest().upper();git=lambda *x:subprocess.check_output(['git',*x],cwd=r)
parent='1095b1e04c863f19647ae467feeb0799b6721d3c';assert git('rev-parse','HEAD').decode().strip()==parent
if o.exists():assert [q.relative_to(o).as_posix() for q in sorted(o.rglob('*')) if q.is_file()]==['tests.json'] and (o/'tests.json').read_bytes()==(a/'tests.json').read_bytes()
f=j(a/'root-part296-final-source-freeze.json');assert f['ParentHead']==parent and f['SourcePaths']==len(f['Files'])==26 and f['TokensExact'] and not f['PublicLoadNifRegistered'] and not f['ActualNativeExecuted']
paths={x['Path'] for x in f['Files']};assert paths==set(j(a/'root-part296-source-paths.json'))
for x in f['Files']:
 b=(r/x['Path']).read_bytes();assert len(b)==x['Bytes'] and h(b)==x['Sha256']
assert set(git('diff','HEAD','--name-only','-z','--','src','tests','tools','Erlang.Net.slnx','.github/workflows').decode().split('\0'))-{''}<=paths
assert j(a/'root-part296-full-exit.json')['ExitCode']==0 and b'Validation passed' in (a/'root-part296-full-validation.log').read_bytes()
completion=j(a/'root-part296-main-completion-guard.json');assert completion['Status']=='PASS_ACTUAL_MAIN6608_FULL_SOURCE26' and completion['Passed']==6608 and completion['Failed']==0 and completion['SourcePaths']==26 and completion['TokensExact'] and completion['Historical6606OrderedNamesExact'] and completion['IntegrationAndPackagesPassed']
assert j(a/'root-part296-tokens-before.json')==j(a/'root-part296-tokens-after.json')
t=j(a/'tests.json');old=j(r/'docs/validation/part-295/tests.json');assert (t['Passed'],t['Failed'])==(6608,0)
names=[x['Name'] for x in t['Tests']];oldnames=[x['Name'] for x in old['Tests']]
assert len(names)==len(set(names))==6608 and len(oldnames)==len(set(oldnames))==6606 and all(x['Status']=='Passed' for x in t['Tests'])
added={'runtime/nif-caller-image-reservation-generation-and-cleanup','runtime/nif-source-named-entry-routing-and-owned-drain'}
assert [n for n in names if n in set(oldnames)]==oldnames and {n for n in names if n not in set(oldnames)}==added
assert j(a/'integration-results.json')==j(r/'docs/validation/part-295/integration-results.json') and len(j(a/'integration-results.json'))==11
registry=j(r/'docs/supported-mfas.json');assert len(registry)==502 and registry==json.loads(git('show','HEAD:docs/supported-mfas.json'))
ready=j(r/'docs/module-readiness.json');assert ready['LocalPassedTests']==6608 and ready['RegisteredMfas']==502 and ready['RegisteredMfaDelta']==0 and ready['ComparisonTestReport']=='validation/part-296/tests.json'
focus=j(a/'root-part296-isolated-focus-guard.json');assert focus['FocusedGroupsPassed']==len(focus['Names'])==40 and not focus['NativeExecuted'] and focus['AfterInitialTestParameterFix'] and focus['BeforeConstantRelocationAndFormatting']
focuslog=(a/'root-nif-integration296-isolated/focus40-revised.log').read_text(encoding='utf-8-sig');assert all(re.search(r'^PASS '+re.escape(n)+r'\s*$',focuslog,re.M) for n in focus['Names'])
revision=j(a/'root-nif-integration296-isolated/source-entry-publish-fixture-revision.json');assert not revision['ProductionChanged'] and revision['HistoricalAssertionsRemoved']==0
ci=j(a/'pack295-ci-38708183633-independent-guard.json');assert ci['Head']==parent and ci['Status']=='PASS_EXACT_HEAD295_CI6606_FULL' and ci['Passed']==6606 and ci['Failed']==0 and ci['OrderedNamesExact'] and len(ci['Integration'])==11
closed44='114B40E0985AEF79089330BAA00B445D955423CC8C81DB00DE5D160D981EC4C0';assert h((r/'docs/changelog/0044.md').read_bytes())==closed44
volume=r/'docs/changelog/0045.md';entry=(a/'part296-changelog-draft.md').read_text(encoding='utf-8-sig');combined=volume.read_bytes()+b'\r\n'+entry.replace('\r\n','\n').replace('\n','\r\n').encode();assert b'## Part 296' not in volume.read_bytes() and len(combined)<=32768 and len(combined.splitlines())<=400
index=r/'CHANGELOG.md';ib=index.read_bytes();label='part295 (active)'.encode();assert ib.count(label)==1
metadata=['AGENTS.md','docs/PROGRESS.md','docs/NEXT_STEPS.md','docs/COMPATIBILITY.md','docs/ERLANG_COMPATIBILITY.md','docs/DECISIONS.md']
for name in metadata:
 b=(r/name).read_bytes();assert b'Part296:' not in b and re.search(br'\r?\n\r?\n',b)
attrs=r/'.gitattributes';assert b'docs/validation/part-296/** binary' not in attrs.read_bytes()
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
manifest(a/'pack295-ci-detached-staging-manifest.json')
native_guard=a/'pack295-original-38708186925-independent-guard.json';native_manifest=a/'pack295-original-detached-staging-manifest.json';native_verified=False
if native_guard.exists() or native_manifest.exists():
 assert native_guard.exists() and native_manifest.exists();ng=j(native_guard);assert ng['Head']==parent and ng['Status']=='PASS_EXACT_HEAD295_ORIGINAL5846_AND_FD8' and ng['Passed']==5846 and ng['Failed']==0 and ng['FdPassed']==8 and ng['Historical4903Module778Expression165DiagnosticRowsExact']
 manifest(native_manifest);native_verified=True
for src in sorted(a.glob('root-part296-*')):
 if src.is_file():select(src,o/'root-artifacts'/src.name)
for name in ['part296-changelog-draft.md','part296-readiness-proposal.py','part296-finalize-proposal.py','part296-stage-guard-proposal.py','root-run296-full.ps1']:select(a/name,o/'root-artifacts'/name)
for name in ['root-dispatch295-ordinary.ps1','root-part295-ordinary-dispatch-attempt.json','root-part295-ordinary-dispatch-result.json','pack295-read-two-runs.ps1','pack295-read-ci-only.ps1']:select(a/name,o/'previous295-dispatch-and-readers'/name)
for base in ['nif-image-admission-candidate1','nif-source-entry296-overlay']:
 for src in sorted((a/base).iterdir()):
  if src.is_file() and src.suffix in ['.json','.md','.py','.txt']:select(src,o/'candidate-provenance'/base/src.name)
for src in sorted((a/'root-nif-integration296-isolated').iterdir()):
 if src.is_file() and src.suffix in ['.log','.json','.cs']:select(src,o/'isolated-revisions'/src.name)
for src,dst in [(a/'tests.json',o/'tests.json'),(a/'integration-results.json',o/'integration-results.json'),(r/'docs/supported-mfas.json',o/'supported-mfas.json')]:select(src,dst)
for name in ['module-readiness.json','MODULE_READINESS.md']:select(r/'docs'/name,o/name)
for dst,(src,digest) in copies.items():
 b=src.read_bytes();assert h(b)==digest;dst.parent.mkdir(parents=True,exist_ok=True);dst.write_bytes(b)
volume.write_bytes(combined);index.write_bytes(ib.replace(label,'parts295–296 (active)'.encode()))
note='Part296: actual Main6608/full integration passes, all6606 historical ordered names plus2 HOSTs, source26/token exact after semantic constant relocation. Exact caller-image reservation/generation cleanup and named source-entry managed binding routes are internal prerequisites; Runtime friends real Compiler only, no public test hook or activated native ABI/load_nif. Initial parameter-count fixture failure/revision and40 isolated groups retained. Registry502/erlang344 of351 presence unchanged. Prior295CI6606/full archived; original295 state is explicit in receipt. New same-head CI and changed-source ordinary comparison pending. Native lifecycle/BEAM entry/physical/upstream/performance remain incomplete.'
for name in metadata:
 q=r/name;b=q.read_bytes();cut=re.search(br'\r?\n\r?\n',b).end();nl=b'\r\n' if b'\r\n' in b else b'\n';q.write_bytes(b[:cut]+note.encode()+nl+nl+b[cut:])
attrs.write_bytes(attrs.read_bytes()+b'docs/validation/part-296/** binary\n');assert h((r/'docs/changelog/0044.md').read_bytes())==closed44
receipt={'Status':'ACTUAL_MAIN6608_FULL_CALLER_IMAGE_AND_NAMED_SOURCE_ENTRY_FOUNDATION_NEW_NATIVE_AND_CI_PENDING','ParentHead':parent,'SourcePaths':26,'Passed':6608,'Failed':0,'Historical6606OrderedNamesExact':True,'Added':[n for n in names if n in added],'Registry':502,'ErlangPresence':344,'ErlangReference':351,'IntegrationAndPackagesPassed':True,'IsolatedInitialFocusedGroupsPassed':40,'InitialFixtureParameterCountFailureRetained':True,'HistoricalAssertionsRemoved':0,'CI295Passed':6606,'Original295Verified':native_verified,'Original295Passed':5846 if native_verified else None,'Original295FdPassed':8 if native_verified else None,'LoadNifRegistered':False,'ActualNativeExecuted':False,'BindingsInHostAreManagedFakes':True,'SameHeadCI296Verified':False,'ChangedSourceOriginalVerified':False,'WholeCompatibility':False,'Closed0044Sha256':closed44}
receipt['Files']=[{'Path':q.relative_to(o).as_posix(),'Bytes':q.stat().st_size,'Sha256':h(q.read_bytes())} for q in sorted(o.rglob('*')) if q.is_file()]
(o/'evidence-receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8');print(json.dumps({'Passed':6608,'SourcePaths':26,'Original295Verified':native_verified,'ProofFiles':len(receipt['Files']),'VolumeBytes':len(combined)}))
