"""Root-only: refuse every archive/metadata mutation until actual full and native gates."""
from pathlib import Path
import hashlib,json,re,subprocess
package=Path(__file__).resolve().parent
r=Path.cwd().resolve();a=r/'artifacts';o=r/'docs/validation/part-300';j=lambda p:json.loads(p.read_bytes());h=lambda b:hashlib.sha256(b).hexdigest().upper();git=lambda *x:subprocess.check_output(['git',*x],cwd=r)
parent='f29bbeeb42aa44b52bd919e352542edbd2e2efaa';assert git('rev-parse','HEAD').decode().strip()==parent
if o.exists():assert [q.relative_to(o).as_posix() for q in sorted(o.rglob('*')) if q.is_file()]==['tests.json'] and (o/'tests.json').read_bytes()==(a/'tests.json').read_bytes()
freeze=a/'root-part300-final-source-freeze.json';assert h(freeze.read_bytes())=='501D9476C7D4A35CCE8949323575A1802BEC4861712F0B7E51AD9A1085FACFA0';f=j(freeze)
count=f['SourcePaths'];cs=f['CSharpPaths'];assert f['ParentHead']==parent and count==len(f['Files']) and cs==27 and count==30 and f['TokensExactForCSharp'] and not f['PublicLoadNifRegistered']
paths={x['Path'] for x in f['Files']};assert len(paths)==count and len([p for p in paths if p.endswith('.cs')])==cs
for x in f['Files']:
 b=(r/x['Path']).read_bytes();assert len(b)==x['Bytes'] and h(b)==x['Sha256']
assert set(git('diff','HEAD','--name-only','-z','--','src','tests','tools','Erlang.Net.slnx','.github/workflows').decode().split('\0'))-{''}<=paths
plan=r/'tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json';assert h(plan.read_bytes())==f['ReviewedSourcePlanSha256'] and len(j(plan)['Files'])==f['ReviewedSourceInputs'] and f['ReviewedSourceInputs']==1001 and h(plan.read_bytes())=='588DC646C2E9593FF89F5F496EC6D6BD308DE1F73AD9CC4329E89759C69ED97E'
assert j(a/'root-part300-full-exit.json')['ExitCode']==0 and b'Validation passed' in (a/'root-part300-full-validation.log').read_bytes()
t=j(a/'tests.json');old=j(r/'docs/validation/part-299/tests.json');names=[x['Name'] for x in t['Tests']]
oldnames=[x['Name'] for x in old['Tests']];added='runtime/nif-owned-purge-discard-shutdown-and-real-unwind'
assert (t['Passed'],t['Failed'])==(6611,0) and len(names)==len(set(names))==6611 and len(oldnames)==6610 and [n for n in names if n in set(oldnames)]==oldnames and [n for n in names if n not in set(oldnames)]==[added] and all(x['Status']=='Passed' for x in t['Tests'])
assert j(a/'integration-results.json')==j(r/'docs/validation/part-299/integration-results.json') and len(j(a/'integration-results.json'))==11
registry=j(r/'docs/supported-mfas.json');assert len(registry)==502 and registry==json.loads(git('show','HEAD:docs/supported-mfas.json'))
native=j(a/'root-part300-main-native-guard.json')
assert native['Status'].startswith('PASS_ACTUAL_MAIN_NATIVE') and native['ExitCode']==0 and native['SourcePaths']==count and native['CSharpPaths']==cs and native['SourceHashesExact'] and native['SourceFreezeSha256']==h(freeze.read_bytes()) and native['ReviewedSourcePlanSha256']==h(plan.read_bytes())
assert native['NonblockingDisposeRegressionPassed'] and native['DormantReentrantDisposeRegressionPassed'] and native['RegistryHeldNifDisposeRefusedBeforeMutation']
assert native['ActualNativeFenceConcurrencyPassed'] and native['NonDefaultNativeProviderRefusalPassed'] and native['HaltRegressionRedThenGreenPassed']
assert native['NativeTrustedWindowsProfilePassed'] and native['ActualGeneratedCSharpRoutePassed'] and not native['GeneralNifAbiComplete'] and not native['PublicLoadNifRegistered']
assert native['FixtureSha256']=='235D9E8997B7760E120ECD5CFACA29F01D0936A8FD89EF8B23DDF7D0DB43883F' and native['FixtureSourceSha256']=='7B6AF591C635DED2FE686A9E8671ACD58F9C8A6A080D47C61420F844FF33F074'
for key in ['ManagedToolDllSha256','RuntimeDllSha256','GeneratedCSharpSha256']:assert re.fullmatch('[0-9A-F]{64}',native[key])
native_lines=(a/'root-part300-main-native.log').read_text(encoding='utf-8-sig').splitlines()
assert native_lines==['Managed backend checks passed; native ABI and original OTP NOT executed.','Internal trusted native fixture checks passed; general NIF ABI NOT implemented.','Native generated source SHA256: '+native['GeneratedCSharpSha256'],'Trusted native generated-source entry routes passed; BEAM/old-image/general ABI NOT established.']
ready=j(r/'docs/module-readiness.json');assert ready['LocalPassedTests']==6611 and ready['RegisteredMfas']==502 and ready['RegisteredMfaDelta']==0 and ready['ComparisonTestReport']=='validation/part-300/tests.json'
isolated=a/'root-nif-lifetime300-isolated';focus=j(isolated/'dormant-revision3-main-focus53-exit.json')
assert focus['ExitCode']==0 and focus['ExistingInitializerAndHeldRegistryRaceUnchanged'] and focus['LifetimeScenarios']==f['OwnedLifetimeScenarios']==13 and focus['ExpectedGroups']==f['IsolatedFocusGroups']==53
focused=re.findall(r'^PASS ([^\s]+)\s*$',(isolated/'dormant-revision3-main-focus53.log').read_text(encoding='utf-8-sig'),re.M);assert len(focused)==len(set(focused))==53 and {'NifLifetimeHostChecks','InitializationLifecycleChecks','BeamLoaderMfaChecks:Races'}<=set(focused)
assert j(isolated/'tokens27-before.json')==j(isolated/'tokens27-after.json') and j(isolated/'format27-guard.json')['TokensExact']
assert f['ActualNativeFenceConcurrencyPassed'] and f['NonDefaultNativeProviderRefusalPassed'] and f['HaltRegressionRedThenGreenPassed']
initial=j(a/'root-part300-initial-full-tests.json');assert (initial['Passed'],initial['Failed'])==(6610,1)
initial_failures=[x for x in initial['Tests'] if x['Status']!='Passed'];assert len(initial_failures)==1 and initial_failures[0]['Name']=='runtime/initialization-ownership-atomic-registry-lifecycle'
assert j(a/'root-part300-initial-full-exit.json')['ExitCode']!=0 and j(a/'root-part300-initial-source-freeze.json')['ParentHead']==parent
assert j(isolated/'dispose-fix-main-focus53-exit.json')['ExitCode']!=0 and b'TimeoutException' in (isolated/'dispose-fix-main-focus53.log').read_bytes()
fixture_contract=a/'part300-unchanged-shutdown-fixture-contracts.json';historical=j(fixture_contract);assert historical['Head']==parent and len(historical['Files'])==3
for row in historical['Files']:
 b=(r/row['Path']).read_bytes();assert len(b)==row['Bytes'] and h(b)==row['Sha256'] and b==git('show',parent+':'+row['Path'])
