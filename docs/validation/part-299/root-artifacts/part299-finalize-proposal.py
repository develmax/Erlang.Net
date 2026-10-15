"""Root-only: refuse every archive/metadata mutation until actual full and native gates."""
from pathlib import Path
import hashlib,json,re,subprocess
r=Path.cwd().resolve();a=r/'artifacts';o=r/'docs/validation/part-299';j=lambda p:json.loads(p.read_bytes());h=lambda b:hashlib.sha256(b).hexdigest().upper();git=lambda *x:subprocess.check_output(['git',*x],cwd=r)
parent='e037631759438f944eda934005432ca04fb516e5';assert git('rev-parse','HEAD').decode().strip()==parent
if o.exists():assert [q.relative_to(o).as_posix() for q in sorted(o.rglob('*')) if q.is_file()]==['tests.json'] and (o/'tests.json').read_bytes()==(a/'tests.json').read_bytes()
freeze=a/'root-part299-final-source-freeze.json';f=j(freeze)
assert f['ParentHead']==parent and f['SourcePaths']==len(f['Files'])==55 and f['CSharpPaths']==31 and f['TokensExactForCSharp'] and not f['PublicLoadNifRegistered']
paths={x['Path'] for x in f['Files']};assert len(paths)==55 and len([p for p in paths if p.endswith('.cs')])==31
for x in f['Files']:
 b=(r/x['Path']).read_bytes();assert len(b)==x['Bytes'] and h(b)==x['Sha256']
assert set(git('diff','HEAD','--name-only','-z','--','src','tests','tools','Erlang.Net.slnx','.github/workflows').decode().split('\0'))-{''}<=paths
plan=r/'tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json';assert h(plan.read_bytes())==f['ReviewedSourcePlanSha256']=='7D5BBD71767A5C202A87C955D558A7D2AFE45C490EE33B236D918900122ACBFA' and len(j(plan)['Files'])==f['ReviewedSourceInputs']==992
assert j(a/'root-part299-full-exit.json')['ExitCode']==0 and b'Validation passed' in (a/'root-part299-full-validation.log').read_bytes()
t=j(a/'tests.json');old=j(r/'docs/validation/part-298/tests.json');names=[x['Name'] for x in t['Tests']]
assert (t['Passed'],t['Failed'])==(6610,0) and len(names)==len(set(names))==6610 and names==[x['Name'] for x in old['Tests']] and all(x['Status']=='Passed' for x in t['Tests'])
assert j(a/'integration-results.json')==j(r/'docs/validation/part-298/integration-results.json') and len(j(a/'integration-results.json'))==11
registry=j(r/'docs/supported-mfas.json');assert len(registry)==502 and registry==json.loads(git('show','HEAD:docs/supported-mfas.json'))
native=j(a/'root-part299-main-native-guard.json')
assert native['Status']=='PASS_ACTUAL_MAIN_NATIVE_TRUSTED_WINDOWS_AND_COMPILED_SOURCE' and native['ExitCode']==0 and native['SourcePaths']==55 and native['CSharpPaths']==31 and native['SourceHashesExact'] and native['SourceFreezeSha256']==h(freeze.read_bytes()) and native['ReviewedSourcePlanSha256']==h(plan.read_bytes())
assert native['NativeTrustedWindowsProfilePassed'] and native['ActualGeneratedCSharpRoutePassed'] and not native['GeneralNifAbiComplete'] and not native['PublicLoadNifRegistered']
assert native['FixtureSha256']=='235D9E8997B7760E120ECD5CFACA29F01D0936A8FD89EF8B23DDF7D0DB43883F' and native['FixtureSourceSha256']=='7B6AF591C635DED2FE686A9E8671ACD58F9C8A6A080D47C61420F844FF33F074'
for key in ['ManagedToolDllSha256','RuntimeDllSha256','GeneratedCSharpSha256']:assert re.fullmatch('[0-9A-F]{64}',native[key])
native_lines=(a/'root-part299-main-native.log').read_text(encoding='utf-8-sig').splitlines()
assert native_lines==['Managed backend checks passed; native ABI and original OTP NOT executed.','Internal trusted native fixture checks passed; general NIF ABI NOT implemented.','Native generated source SHA256: '+native['GeneratedCSharpSha256'],'Trusted native generated-source entry routes passed; BEAM/old-image/general ABI NOT established.']
ready=j(r/'docs/module-readiness.json');assert ready['LocalPassedTests']==6610 and ready['RegisteredMfas']==502 and ready['RegisteredMfaDelta']==0 and ready['ComparisonTestReport']=='validation/part-299/tests.json'
isolated=a/'root-nif-native299-isolated';assert j(isolated/'tokens31-before.json')==j(isolated/'tokens31-after.json') and j(isolated/'format31-guard.json')['TokensExact']
ci=j(a/'pack298-ci-38716028747-independent-guard.json');assert ci['Head']==parent and ci['Status']=='PASS_EXACT_HEAD298_CI6610_FULL' and ci['Passed']==6610 and ci['Failed']==0 and ci['OrderedNamesExact'] and len(ci['Integration'])==11
prior=j(a/'pack297-original-38714645985-independent-guard.json');assert prior['Head']=='26e158fc1440002b9269896d0ba1a9cf5b1bbdd5' and prior['Status']=='PASS_EXACT_HEAD297_ORIGINAL5846_AND_FD8' and prior['Passed']==5846 and prior['Failed']==0 and prior['FdPassed']==8 and prior['Historical4903Module778Expression165DiagnosticRowsExact']
closed='114B40E0985AEF79089330BAA00B445D955423CC8C81DB00DE5D160D981EC4C0';assert h((r/'docs/changelog/0044.md').read_bytes())==closed
volume=r/'docs/changelog/0045.md';entry=(a/'part299-changelog-draft.md').read_text(encoding='utf-8-sig');combined=volume.read_bytes()+b'\r\n'+entry.replace('\r\n','\n').replace('\n','\r\n').encode();assert b'## Part 299' not in volume.read_bytes() and len(combined)<=32768 and len(combined.splitlines())<=400
index=r/'CHANGELOG.md';ib=index.read_bytes();label='parts295–298 (active)'.encode();assert ib.count(label)==1
metadata=['AGENTS.md','docs/PROGRESS.md','docs/NEXT_STEPS.md','docs/COMPATIBILITY.md','docs/ERLANG_COMPATIBILITY.md','docs/DECISIONS.md']
for name in metadata:assert b'Part299:' not in (r/name).read_bytes() and re.search(br'\r?\n\r?\n',(r/name).read_bytes())
attrs=r/'.gitattributes';assert b'docs/validation/part-299/** binary' not in attrs.read_bytes()
copies={}
def select(src,dst,expected=None,size=None):
 src=src.resolve();dst=dst.resolve();assert src.is_file() and dst.is_relative_to(o) and not {'bin','obj','__pycache__'}.intersection(src.parts) and 'LICENSE.local.txt' not in src.parts
 b=src.read_bytes()
 if expected:assert h(b)==expected
 if size is not None:assert len(b)==size
 if dst in copies:assert copies[dst][1]==h(b)
 copies[dst]=(src,h(b))
