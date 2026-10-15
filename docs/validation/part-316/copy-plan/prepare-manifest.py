"""Passive explicit316 copy manifest only; no copy/finalizer/archive/build/native dispatch."""
from pathlib import Path
import json,hashlib
R=Path.cwd().resolve();A=R/'artifacts';D=A/'part316-lean-copy-plan1';ROWS={};DEST={}
def digest(p):return hashlib.sha256(p.read_bytes()).hexdigest().upper()
def read(p):return json.loads(p.read_bytes())
def write(p,v):p.write_bytes((json.dumps(v,indent=2)+'\n').encode())
def add(p,destination,role,expected=None):
 p=p.resolve();assert p.is_relative_to(A) or p==R/'tests/Erlang.Tests/HaltSubprocessRegistrationCounts.cs';assert p.is_file();assert not any(str(p).endswith(x) for x in ['.dll','.pdb','.exe','.zip','.tar','.tar.gz','.so'])
 b=p.read_bytes();assert len(b)<100*1024*1024
 if expected:assert len(b)==expected['Bytes'] and digest(p)==expected['Sha256'],p
 src=p.relative_to(R).as_posix();target='docs/validation/part-316/'+destination
 if src in ROWS:assert ROWS[src]['Destination']==target;return
 assert target not in DEST
 ROWS[src]={'SourcePath':src,'Destination':target,'Bytes':len(b),'Sha256':digest(p),'HashMode':'BinaryExact','Role':role};DEST[target]=src
# Small author/design directories only. The public author's whole candidate is explicitly excluded.
for name in ['allocator-public-sys-profile316-design1','allocator-public-sys-profile316-oracle1','allocator-public-sys-profile316-oracle2']:
 for p in sorted((A/name).rglob('*')):
  if p.is_file():
   assert not {'bin','obj','__pycache__','candidate'}.intersection(p.relative_to(A/name).parts)
   add(p,'source-author/'+name+'/'+p.relative_to(A/name).as_posix(),'SourceOnlyAuthorOrDesign')
name='allocator-public-sys-profile316-candidate1';base=A/name;f=read(base/'source-freeze.json')
for e in f['Files']:add(base/'candidate'/e['Path'],'source-author/public-provider/overlay/'+e['Path'],'PublicProviderSourceOnlyAuthor',e)
for p in sorted(base.iterdir()):
 if p.is_file() and p.name!='source-resource-closure.json':add(p,'source-author/public-provider/'+p.name,'PublicProviderCatalogAndAuthorMetadata')
for p in sorted((base/'pinned-source').iterdir()):
 if p.is_file():add(p,'source-author/public-provider/pinned-source/'+p.name,'ExactPinnedCatalogGeneratorInput')
# Root composition receipts and actual checks; preserve closure metadata, never candidate source/bin trees.
for name in ['allocator-public-sys-profile316-composed1','allocator-public-sys-profile316-composed2']:
 for p in sorted((A/name).iterdir()):
  if p.is_file() and p.name not in ['tokens-before.json','tokens-after.json']:
   add(p,'isolated/'+name+'/'+p.name,'ActualCompositionFormatTargetFocus')
 p=A/name/'tokens-before.json';q=A/name/'tokens-after.json';assert p.read_bytes()==q.read_bytes()
# Ten actual SDK generated observations and their tiny authored runner, no output assemblies.
for n in ['actual-results.json','build.log','exit.json','run.log','Program.cs','AllocatorPublicManagedFocus.csproj']:
 add(A/'allocator-public-sys-profile316-managed-focus'/n,'isolated/generated-sdk/'+n,'ActualTenGeneratedSdkCases')
for n in ['allocator-public-sys-profile316-beam310-source-peer.json','allocator-public-sys-profile316-final-beam310-source-peer.json','allocator-public-sys-profile316-count-correction-beam310-source-peer.json','beam310-peer316-final-composition.py','root-compose316-core.py','root-compose316-final.py','root-freeze316-final.py','root-format316-core.ps1','root-format316-observer.ps1','root-focus316-core.ps1','root-promote316.py','root-correct316-registry-count.py','root-correct316-registry-count2.py','root316-first-full-halt-count-beam310-review.json','root-run316-main-full.ps1','root-run316-main2-full.ps1','root-guard316-full.py']:
 add(A/n,'source-controls/'+n,'SourcePeerCompositionOrCorrection')
# First failed full is immutable and mandatory; current Main2 full log/results are NOT frozen while running.
first=A/'root-part316-main1';tests=read(first/'tests-first-failed.json');assert (tests['Passed'],tests['Failed'])==(6653,1)
for n in ['full-exit.json','full-validation.log','raw-supported-mfas-first-failed.json','source-freeze.json','tests-first-failed.json']:
 add(first/n,'retained-first-main-failure/'+n,'ActualFirstFullFailure6653AndOne')