red=j(isolated/'halt-red-control-exit.json');assert red['ExitCode']!=0 and red['ExpectedFailure'] and not red['ProductionFixApplied'] and b'ReentrantRetirementOperation' in (isolated/'halt-red-control.log').read_bytes()
ci=j(a/'pack299-ci-38720069790-independent-guard.json');assert ci['Head']==parent and ci['Status']=='PASS_EXACT_HEAD299_CI6610_FULL' and ci['Passed']==6610 and ci['Failed']==0 and ci['OrderedNamesExact'] and len(ci['Integration'])==11
prior=j(a/'pack299-native-combined-independent-guard.json');assert prior['Head']==parent and prior['Status']=='PASS_EXACT_HEAD299_LINUX_TRUSTED_SDK_AND_SEPARATE_ORIGINAL13' and prior['SdkSixArgumentNativePassed'] and prior['IndependentOriginal13TypedPassed'] and prior['OriginalCases']==13 and not prior['OriginalAndSdkThirteenCasesCompared'] and not prior['GeneralNifAbiComplete']
closed='114B40E0985AEF79089330BAA00B445D955423CC8C81DB00DE5D160D981EC4C0';assert h((r/'docs/changelog/0044.md').read_bytes())==closed
volume=r/'docs/changelog/0045.md';entry=(package/'part300-changelog-draft.md').read_text(encoding='utf-8-sig').replace('{SOURCE_PATHS}',str(count)).replace('{CSHARP_PATHS}',str(cs)).replace('{SOURCE_INPUTS}',str(f['ReviewedSourceInputs']));combined=volume.read_bytes()+b'\r\n'+entry.replace('\r\n','\n').replace('\n','\r\n').encode();assert b'## Part 300' not in volume.read_bytes() and len(combined)<=32768 and len(combined.splitlines())<=400
index=r/'CHANGELOG.md';ib=index.read_bytes();label='parts295–299 (active)'.encode();assert ib.count(label)==1
metadata=['AGENTS.md','docs/PROGRESS.md','docs/NEXT_STEPS.md','docs/COMPATIBILITY.md','docs/ERLANG_COMPATIBILITY.md','docs/DECISIONS.md']
for name in metadata:assert b'Part300:' not in (r/name).read_bytes() and re.search(br'\r?\n\r?\n',(r/name).read_bytes())
attrs=r/'.gitattributes';assert b'docs/validation/part-300/** binary' not in attrs.read_bytes()
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
manifest(a/'pack299-ci-detached-staging-manifest.json');manifest(a/'pack299-native-detached-staging-manifest.json');manifest(a/'pack299-native-peer-review-detached-staging-manifest.json')
ngpaths=[p for p in [a/'pack299-original-38720200815-independent-guard.json',a/'pack299-original-38720200815-semantic-guard.json'] if p.exists()];nmpath=a/'pack299-original-detached-staging-manifest-for300.json';original299=False;raw_equal=False;variances=0
if ngpaths or nmpath.exists():
 assert len(ngpaths)==1 and nmpath.exists();ng=j(ngpaths[0]);assert ng['Head']==parent and ng['Passed']==5846 and ng['Failed']==0 and ng['FdPassed']==8 and ng['Status'] in ['PASS_EXACT_HEAD299_ORIGINAL5846_AND_FD8','PASS_EXACT_HEAD299_ORIGINAL5846_FD8_PREDECLARED_MAP_STDOUT_VARIANCE']
 raw_equal=ng['Historical4903Module778Expression165DiagnosticRowsExact']
 if not raw_equal:assert ng['PredeclaredNativeMapStdoutVarianceRows']==1 and not ng['RawNormalization'] and ng['HistoricalSourceExpectedSdkGeneratedAndPoliciesExact'];variances=1
 manifest(nmpath);original299=True
