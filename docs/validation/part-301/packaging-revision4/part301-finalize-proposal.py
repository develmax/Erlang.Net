"""Root-only: every mutation is gated by actual full/native and immutable source proof."""
from pathlib import Path
import hashlib,json,re,subprocess
r=Path.cwd().resolve();a=r/'artifacts';o=r/'docs/validation/part-301';j=lambda p:json.loads(p.read_bytes());h=lambda b:hashlib.sha256(b).hexdigest().upper();git=lambda *x:subprocess.check_output(['git',*x],cwd=r)
parent='c2d2cc304d03201dd52592c77d0389062aea0268';assert git('rev-parse','HEAD').decode().strip()==parent and not o.exists()
freeze=a/'root-part301-final-source-freeze.json';assert h(freeze.read_bytes())=='A763E55596D771736233F06A197ED46485013D8EFD0D01D13B854BE44AFAED90';f=j(freeze)
assert f['ParentHead']==parent and f['SourcePaths']==len(f['Files'])==14 and f['CSharpPaths']==12 and f['TokensExactForCSharp'] and f['ReviewedSourceInputs']==1003 and not f['OriginalNewCasesExecuted'] and not f['FullPinnedDefaultPhysicalParity']
paths={x['Path'] for x in f['Files']};assert len(paths)==14 and len([p for p in paths if p.endswith('.cs')])==12
for row in f['Files']:
 data=(r/row['Path']).read_bytes();assert len(data)==row['Bytes'] and h(data)==row['Sha256']
assert set(git('diff','HEAD','--name-only','-z','--','src','tests','tools','Erlang.Net.slnx','.github/workflows').decode().split('\0'))-{''}<=paths
plan=r/'tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json';assert h(plan.read_bytes())==f['ReviewedSourcePlanSha256']=='DCDB739375C0CA2C11EB8DC2998A9B790BD6F202D46FAAB136F13F471544D03D' and len(j(plan)['Files'])==1003
assert j(a/'root-part301-full-exit.json')['ExitCode']==0 and b'Validation passed' in (a/'root-part301-full-validation.log').read_bytes()
t=j(a/'tests.json');old=j(r/'docs/validation/part-300/tests.json');names=[x['Name'] for x in t['Tests']];oldnames=[x['Name'] for x in old['Tests']]
case_constants=dict(re.findall(r'public const string (\w+) = "(process-info-default-subset-[^"]+)";', (r/'tools/Erlang.Differential/ProcessInfoDefaultSubsetCaseNames.cs').read_text()))
case_refs=re.findall(r'^\s*ProcessInfoDefaultSubsetCaseNames\.(\w+),\s*$', (r/'tools/Erlang.Differential/ProcessInfoDefaultSubsetCases.cs').read_text(),re.M)
case_names=[case_constants[name] for name in case_refs];assert len(case_names)==len(set(case_names))==len(case_constants)==8
added=['mfa/erlang:process_info/1',*['oracle/compiled-module/'+name for name in case_names],'runtime/process-info-default-supported-subset-public-registration']
assert (t['Passed'],t['Failed'])==(6621,0) and len(names)==len(set(names))==6621 and len(oldnames)==6611 and [n for n in names if n in set(oldnames)]==oldnames and set(names)-set(oldnames)==set(added) and all(x['Status']=='Passed' for x in t['Tests'])
assert j(a/'integration-results.json')==j(r/'docs/validation/part-300/integration-results.json') and len(j(a/'integration-results.json'))==11
raw_path=a/'root-part301-supported-mfas-raw-export.json';raw=j(raw_path);derived=j(r/'docs/supported-mfas.json');previous=json.loads(git('show',parent+':docs/supported-mfas.json'));key=lambda row:(row['Module'],row['Function'],row['Arity']);new=('erlang','process_info',1)
assert len(raw)==len(derived)==503 and len(previous)==502 and [row for row in raw if key(row)!=new]==previous and [key(row) for row in raw]==[key(row) for row in derived]
for before,after in zip(raw,derived):
 if key(before)==new:assert {k:v for k,v in before.items() if k!='Limits'}=={k:v for k,v in after.items() if k!='Limits'} and before['Status']=='Partially compatible' and all(name in after['Limits'] for name in ['twelve','thirteen','total_heap_size','heap_size','stack_size','garbage_collection','pending'])
 else:assert before==after