def manifest(mp):
 for x in j(mp):select(r/x['SourcePath'],r/x['Destination'],x['Sha256'],x['Bytes'])
 select(mp,o/'staging-manifests'/mp.name)
manifest(a/'pack298-ci-detached-staging-manifest.json');manifest(a/'pack297-original-detached-staging-manifest-for299.json')
ngpaths=[p for p in [a/'pack298-original-38716051666-independent-guard.json',a/'pack298-original-38716051666-semantic-guard.json'] if p.exists()];nmpath=a/'pack298-original-detached-staging-manifest-for299.json';original298=False;raw_equal=False;variances=0
if ngpaths or nmpath.exists():
 assert len(ngpaths)==1 and nmpath.exists();ng=j(ngpaths[0]);assert ng['Head']==parent and ng['Passed']==5846 and ng['Failed']==0 and ng['FdPassed']==8 and ng['Status'] in ['PASS_EXACT_HEAD298_ORIGINAL5846_AND_FD8','PASS_EXACT_HEAD298_ORIGINAL5846_FD8_PREDECLARED_MAP_STDOUT_VARIANCE']
 raw_equal=ng['Historical4903Module778Expression165DiagnosticRowsExact']
 if not raw_equal:assert ng['PredeclaredNativeMapStdoutVarianceRows']==1 and not ng['RawNormalization'] and ng['HistoricalSourceExpectedSdkGeneratedAndPoliciesExact'];variances=1
 manifest(nmpath);original298=True
for src in sorted(a.glob('root-part299-*')):
 if src.is_file():select(src,o/'root-artifacts'/src.name)
for pattern in ['root-build-native299*.ps1','root-format-native299*.ps1','root-check299*.py','root-seal-guard299.py','root-guard299*.py','root-verify299-native-provenance.py','root-run299-full.ps1','part299-*-proposal.py','part299-changelog-draft.md']:
 for src in sorted(a.glob(pattern)):
  if src.is_file():select(src,o/'root-artifacts'/src.name)
for src in sorted(isolated.iterdir()):
 if src.is_file() and src.suffix in ['.json','.log','.cs']:select(src,o/'isolated-records'/src.name)
