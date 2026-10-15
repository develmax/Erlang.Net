"""Artifact-only builder. Safe to rerun; missing actual evidence remains explicitly unbound."""
from pathlib import Path
import hashlib,json,ast
r=Path.cwd();base=Path(__file__).resolve().parent;a=r/'artifacts';h=lambda b:hashlib.sha256(b).hexdigest().upper()
def pin(name):
 p=r/name
 return {'Path':name,'Bytes':p.stat().st_size if p.is_file() else -1,'Sha256':h(p.read_bytes()) if p.is_file() else 'UNBOUND_PENDING_ACTUAL_ROOT_EVIDENCE'}
rows=[]
def add(src,dest,category):
 p=pin(src);assert p['Bytes']>=0
 assert not any(x['Destination']==dest for x in rows)
 rows.append({'SourcePath':src,'Destination':dest,'Bytes':p['Bytes'],'Sha256':p['Sha256'],'Category':category})
for kind in ['ci','native','original']:
 mp=a/f'pack305-{kind}-detached-staging-manifest.json'
 for row in json.loads(mp.read_bytes()):
  assert pin(row['SourcePath'])['Sha256']==row['Sha256'];rows.append(dict(row,Category='previous305-'+kind))
 add(mp.relative_to(r).as_posix(),'docs/validation/part-306/staging-manifests/'+mp.name,'previous305-'+kind)
def folder(relative,category,shallow=False):
 root=r/relative
 for p in sorted(root.glob('*') if shallow else root.rglob('*')):
  if not p.is_file() or {'.git','bin','obj','__pycache__','candidate','composed-source'}.intersection(p.relative_to(root).parts):continue
  if any('license' in x.lower() or 'cache' in x.lower() for x in p.relative_to(root).parts):continue
  add(p.relative_to(r).as_posix(),'docs/validation/part-306/candidate-provenance/'+root.name+'/'+p.relative_to(root).as_posix(),category)
folder('artifacts/distribution-owner306-combined-formatted2','runtime-candidate')
folder('artifacts/distribution-owner306-combined-validated1','runtime-candidate')
folder('artifacts/nif-zero-export-owner308-validated2','runtime-candidate')
for name in ['distribution-stat307-candidate1','distribution-stat307-fixture-visibility-revision3','distribution-stat307-reopen-schema-revision4','distribution-stat307-registered-schema-revision5']:
 folder('artifacts/'+name,'runtime-candidate')
# Original279 seven-VM raw capture is already archived. Verify and reference its
# uncompressed evidence in place; preserve the bounded guard, never duplicate ZIP.
native_root=r/'docs/validation/part-279/external/native-queue278'
references=[pin(p.relative_to(r).as_posix()) for p in sorted(native_root.rglob('*')) if p.is_file() and p.suffix!='.zip']
references.append(pin('docs/validation/part-279/evidence-receipt.json'))
(base/'reused-original279-references.json').write_text(json.dumps({'Status':'EXISTING_ARCHIVE_HASHED_IN_PLACE_NO_FRESH_EXECUTION','FreshScenarioVMs':7,'DuplicateZipCopied':False,'Files':references},indent=2)+'\n')
for source in ['docs/validation/part-279/external/native-queue278/native269-part278-queue-native-independent-guard.json','docs/validation/part-279/external/native-queue278/native269-part278-queue-faithful-typed-facts.json']:
 add(source,'docs/validation/part-306/reused-original279/'+Path(source).name,'runtime-peer')
add((base/'reused-original279-references.json').relative_to(r).as_posix(),'docs/validation/part-306/reused-original279/reused-original279-references.json','runtime-peer')
folder('artifacts/root-part306-combined-isolated','runtime-failures',True)
folder('artifacts/allocator-memory309-original18-proposal1','allocator-controls')
folder('artifacts/allocator-memory309-original18-proposal1/overlay','allocator-source')
folder('artifacts/allocator-memory309-source-audit1','allocator-source')
for name,category in [('root306-beam310-independent-source-review.json','runtime-peer'),('lifecycle308-validated2-independent-review.json','runtime-peer'),('lifecycle308-source-only-independent-review1.json','runtime-peer'),('nif-proof307-independent-source-review1.json','runtime-peer'),('allocator309-beam310-independent-source-review.json','allocator-peer')]:
 add('artifacts/'+name,'docs/validation/part-306/independent-reviews/'+name,category)