correction=A/'allocator-public-sys-profile316-count-correction1/correction.json';v=read(correction);assert v['ProductionChanged'] is False
add(correction,'retained-first-main-failure/correction.json','NarrowTestCountCorrectionOnly')
old=A/'allocator-public-sys-profile316-composed2/candidate/tests/Erlang.Tests/HaltSubprocessRegistrationCounts.cs';assert digest(old)==v['BeforeSha256'];add(old,'retained-first-main-failure/HaltSubprocessRegistrationCounts.before.cs','PreviousStale505And507TestConstants')
new=R/'tests/Erlang.Tests/HaltSubprocessRegistrationCounts.cs';assert digest(new)==v['AfterSha256'];add(new,'retained-first-main-failure/HaltSubprocessRegistrationCounts.after.cs','Corrected507And509TestConstants')
main=A/'root-part316-main2';assert digest(main/'source-freeze.json')=='57F9F381B91AECFDA6634167A659B6C6C9C0A9147ED0AF9BCEDA9F1B3AD7680C';assert digest(main/'source-resource-closure.json')=='87E2A60C038EC320EE9F91E9C1A3E7387E250D5EA5EA4AD293D717D973DF4691'
for n in ['source-freeze.json','source-resource-closure.json']:
 add(main/n,'final-main/'+n,'CorrectedMain2ImmutableSourceOnlyUntilFullPass')
for n in ['count-correction-build.log','count-correction-exit.json','count-correction-focus.json','count-correction-focus.log']:
 add(main/n,'retained-first-main-failure/'+n,'ActualCorrectedTargetAndTwoHostChecks')
# Small source-reviewed native plan only, never execute proposals.
for name in ['part316-main-native-proposal1','part316-main-native-proposal2']:
 for p in sorted((A/name).iterdir()):
  if p.is_file():add(p,'source-controls/'+name+'/'+p.name,'SourceOnlyPostFullNativeProposal')
# Prior315 compact terminal controls; raw full binaries/prefixes/ZIPs are external local/API evidence.
summary=A/'pack315-terminal-independent-summary.json';sv=read(summary);assert sv['Head']=='32c394232588c275703e29be444f8d84cb5d27a4'
for n in ['pack315-terminal-independent-summary.json','pack315-ci-independent-guard.json','pack315-native-combined-independent-guard.json','pack315-native-38811779363-independent-envelope-and-sdk-guard.json','pack315-ordinary-strict-independent-guard.json','pack315-reallocation-independent-failure-guard.json','pack315-original13-independent-local-report.json','pack315-original13-independent-local-capture.json','pack315-reallocation-failure-guard.py','pack315-native-terminal-guard.py','pack315-ordinary-contract.py']:
 add(A/n,'previous-head315/'+n,'Prior315IndependentTypedProofOrFailureAudit')
external=[]
for role,run in [('ci',38811662261),('native',38811779363),('ordinary',38811810193),('new-reallocation-failure',38811758281)]:
 name='pack315-watch-'+str(run)+'-terminal';base=A/name;meta=read(base/'artifacts.json')
 for n in ['artifacts.json','raw-capture.json','run.json','jobs.json','download-manifest.json']:
  add(base/n,'previous-head315/'+name+'/'+n,'Prior315RawApiIdentityDigestAndTerminalMetadata')
 for e in meta['artifacts']:
  external.append({'Role':role,'Run':run,'Artifact':e['id'],'ApiDigest':e['digest'],'Bytes':e['size_in_bytes'],'ApiUrl':e['url'],'ArchiveSourcePath':(base/('artifact-'+str(e['id'])+'.zip')).relative_to(R).as_posix(),'Copied':False,'Reason':'Large raw API ZIP and installed-prefix binaries stay local/API; no portable binary replay claim.'})
 manifest=read(base/'download-manifest.json')
 for e in manifest:
  n=e['Path'];p=base/n;take=False
  if role=='ci':take=n.endswith('/tests.json') or n.endswith('/integration-results.json')
  elif role=='ordinary':take=n.endswith('/differential.json') or n.endswith('/fd-differential.json')
  elif role=='native':
   take=('/nif-pending-original-observations/' in n and (n.endswith('/result.etf') or Path(n).name in ['receipt.json','source-plan.json','metadata.etf','metadata.stdout','metadata.stderr','metadata.process.json','observation.stdout','observation.stderr','observation.process.json']))
   take=take or n.endswith('/nif-pending-original-typed-review.json') or n.endswith('/nif-pending-otp-build-provenance.json')
   take=take or ('/nif-bounded-sdk/' in n and Path(n).name in ['sdk-receipt.json','sdk-six-argument.process.json','sdk-six-argument.stdout','sdk-six-argument.stderr','build-receipt.json','compile-veneer.process.json','gcc-version.stdout','nif_bridge-so-undefined.stdout','nif_bridge-so-undefined.stderr','nif_bridge-so-undefined.process.json'])
  else:
   take=('/sdk-native/' in n and (Path(n).name.startswith(('compiler-version.','legacy_fixture-so-compile.','legacy_fixture-so-undefined.'))))
   take=take or n.endswith('/original-build-provenance.json') or n.endswith('/source-review.json') or n.endswith('/runtime-approval.json') or n.endswith('/legacy-provenance.json')
  if take:add(p,'previous-head315/'+name+'/'+n,'Prior315ActualRawTypedOrImportRecipeEvidence',e)
 if role=='new-reallocation-failure':
  job=base/'job-116686447153.log';add(job,'previous-head315/'+name+'/'+job.name,'ActualImportGateFailureJobLog')