raw_capture=j(a/'root-part301-supported-mfas-derived-metadata-receipt.json');assert raw_capture['Status']=='ACTUAL_TERMINAL_FULL503_EXPORT_COPIED_BEFORE_DERIVED_LIMITS_ANNOTATION' and raw_capture['RawSha256']==h(raw_path.read_bytes()) and raw_capture['FullValidationLogSha256']==h((a/'root-part301-full-validation.log').read_bytes()) and raw_capture['ActualFullTestReportSha256']==h((a/'tests.json').read_bytes()) and not raw_capture['SourceProgramModifiedByThisScript']
ready=j(r/'docs/module-readiness.json');assert ready['LocalPassedTests']==6621 and ready['RegisteredMfas']==503 and ready['RegisteredMfaDelta']==1 and ready['ComparisonTestReport']=='validation/part-300/tests.json'
native=j(a/'root-part301-main-native-guard.json');assert native['Status']=='PASS_ACTUAL_MAIN_NATIVE_TRUSTED_WINDOWS_AND_COMPILED_SOURCE' and native['ExitCode']==0 and native['SourcePaths']==14 and native['CSharpPaths']==12 and native['SourceHashesExact'] and native['SourceFreezeSha256']==h(freeze.read_bytes()) and native['ReviewedSourcePlanSha256']==h(plan.read_bytes())
assert native['NativeTrustedWindowsProfilePassed'] and native['ActualGeneratedCSharpRoutePassed'] and native['ActualNativeFenceConcurrencyPassed'] and native['NonDefaultNativeProviderRefusalPassed'] and native['Part300OwnedLifetimeHostPassedInCurrentFull'] and not native['GeneralNifAbiComplete'] and not native['OriginalNewProcessInfoCasesExecuted']
assert native['GeneratedCSharpSha256']=='6E28E8B093F042C22ACAFD2C0213A55C7DCEFC2D162B486AC8443FC7C4E5F77A' and (a/'root-part301-main-native.log').read_text(encoding='utf-8-sig').splitlines()==(a/'root-part300-main-native.log').read_text(encoding='utf-8-sig').splitlines()
isolated=a/'root-process-info301-isolated'
for name in ['integration11-target-exit.json','integration11-focus4-exit.json','integration11-cases-and-mfa-exit.json']:assert j(isolated/name)['ExitCode']==0
case_lines=re.findall(r'^PASS ([^\r\n]+)\s*$',(isolated/'integration11-cases-and-mfa.log').read_text(encoding='utf-8-sig'),re.M);assert case_lines==case_names+['actual registered MFA direct fixture route']
focused=re.findall(r'^PASS ([^\s]+)\s*$',(isolated/'integration11-focus4.log').read_text(encoding='utf-8-sig'),re.M);assert set(focused)=={'IoCharacterHostChecks','ProcessInfoDefaultSubsetHostChecks','ProcessInfoDefaultHostChecks','ProcessInfoObservableHostChecks'} and len(focused)==4
assert j(isolated/'tokens12-before.json')==j(isolated/'tokens12-after.json') and j(isolated/'format12-guard.json')['TokensExact']
first=j(a/'root-part301-initial-full-tests.json');assert (first['Passed'],first['Failed'])==(6619,2) and j(a/'root-part301-initial-full-exit.json')['ExitCode']==1
assert {x['Name'] for x in first['Tests'] if x['Status']!='Passed'}=={'runtime/halt-dedicated-raw-process-numeric-errors-abort-and-real-dump','mfa/erlang:process_info/1'}
assert j(a/'root-part301-initial-source-freeze.json')['SourcePaths']==10