for src in sorted(a.glob('root-part300-*')):
 if src.is_file():select(src,o/'root-artifacts'/src.name)
for pattern in ['root-*300*.ps1','root-*300*.py','part300-*-proposal.py','part300-changelog-draft.md','part300-packaging-plan.md','part300-changelog-preliminary-draft.md']:
 for src in sorted(a.glob(pattern)):
  if src.is_file():select(src,o/'root-artifacts'/src.name)
for src in sorted(isolated.iterdir()):
 if src.is_file() and src.suffix in ['.json','.log','.cs']:select(src,o/'isolated-records'/src.name)
proposals=['nif-owned-lifetime299-overlay','nif-owned-lifetime300-revision2','nif-lifetime-mailbox-fixture300-revision3','nif-lifetime-otp2911-bindings-audit1','nif-native-lifecycle-fence300-overlay','nif-native-lifecycle-default-alc300-revision1','nif-native-fence-controls300-overlay','nif-native-fence-probe300-revision1','nif-retirement-halt-isolation-revision1','nif-retirement-halt-isolation-revision2','nif-dispose-registry-lock-revision1','nif-dispose-dormant-reentrant-revision2','nif-dispose-dormant-reentrant-revision3']
for folder in proposals:
 directory=a/folder;assert directory.is_dir(),folder
 for src in sorted(directory.iterdir()):
  if src.is_file() and src.suffix in ['.json','.md','.py','.yml','.txt']:select(src,o/'candidate-provenance'/folder/src.name)
audit=a/'nif-lifetime-otp2911-bindings-audit1'
for src in sorted((audit/'source-excerpts').rglob('*')):
 if src.is_file():select(src,o/'candidate-provenance'/audit.name/src.relative_to(audit))
pinned=a/'part300-lifetime-pinned-source-blobs';pinned_guard=j(pinned/'guard.json');assert pinned_guard['Status']=='EXACT_PINNED_OTP_SOURCE_BLOBS_AND_ALL_AUDIT_EXCERPTS_MATCH' and not pinned_guard['OriginalEmulatorExecuted'] and len(pinned_guard['Files'])==3
for row in pinned_guard['Files']:assert (r/row['Path']).stat().st_size==row['Bytes'] and h((r/row['Path']).read_bytes())==row['Sha256']
for src in sorted(pinned.iterdir()):
 if src.is_file():select(src,o/'candidate-provenance'/'pinned-full-source-blobs'/src.name)
select(fixture_contract,o/'historical-fixture-contracts'/fixture_contract.name)
for row in historical['Files']:select(r/row['Path'],o/'historical-fixture-contracts'/Path(row['Path']).name,row['Sha256'],row['Bytes'])
for src in sorted(package.iterdir()):
 if src.is_file():select(src,o/'packaging-revision2'/src.name)
for name in ['root-dispatch299-ordinary.ps1','root-dispatch299-native.ps1','root-part299-ordinary-dispatch-attempt.json','root-part299-ordinary-dispatch-result.json','root-part299-native-dispatch-attempt.json','root-part299-native-dispatch-result.json','pack299-read-three-runs.ps1']:
 if (a/name).is_file():select(a/name,o/'previous299-dispatch-and-reader'/name)