write(D/'external-api-artifact-index.json',{'Status':'EXTERNAL_RAW_API_BINARIES_NOT_COPIED_NO_PORTABLE_RUNTIME_CLAIM','Head':sv['Head'],'Artifacts':external})
rows=sorted(ROWS.values(),key=lambda e:e['Destination']);write(D/'static-copy-manifest.json',rows)
# Future families are requirements, NOT guessed successful rows/hashes; Root binds after execution.
plan={'Status':'LEAN316_STATIC_COPY_ROWS_AND_PENDING_DYNAMIC_REQUIRED_FAMILIES_NO_COPY','Part':316,'StaticRows':len(rows),'StaticBytes':sum(e['Bytes'] for e in rows),'MaximumIndividualFileBytes':100*1024*1024,'NoArchiveOrBinaryOrFullSourceTreeCopies':True,'LargestRows':sorted([{'SourcePath':e['SourcePath'],'Bytes':e['Bytes']} for e in rows],key=lambda e:-e['Bytes'])[:6],'ExactFiveRelativeProofTargets':['final-main/tests.json','final-main/full-guard.json','final-native/guard.json','module-readiness.json','evidence-receipt.json'],'FinalizerGeneratedReserved':['evidence-receipt.json'],'DynamicFamilies':[{'Role':'CorrectedMain2ActualFull','SourceDirectory':'artifacts/root-part316-main2','RequiredNames':['full-exit.json','full-validation.log','full-guard.json'],'DestinationPrefix':'final-main/','Requires':'Actual full exit0, zero failures, all historical ordered names and actual507/349 registry checks; no pass inferred from first failure or target.'},{'Role':'ActualMainTestsAndElevenIntegrationResults','Sources':{'artifacts/tests.json':'final-main/tests.json','artifacts/integration-results.json':'final-main/integration-results.json'},'Requires':'Exact reports from successful Main2 full; bind hashes only after Root actual gate.'},{'Role':'ActualPostFullNative','SourceDirectory':'artifacts/root-part316-main-windows-native1','DestinationPrefix':'final-native/','RequiredNames':['guard.json','legacy.attempt.json','legacy.process.json','legacy.stdout','legacy.stderr','provider-query.attempt.json','provider-query.process.json','provider-query.stdout','provider-query.stderr','reallocation.attempt.json','reallocation.process.json','reallocation.stdout','reallocation.stderr'],'Requires':'Actual Main2 sourcefreeze57F9/closure87E2, public profile full observation if separately executed must keep declared backend adaptation, typed reviews; source-only proposal never satisfies this family.'},{'Role':'ActualReadiness','Sources':{'docs/module-readiness.json':'module-readiness.json','docs/module-readiness-scope.json':'readiness/module-readiness-scope.json','docs/MODULE_READINESS.md':'readiness/MODULE_READINESS.md','docs/supported-mfas.json':'readiness/supported-mfas.json'},'Requires':'Actual CLI exit0 and current507default/509optional/erlang349 presence; semantic completion remains partial.'}],'OmittedLargeTokenReports':[{'Directory':n,'BeforeSha256':digest(A/n/'tokens-before.json'),'AfterSha256':digest(A/n/'tokens-after.json'),'RawEqualVerified':True,'Copied':False} for n in ['allocator-public-sys-profile316-composed1','allocator-public-sys-profile316-composed2']],'ExternalRawApiArtifactIndex':'external-api-artifact-index.json','FirstFullFailurePreserved':{'Passed':6653,'Failed':1,'Correction':'Only stale HOST registry constants505/507 ->507/509; no production or original expectation change.'},'EnglishDraftNeedsActualGateUpdate':True,'New316OriginalOrNativeOrFinalFullPassClaimed':False,'MainGitSdkBuildNativeVmMutationOrExecutionByIndexer':False}
write(D/'manifest-plan.json',plan)
print(json.dumps({'Rows':len(rows),'Bytes':plan['StaticBytes'],'ManifestSha256':digest(D/'static-copy-manifest.json'),'PlanSha256':digest(D/'manifest-plan.json'),'LargestRows':plan['LargestRows']}))