second=j(a/'root-part301-second-full-tests.json');assert (second['Passed'],second['Failed'])==(6620,1) and j(a/'root-part301-second-full-exit.json')['ExitCode']==1
assert [x['Name'] for x in second['Tests'] if x['Status']!='Passed']==['oracle/compiled-module/erlang-system-profile-dead-receiver']
assert j(a/'root-part301-second-source-freeze.json')['SourcePaths']==13
focus=a/'root-system-profile301-focus';assert j(focus/'corrected23-and40-exit.json')=={'ExitCode':0,'OriginalOtpExecuted':False,'SdkOnly':True}
profile_passes=re.findall(r'^PASS ([^\s]+)\s*$',(focus/'corrected23-and40.log').read_text(encoding='utf-8-sig'),re.M);assert len(profile_passes)==63 and len(set(profile_passes[:23]))==23 and profile_passes[23:]==['erlang-system-profile-dead-receiver']*40
assert j(focus/'original40-exit.json')['ExitCode']!=0 and len(re.findall(r'^PASS original dead-receiver ',(focus/'original40.log').read_text(),re.M))==3 and 'TimeoutException' in (focus/'original40.log').read_text()
assert j(focus/'diagnostic40-exit.json')=={'OriginalOtpExecuted':False,'SdkOnly':True,'ExitCode':0} and (focus/'diagnostic40.log').read_text().count('{ok,noproc}')==3
migration=a/'system-profile-dead-receiver301-fixture-revision1';ledger=j(migration/'fixture-migration-ledger.json');assert ledger['Fixture']=='erlang-system-profile-dead-receiver' and ledger['ReplacementOccurrences']==1 and not ledger['CaseNameChanged'] and not ledger['ExpectedOutcomeChanged'] and not ledger['RuntimeAlgorithmChanged']
profile=r/ledger['ParentPath'];parent_profile=git('show',parent+':'+ledger['ParentPath']);assert h(parent_profile)==ledger['ParentSha256']
old_text=parent_profile.decode().replace('\r\n','\n');new_text=profile.read_text();assert old_text.count(ledger['OldBody'])==1 and new_text.count(ledger['NewBody'])==1 and new_text.replace(ledger['NewBody'],ledger['OldBody'])==old_text
migration_freeze=j(migration/'source-freeze.json')
for row in migration_freeze['Files']:assert len((migration/row['Path']).read_bytes())==row['Bytes'] and h((migration/row['Path']).read_bytes())==row['Sha256']

ci=j(a/'pack300-ci-38727017676-independent-guard.json');assert ci['Head']==parent and ci['Status']=='PASS_EXACT_HEAD300_CI6611_FULL' and ci['Passed']==6611 and ci['Failed']==0 and ci['OrderedNamesExact'] and len(ci['Integration'])==11
closed='114B40E0985AEF79089330BAA00B445D955423CC8C81DB00DE5D160D981EC4C0';assert h((r/'docs/changelog/0044.md').read_bytes())==closed
entry=(Path(__file__).resolve().parent/'part301-changelog-draft.md').read_text(encoding='utf-8-sig').replace('\r\n','\n').rstrip('\r\n')+'\n';volume=r/'docs/changelog/0045.md';combined=volume.read_bytes()+b'\r\n'+entry.replace('\n','\r\n').encode();assert b'## Part 301' not in volume.read_bytes() and len(combined)<=32768 and len(combined.splitlines())<=400 and combined.endswith(b'\r\n') and not combined.endswith(b'\r\n\r\n')
index=r/'CHANGELOG.md';ib=index.read_bytes();label='parts295–300 (active)'.encode();assert ib.count(label)==1
assert isinstance(label,bytes) and isinstance(ib,bytes) and isinstance(combined,bytes)
updated_index=ib.replace(label,'parts295–301 (active)'.encode())
metadata=['AGENTS.md','docs/PROGRESS.md','docs/NEXT_STEPS.md','docs/COMPATIBILITY.md','docs/ERLANG_COMPATIBILITY.md','docs/DECISIONS.md']
for name in metadata:assert b'Part301:' not in (r/name).read_bytes() and re.search(br'\r?\n\r?\n',(r/name).read_bytes())
attrs=r/'.gitattributes';assert b'docs/validation/part-301/** binary' not in attrs.read_bytes()
copies={}
def select(src,dst,expected=None,size=None):
 src=src.resolve();dst=dst.resolve();assert src.is_file() and dst.is_relative_to(o) and not {'bin','obj','__pycache__'}.intersection(src.parts) and 'LICENSE.local.txt' not in src.parts
 data=src.read_bytes()
 if expected:assert h(data)==expected
 if size is not None:assert len(data)==size
 if dst in copies:assert copies[dst][1]==h(data)
 copies[dst]=(src,h(data))
def manifest(mp):
 for row in j(mp):select(r/row['SourcePath'],r/row['Destination'],row['Sha256'],row['Bytes'])
 select(mp,o/'staging-manifests'/mp.name)