proposals=['nif-native-backend-design1','nif-native-backend-candidate1','nif-native-backend-candidate2','nif-native-backend-layout-revision1','nif-native-entry-options-revision1','nif-native-entry-options-revision2','nif-native-veto-harness-revision','nif-native-compiled-source-route-candidate1','nif-native-compiled-source-route-nontail-revision1','nif-pending-varargs-original-proposal1','nif-pending-varargs-semantic-guard1','nif-pending-varargs-semantic-guard2','nif-combined-linux299-proposal','nif-combined-linux299-proposal2','nif-combined-linux299-proposal3']
for folder in proposals:
 directory=a/folder;assert directory.is_dir(),folder
 for src in sorted(directory.iterdir()):
  if src.is_file() and src.suffix in ['.json','.md','.py','.yml','.txt']:select(src,o/'candidate-provenance'/folder/src.name)
for name in ['root-dispatch298-ordinary.ps1','root-part298-ordinary-dispatch-attempt.json','root-part298-ordinary-dispatch-result.json','pack298-read-two-runs.ps1']:
 if (a/name).is_file():select(a/name,o/'previous298-dispatch-and-reader'/name)
for src,dst in [(a/'tests.json',o/'tests.json'),(a/'integration-results.json',o/'integration-results.json'),(r/'docs/supported-mfas.json',o/'supported-mfas.json')]:select(src,dst)
for name in ['module-readiness.json','MODULE_READINESS.md']:select(r/'docs'/name,o/name)
for dst,(src,digest) in copies.items():
 b=src.read_bytes();assert h(b)==digest;dst.parent.mkdir(parents=True,exist_ok=True);dst.write_bytes(b)
volume.write_bytes(combined);index.write_bytes(ib.replace(label,'parts295–299 (active)'.encode()))
note='Part299: actual Main6610/full and all eleven integration/package results retain every historical test name and registry502/erlang344 of351. Final55 source paths/31 CSharp token streams; bounded internal native ownership/callback/pending/metadata foundation plus actual post-full Windows trusted fixture and compiled-source native entry routes. No public load_nif/general ABI/automatic lifecycle claim. Manual Linux source-built pinned-header SDK and separate original13 observation/review infrastructure is sealed992inputs but not executed. CI2986610/full and original2975846/fd8 raw historical rows independently verified; original298 terminal status explicit in receipt. New same-head CI/original/Linux/native observations pending; full module/physical/upstream/performance remain partial.'
for name in metadata:
 q=r/name;b=q.read_bytes();cut=re.search(br'\r?\n\r?\n',b).end();nl=b'\r\n' if b'\r\n' in b else b'\n';q.write_bytes(b[:cut]+note.encode()+nl+nl+b[cut:])
attrs.write_bytes(attrs.read_bytes()+b'docs/validation/part-299/** binary\n');assert h((r/'docs/changelog/0044.md').read_bytes())==closed
receipt={'Status':'ACTUAL_MAIN6610_FULL_AND_TRUSTED_WINDOWS_COMPILED_NATIVE_PROFILE_LINUX_ORIGINAL13_PENDING','ParentHead':parent,'SourcePaths':55,'CSharpPaths':31,'Passed':6610,'Failed':0,'All6610HistoricalOrderedNamesExact':True,'Registry':502,'ErlangPresence':344,'ErlangReference':351,'IntegrationAndPackagesPassed':True,'ActualPostFullMainNativeWindowsPassed':True,'ActualGeneratedCSharpRoutePassed':True,'GeneratedCSharpSha256':native['GeneratedCSharpSha256'],'ReviewedSourceInputs':992,'ReviewedSourcePlanSha256':h(plan.read_bytes()),'CI298Verified':True,'Original297Passed':5846,'Original297FdPassed':8,'Original297StrictRawHistoryEqual':True,'Original298Verified':original298,'Original298StrictRawHistoryEqual':raw_equal,'Original298PredeclaredNativeStdoutVarianceRows':variances,'Original13Executed':False,'LinuxNativeSdkExecuted':False,'PublicLoadNifRegistered':False,'GeneralNifAbiComplete':False,'AutomaticNativeLifecycleImplemented':False,'SameHeadCI299Verified':False,'ChangedSourceOriginalVerified':False,'WholeCompatibility':False,'Closed0044Sha256':closed}
receipt['Files']=[{'Path':q.relative_to(o).as_posix(),'Bytes':q.stat().st_size,'Sha256':h(q.read_bytes())} for q in sorted(o.rglob('*')) if q.is_file()]
(o/'evidence-receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8');print(json.dumps({'Passed':6610,'SourcePaths':55,'ProofFiles':len(receipt['Files']),'VolumeBytes':len(combined),'Original298Verified':original298}))