for src,dst in [(a/'tests.json',o/'tests.json'),(a/'integration-results.json',o/'integration-results.json'),(r/'docs/supported-mfas.json',o/'supported-mfas.json')]:select(src,dst)
for name in ['module-readiness.json','MODULE_READINESS.md']:select(r/'docs'/name,o/name)
for dst,(src,digest) in copies.items():
 b=src.read_bytes();assert h(b)==digest;dst.parent.mkdir(parents=True,exist_ok=True);dst.write_bytes(b)
volume.write_bytes(combined);index.write_bytes(ib.replace(label,'parts295–300 (active)'.encode()))
note='Part300: actual Main6611/full plus eleven integration/package results retain all6610 historical names and add one NIF lifetime HOST with13 scenarios. Bounded registry-owned reservations/retirement/unwind/shutdown and asynchronous public purge boundary; process-wide default-provider native init/unload fence and actual trusted-tool controls. Genuine retirement-originated halt red failure is fixed by independent actor ownership isolation. Initial full6610/6611 failure exposed synchronous Dispose registry contention; the first scheduling fix exposed dormant caller-held-monitor deadlock. Both failures and unchanged historical tests are retained. Final guarded close uses owned async closure outside the monitor, dormant inline closure inside it, and refuses active NIF ownership under a caller-held monitor before shutdown mutation. No public load_nif/general ABI/resources/dirty-work or full shutdown equivalence. CI2996610/full, Linux trusted SDK and separately typed original13 are independently verified; ordinary299 status explicit in receipt. New same-head/full lifetime original checks pending; registry502/erlang344 of351 unchanged, full module/physical/upstream/performance partial.'
for name in metadata:
 q=r/name;b=q.read_bytes();cut=re.search(br'\r?\n\r?\n',b).end();nl=b'\r\n' if b'\r\n' in b else b'\n';q.write_bytes(b[:cut]+note.encode()+nl+nl+b[cut:])
attrs.write_bytes(attrs.read_bytes()+b'docs/validation/part-300/** binary\n');assert h((r/'docs/changelog/0044.md').read_bytes())==closed
receipt={'Status':'ACTUAL_MAIN6611_FULL_BOUNDED_NIF_OWNED_LIFETIME_NATIVE_TOOL_GATES_NEW_SAME_HEAD_PENDING','ParentHead':parent,'SourcePaths':count,'CSharpPaths':cs,'Passed':6611,'Failed':0,'Historical6610OrderedNamesExact':True,'Added':[added],'Registry':502,'ErlangPresence':344,'ErlangReference':351,'IntegrationAndPackagesPassed':True,'FocusedGroupsPassed':53,'LifetimeScenariosPassed':13,'InitialFullFailed6610Of6611Preserved':True,'InitialFullFailureHistoricalInitializer':True,'FirstDisposeSchedulingRevisionFocusFailedPreserved':True,'ThreeHistoricalShutdownFixtureSourcesUnchanged':True,'NonblockingDisposeRegressionPassed':True,'DormantReentrantDisposeRegressionPassed':True,'RegistryHeldNifDisposeRefusedBeforeMutation':True,'GenuineHaltRedControlFailedBeforeFix':True,'ActualPostFullMainNativeWindowsPassed':True,'FenceConcurrencyControlIncluded':True,'NonDefaultProviderRefusalIncluded':True,'ActualGeneratedCSharpRoutePassed':True,'ReviewedSourceInputs':f['ReviewedSourceInputs'],'ReviewedSourcePlanSha256':h(plan.read_bytes()),'CI299Verified':True,'Previous299LinuxSdkNativePassed':True,'Previous299Original13TypedPassed':True,'Previous299Original13SdkCompared':False,'Original299Verified':original299,'Original299StrictRawHistoryEqual':raw_equal,'Original299PredeclaredNativeStdoutVarianceRows':variances,'NewLifetimeOriginalObservationExecuted':False,'PublicLoadNifRegistered':False,'GeneralNifAbiComplete':False,'RegistryOwnedBoundedImageLifetime':True,'SameHeadCI300Verified':False,'ChangedSourceOriginalVerified':False,'WholeCompatibility':False,'Closed0044Sha256':closed}
receipt['Files']=[{'Path':q.relative_to(o).as_posix(),'Bytes':q.stat().st_size,'Sha256':h(q.read_bytes())} for q in sorted(o.rglob('*')) if q.is_file()]
(o/'evidence-receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8');print(json.dumps({'Passed':6611,'SourcePaths':count,'ProofFiles':len(receipt['Files']),'VolumeBytes':len(combined),'Original299Verified':original299}))