manifest(a/'pack300-ci-detached-staging-manifest.json')
native300=False;ngpath=a/'pack300-native-combined-independent-guard.json';nmpath=a/'pack300-native-detached-staging-manifest.json'
if ngpath.exists() or nmpath.exists():
 assert ngpath.exists() and nmpath.exists();ng=j(ngpath);assert ng['Head']==parent and ng['Status']=='PASS_EXACT_HEAD300_LINUX_TRUSTED_SDK_AND_SEPARATE_ORIGINAL13' and ng['SdkSixArgumentNativePassed'] and ng['IndependentOriginal13TypedPassed'] and ng['OriginalCases']==13 and ng['SourceInputsVerified']==1001 and not ng['OriginalAndSdkThirteenCasesCompared'];manifest(nmpath);native300=True
ngpaths=[p for p in [a/'pack300-original-38727291159-independent-guard.json',a/'pack300-original-38727291159-semantic-guard.json'] if p.exists()];nmpath=a/'pack300-original-detached-staging-manifest-for301.json';original300=False;raw_equal=False;variances=0
if ngpaths or nmpath.exists():
 assert len(ngpaths)==1 and nmpath.exists();ng=j(ngpaths[0]);assert ng['Head']==parent and ng['Passed']==5846 and ng['Failed']==0 and ng['FdPassed']==8 and ng['Status'] in ['PASS_EXACT_HEAD300_ORIGINAL5846_AND_FD8','PASS_EXACT_HEAD300_ORIGINAL5846_FD8_PREDECLARED_MAP_STDOUT_VARIANCE']
 raw_equal=ng['Historical4903Module778Expression165DiagnosticRowsExact']
 if not raw_equal:assert ng['PredeclaredNativeMapStdoutVarianceRows']==1 and not ng['RawNormalization'] and ng['HistoricalSourceExpectedSdkGeneratedAndPoliciesExact'];variances=1
 manifest(nmpath);original300=True
for src in sorted(a.glob('root-part301-*')):
 if src.is_file():select(src,o/'root-artifacts'/src.name)
for pattern in ['root-*301*.ps1','root-*301*.py','part301-*-proposal.py','part301-changelog-draft.md']:
 for src in sorted(a.glob(pattern)):
  if src.is_file():select(src,o/'root-artifacts'/src.name)
for src in sorted(isolated.iterdir()):
 if src.is_file() and src.suffix in ['.json','.log','.cs']:select(src,o/'isolated-records'/src.name)
for folder in ['process-info-default-completion-audit1','process-info-default-subset-candidate1','process-info-default-subset-readonly-review2','process-info301-integration-revision1']:
 directory=a/folder;assert directory.is_dir()
 for src in sorted(directory.iterdir()):
  if src.is_file() and src.suffix in ['.json','.md','.py','.txt']:select(src,o/'candidate-provenance'/folder/src.name)
 for prefix in ['overlay','host','production','parents']:
  for src in sorted((directory/prefix).rglob('*.cs')):
   if src.is_file() and not {'bin','obj','__pycache__'}.intersection(src.parts):select(src,o/'candidate-provenance'/folder/src.relative_to(directory))
for src in sorted(Path(__file__).resolve().parent.iterdir()):
 if src.is_file() and src.suffix in ['.py','.md','.json']:select(src,o/'packaging-revision4'/src.name)
for directory,archive_label in [(a/'root-system-profile301-focus','system-profile-sdk-focus'),(migration,'system-profile-fixture-provenance'),(a/'part301-packaging-revision2','packaging-revision2-history'),(a/'part301-packaging-revision3','packaging-revision3-history')]:
 for src in sorted(directory.rglob('*')):
  if src.is_file() and not {'bin','obj','__pycache__'}.intersection(src.parts):select(src,o/archive_label/src.relative_to(directory))
for pattern in ['root-dispatch300-*','root-part300-ordinary-dispatch-*','root-part300-native-dispatch-*','pack300-read-three-runs.ps1']:
 for src in sorted(a.glob(pattern)):
  if src.is_file():select(src,o/'previous300-dispatch-and-reader'/src.name)