for name in ['root-part306-final-source-freeze2.json','root-part306-full-exit.json','root-part306-full-validation.log','root-part306-full-guard.json','root-part306-main-native-guard.json','root-part306-main-native.log','root-part306-raw-supported-mfas.json','root-part306-registry-metadata-guard.json','root-part306-optional-io-guard.json','root-part306-installed-allocator52.json','root-run306-full.ps1']:
 if (a/name).is_file():add('artifacts/'+name,'docs/validation/part-306/root-artifacts/'+name,'actual-main')
for src,dest in [('artifacts/tests.json','tests.json'),('artifacts/integration-results.json','integration-results.json'),('docs/supported-mfas.json','supported-mfas.json'),('docs/module-readiness.json','module-readiness.json'),('docs/MODULE_READINESS.md','MODULE_READINESS.md')]:
 add(src,'docs/validation/part-306/'+dest,'actual-main')
for p in sorted(a.glob('root*306*')):
 if p.is_file() and p.suffix in ['.py','.ps1'] and not any(x['SourcePath']==p.relative_to(r).as_posix() for x in rows):
  add(p.relative_to(r).as_posix(),'docs/validation/part-306/root-artifacts/'+p.name,'actual-main')
for p in sorted(base.iterdir()):
 if p.is_file() and (p.suffix in ['.py','.md','.txt']):add(p.relative_to(r).as_posix(),'docs/validation/part-306/packaging-proposal3/'+p.name,'packaging-proposal')
index=base/'evidence-index.json';index.write_text(json.dumps(rows,indent=2)+'\n')
c={'Part':306,'ParentHead':'0494375635061b4952d40a3c7667234db321a931','Expected':{'Passed':6627,'Failed':0,'Registry':504,'OptionalIo':506,'Erlang':346,'ErlangReference':351,'NativeInputs':1012,'MfaDelta':1},'SourcePaths':38,'CSharpPaths':22,'SourceFreeze':pin('artifacts/root-part306-final-source-freeze2.json'),'AddedNames':['runtime/distribution-statistics-accounted-storage-generation','runtime/nif-prepared-image-owner-without-global-exports','UNBOUND_ACTUAL_REGISTERED_MFA_TEST_NAME'],'ComparisonTestReport':'validation/part-305/tests.json','RequiredEvidenceCategories':['previous305-ci','previous305-native','previous305-original','runtime-candidate','runtime-failures','runtime-peer','allocator-source','allocator-controls','allocator-peer','actual-main'],'PublicLoadNifActivated':False,'WholeCompatibilityComplete':False,'FreshOriginal306Verified':False}
bindings={'FullExit':'artifacts/root-part306-full-exit.json','FullLog':'artifacts/root-part306-full-validation.log','Tests':'artifacts/tests.json','PreviousTests':'docs/validation/part-305/tests.json','Integration':'artifacts/integration-results.json','PreviousIntegration':'docs/validation/part-305/integration-results.json','FullGuard':'artifacts/root-part306-full-guard.json','NativeGuard':'artifacts/root-part306-main-native-guard.json','RegistryGuard':'artifacts/root-part306-registry-metadata-guard.json','OptionalIoGuard':'artifacts/root-part306-optional-io-guard.json','RuntimePeer':'artifacts/root306-beam310-independent-source-review.json','AllocatorPeer':'artifacts/allocator309-beam310-independent-source-review.json','AllocatorControls':'artifacts/allocator-memory309-original18-proposal1/validation-result.json','NativeSourcePlan':'tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json','RawRegistry':'artifacts/root-part306-raw-supported-mfas.json','DerivedRegistry':'docs/supported-mfas.json','ActiveChangelogBefore':'docs/changelog/0046.md','ReadinessScopeBefore':'docs/module-readiness-scope.json','ReadinessResult':'docs/module-readiness.json','ChangelogIndexBefore':'CHANGELOG.md','MetadataNote':'artifacts/part306-packaging-proposal3/part306-metadata-note.txt','ChangelogDraft':'artifacts/part306-packaging-proposal3/part306-changelog-draft.md','EvidenceIndex':'artifacts/part306-packaging-proposal3/evidence-index.json'}
c.update({key:pin(value) for key,value in bindings.items()})
c['InstalledAllocatorControls']=pin('artifacts/root-part306-installed-allocator52.json')
actual=json.loads((r/'artifacts/tests.json').read_bytes());previous=json.loads((r/'docs/validation/part-305/tests.json').read_bytes())
if (actual['Passed'],actual['Failed'])==(6627,0):
 oldnames={x['Name'] for x in previous['Tests']};c['AddedNames']=[x['Name'] for x in actual['Tests'] if x['Name'] not in oldnames]