for src,dst in [(a/'tests.json',o/'tests.json'),(a/'integration-results.json',o/'integration-results.json'),(r/'docs/supported-mfas.json',o/'supported-mfas.json'),(raw_path,o/'supported-mfas-raw-export.json')]:select(src,dst)
for name in ['module-readiness.json','MODULE_READINESS.md']:select(r/'docs'/name,o/name)
for dst,(src,digest) in copies.items():
 data=src.read_bytes();assert h(data)==digest;dst.parent.mkdir(parents=True,exist_ok=True);dst.write_bytes(data)
volume.write_bytes(combined);index.write_bytes(updated_index)
note='Part301: actual Main6621/full plus eleven integration/package results retain all6611 historical names and append8 compiled projections, one supported-subset HOST and one MFA row. process_info/1 exposes12/13 coherent logical fields, with4 pinned physical heap/stack/GC fields absent; /2 behavior unchanged. Registry503/erlang345 of351 is presence only. Exact raw503 exporter output is preserved; only the new row Limits is annotated as derived subset metadata. Four HOST groups/eight generated SDK cases plus the actual registered MFA fixture route pass; eight new original projections plus the single approved system-profile fixture migration await original comparison (5854/fd8 expected). SDK-only historical timeout reproduction and corrected23+40 focus are preserved; no runtime algorithm changed. Prior300 CI/native/ordinary states explicit in receipt; no general NIF/physical/whole-module/performance completion claim.'
for name in metadata:
 q=r/name;data=q.read_bytes();cut=re.search(br'\r?\n\r?\n',data).end();nl=b'\r\n' if b'\r\n' in data else b'\n';q.write_bytes(data[:cut]+note.encode()+nl+nl+data[cut:])
attrs.write_bytes(attrs.read_bytes()+b'docs/validation/part-301/** binary\n');assert h((r/'docs/changelog/0044.md').read_bytes())==closed
receipt={'Status':'ACTUAL_MAIN6621_FULL_SUPPORTED_PROCESS_INFO1_SUBSET_NEW_ORIGINAL8_PENDING','ParentHead':parent,'SourcePaths':14,'CSharpPaths':12,'Passed':6621,'Failed':0,'Historical6611OrderedNamesExact':True,'Added':added,'Registry':503,'RegistryDeltaFromParent':1,'ErlangPresence':345,'ErlangReference':351,'IntegrationAndPackagesPassed':True,'IsolatedHostGroupsPassed':4,'GeneratedSdkCasesPassed':8,'SecondFull6620PassedHistoricalFixtureTimeoutPreserved':True,'SingleHistoricalFixtureMigration':'erlang-system-profile-dead-receiver','FixtureReproductionSdkOnly':True,'CorrectedSystemProfileCasesPassed':23,'CorrectedDeadReceiverRepeatsPassed':40,'Original301ExpectedPassed':5854,'InitialFull6619PassedTwoIntegrationFixtureFailuresPreserved':True,'IntegrationRepairChangesRuntimeAlgorithm':False,'ActualRegisteredMfaFixtureRoutePassed':True,'Raw503ExporterOutputPreserved':True,'OnlyNewMfaLimitsDerivedAnnotation':True,'PhysicalDefaultFieldsOmitted':['total_heap_size','heap_size','stack_size','garbage_collection'],'ProcessInfo2Changed':False,'OriginalNew8CasesExecuted':False,'ActualPostFullMainNativeWindowsPassed':True,'Previous300CI6611Verified':True,'Previous300NativeSdkAndSeparateOriginal13Verified':native300,'Previous300Original13SdkCompared':False,'Original300Verified':original300,'Original300StrictRawHistoryEqual':raw_equal,'Original300PredeclaredNativeStdoutVarianceRows':variances,'ReviewedSourceInputs':1003,'ReviewedSourcePlanSha256':h(plan.read_bytes()),'SameHeadCI301Verified':False,'ChangedSourceOriginalVerified':False,'WholeCompatibility':False,'Closed0044Sha256':closed}
receipt['Files']=[{'Path':q.relative_to(o).as_posix(),'Bytes':q.stat().st_size,'Sha256':h(q.read_bytes())} for q in sorted(o.rglob('*')) if q.is_file()]
(o/'evidence-receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8');print(json.dumps({'Passed':6621,'SourcePaths':14,'ProofFiles':len(receipt['Files']),'VolumeBytes':len(combined),'Original300Verified':original300,'Native300Verified':native300}))