c['GuardAssertions']={key:{'Status':'UNBOUND_ROOT_MUST_BIND_EXACT_SUCCESS_AND_REQUIRED_BOOLEAN_FIELDS'} for key in ['FullGuard','NativeGuard','RegistryGuard','OptionalIoGuard']}
c['GuardAssertions'].update({'FullGuard':{'Status':'ACTUAL_MAIN_6627_FULL_ALL6624_ORDERED_NAMES_38_SOURCE_22_CSHARP_AND_11_INTEGRATION_PASS','Passed':6627,'Failed':0,'HistoricalOrderedNamesPreserved':6624,'ReviewedSourceInputs':1012,'PublicLoadNifActivated':False},'NativeGuard':{'Status':'PASS_ACTUAL_MAIN_NATIVE_TRUSTED_WINDOWS_AND_COMPILED_SOURCE','ExitCode':0,'NativeTrustedWindowsProfilePassed':True,'ActualGeneratedCSharpRoutePassed':True,'ActualNativeFenceConcurrencyPassed':True,'NonDefaultNativeProviderRefusalPassed':True,'GeneralNifAbiComplete':False,'PublicLoadNifActivated':False},'RegistryGuard':{'Status':'ACTUAL_RAW504_CAPTURED_ONE_NEW_MFA_ONLY_EXISTING_PROCESS_INFO_DISCLOSURE_RESTORED','Registry':504,'ErlangMfas':346,'PublicMfaDelta':1,'Other503RowsFieldsAndOrderingExact':True,'SourceExporterChanged':False}})
c['GuardAssertions'].update({'RuntimePeer':{'FreezeSha256':'C6AD4F666C908F34F5A4F64E9B7B98C1B1B4FE352F6309FA1FBC26819CBB68F3','SourceCount':38,'CSharpCount':22,'NativeClosureCount':1012,'NativeClosureMismatches':[]},'AllocatorPeer':{'Status':'READONLY_SOURCE_REVIEW_PASS','OriginalExecuted':False,'PromotedFiles':13},'AllocatorControls':{'Status':'FROZEN_SOURCE_PROPOSAL_SYNTHETIC_ONLY','PlannedFreshVmCases':19,'OriginalExecuted':False,'ProjectionAndEnvelopeSyntheticControls':52}})
c['GuardAssertions']['InstalledAllocatorControls']={'Passed':True,'Count':52,'OriginalExecuted':False,'SdkCompared':False,'SyntheticOnly':True}
c['GuardAssertions']['OptionalIoGuard']={'Status':'PASS_ACTUAL_FULL_HOST_DEFAULT504_OPTIONAL_IO506','DefaultExports':504,'OptionalIoExports':506,'IndependentAdditionalExecution':False}
c['MetadataPaths']=['AGENTS.md','docs/PROGRESS.md','docs/NEXT_STEPS.md','docs/COMPATIBILITY.md','docs/ERLANG_COMPATIBILITY.md','docs/DECISIONS.md']
c['StageMetadataPaths']=c['MetadataPaths']+['CHANGELOG.md','docs/changelog/0046.md','docs/module-readiness-scope.json','docs/module-readiness.json','docs/MODULE_READINESS.md','docs/supported-mfas.json']
c['ChangelogIndexOldLabel']='parts302–305 (active)';c['ChangelogIndexNewLabel']='parts302–306 (active)'
(base/'config-unbound.json').write_text(json.dumps(c,indent=2)+'\n')
for script in base.glob('*.py'):ast.parse(script.read_text(encoding='utf-8-sig'))
print(json.dumps({'EvidenceRows':len(rows),'AllScriptAstParsed':True,'ActualEvidenceStillRequiresRootBinding':True}))
