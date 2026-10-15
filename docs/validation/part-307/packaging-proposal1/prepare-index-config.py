"""Artifact-only index/config builder. Refresh after terminal guards and readiness CLI."""
from pathlib import Path
import hashlib,json,ast
r=Path.cwd();a=r/'artifacts';b=Path(__file__).resolve().parent;sha=lambda data:hashlib.sha256(data).hexdigest().upper()
def pin(name):
 p=r/name;return {'Path':name,'Bytes':p.stat().st_size if p.is_file() else -1,'Sha256':sha(p.read_bytes()) if p.is_file() else 'UNBOUND_PENDING_ACTUAL_EVIDENCE'}
rows=[]
def add(source,dest,category):
 p=pin(source);assert p['Bytes']>=0 and not any(x['Destination']==dest for x in rows)
 rows.append({'SourcePath':source,'Destination':dest,'Bytes':p['Bytes'],'Sha256':p['Sha256'],'Category':category})
verified=[]
allocator_manifest=a/'allocator-memory309-original18-proposal1/part307-original19-staging-manifest.json'
allocator_verified=allocator_manifest.is_file()
if allocator_verified:
 allocator_rows=json.loads(allocator_manifest.read_bytes())['Files'];assert len(allocator_rows)==51
 for row in allocator_rows:assert pin(row['SourcePath'])['Sha256']==row['Sha256'];rows.append(dict(row,Category='previous306-allocator19'))
 add(allocator_manifest.relative_to(r).as_posix(),'docs/validation/part-307/staging-manifests/'+allocator_manifest.name,'previous306-allocator19')
 add('artifacts/allocator-memory309-original18-proposal1/part307-staged-only-replay-envelope.json','docs/validation/part-307/previous-head306-allocator19/staged-only-replay-envelope.json','previous306-allocator19')
for kind in ['ci','native','original']:
 mp=a/f'pack306-{kind}-detached-staging-manifest.json'
 if mp.is_file():
  for row in json.loads(mp.read_bytes()):assert pin(row['SourcePath'])['Sha256']==row['Sha256'];rows.append(dict(row,Category='previous306-'+kind))
  add(mp.relative_to(r).as_posix(),'docs/validation/part-307/staging-manifests/'+mp.name,'previous306-'+kind);verified.append(kind)
candidate='nif-actual-beam-caller310-validated1';root=a/candidate;freeze=json.loads((root/'validation-freeze.json').read_bytes())
assert sha((root/'validation-freeze.json').read_bytes())=='6C9F0AD268237E161147AD457E9D02F1DF30056634F3A874F37F38321B4EEBAA' and len(freeze['Files'])==94
for row in freeze['Files']:
 source='artifacts/'+candidate+'/'+row['Path'];actual=pin(source);assert actual['Bytes']==row['Bytes'] and actual['Sha256']==row['Sha256'];add(source,'docs/validation/part-307/candidate-provenance/'+candidate+'/'+row['Path'],'candidate310')
add('artifacts/'+candidate+'/validation-freeze.json','docs/validation/part-307/candidate-provenance/'+candidate+'/validation-freeze.json','candidate310')
for name in ['root310-independent-source-review.json','actual-beam310-independent-peer-review.json','actual-beam310-independent-peer-review.py']:
 add('artifacts/'+name,'docs/validation/part-307/independent-reviews/'+name,'source-peer')
for name in ['pack306-terminal-envelope.py','pack306-verify-terminals.py','pack306-detached-staging.py','pack306-read-three-runs.ps1','pack306-read-initial-sandbox-failure.json']:
 add('artifacts/'+name,'docs/validation/part-307/previous-head306-audit-controls/'+name,'previous306-audit-controls')
for p in sorted(a.glob('root*307*')):
 if p.is_file():add(p.relative_to(r).as_posix(),'docs/validation/part-307/root-artifacts/'+p.name,'actual-main')
for source,dest in [('artifacts/tests.json','tests.json'),('artifacts/integration-results.json','integration-results.json'),('docs/supported-mfas.json','supported-mfas.json'),('docs/module-readiness.json','module-readiness.json'),('docs/MODULE_READINESS.md','MODULE_READINESS.md')]:add(source,'docs/validation/part-307/'+dest,'actual-main')
for p in sorted(b.iterdir()):
 if p.is_file() and p.suffix in ['.py','.md','.txt']:add(p.relative_to(r).as_posix(),'docs/validation/part-307/packaging-proposal1/'+p.name,'packaging')
(b/'evidence-index.json').write_text(json.dumps(rows,indent=2)+'\n')
c={'Part':307,'ParentHead':'ae4cd999e20e461f7f5917ec346ade31416e2874','SourcePaths':12,'CSharpPaths':8,'AddedNames':[],'ComparisonTestReport':'validation/part-306/tests.json','Verified306ProofKinds':verified,'PublicLoadNifActivated':False,'WholeCompatibilityComplete':False,'FreshOriginal307Verified':False}
c['Allocator19Verified']=allocator_verified
c['Allocator19Manifest']=pin(allocator_manifest.relative_to(r).as_posix())
c['Allocator19Guard']=pin('artifacts/allocator-memory309-original18-proposal1/part307-original19-status-guard.json')
c['Allocator19Replay']=pin('artifacts/allocator-memory309-original18-proposal1/part307-staged-only-replay-envelope.json')
bindings={'SourceFreeze':'artifacts/root-part307-final-source-freeze2.json','FullExit':'artifacts/root-part307-full-exit.json','FullLog':'artifacts/root-part307-full-validation.log','Tests':'artifacts/tests.json','PreviousTests':'docs/validation/part-306/tests.json','Integration':'artifacts/integration-results.json','PreviousIntegration':'docs/validation/part-306/integration-results.json','FullGuard':'artifacts/root-part307-full-guard.json','NativeGuard':'artifacts/root-part307-main-native-guard.json','RegistryGuard':'artifacts/root-part307-registry-metadata-guard.json','OptionalIoGuard':'artifacts/root-part307-optional-io-guard.json','NativeSourcePlan':'tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json','RawRegistry':'artifacts/root-part307-raw-supported-mfas.json','DerivedRegistry':'docs/supported-mfas.json','ActiveChangelogBefore':'docs/changelog/0046.md','ReadinessScopeBefore':'docs/module-readiness-scope.json','ReadinessResult':'docs/module-readiness.json','ChangelogIndexBefore':'CHANGELOG.md','MetadataNote':'artifacts/part307-packaging-proposal1/part307-metadata-note.txt','ChangelogDraft':'artifacts/part307-packaging-proposal1/part307-changelog-draft.md','EvidenceIndex':'artifacts/part307-packaging-proposal1/evidence-index.json','CandidatePeer':'artifacts/actual-beam310-independent-peer-review.json','RootPeer':'artifacts/root310-independent-source-review.json'}
c.update({key:pin(value) for key,value in bindings.items()})
c['GuardAssertions']={'FullGuard':{'Status':'UNBOUND_ROOT_EXACT_ACTUAL307_FULL_STATUS','Passed':6627,'Failed':0,'PublicLoadNifActivated':False},'NativeGuard':{'Status':'PASS_ACTUAL_MAIN_NATIVE_TRUSTED_WINDOWS_AND_COMPILED_SOURCE','ExitCode':0,'NativeTrustedWindowsProfilePassed':True,'ActualGeneratedCSharpRoutePassed':True,'GeneralNifAbiComplete':False,'PublicLoadNifActivated':False},'RegistryGuard':{'Status':'UNBOUND_ROOT_ACTUAL_RAW504_UNCHANGED_MFA_METADATA_STATUS','SourceExporterChanged':False},'OptionalIoGuard':{'Status':'PASS_ACTUAL_FULL_HOST_DEFAULT504_OPTIONAL_IO506','DefaultExports':504,'OptionalIoExports':506,'IndependentAdditionalExecution':False},'CandidatePeer':{'Status':'BOUNDED_INDEPENDENT_SOURCE_AND_RETAINED_EVIDENCE_PASS','FrozenFilesVerified':94,'Tokens8Exact':True,'CallExtAndCallExtLastActualExecutionProven':False,'GeneralNativeAbiComplete':False},'RootPeer':{'Status':'ROOT_BOUNDED_SOURCE_REVIEW_PASS','SourceAndPinHashesExact':True,'PublicMfaAdded':False}}
c['GuardAssertions']['FullGuard'].update({'Status':'ACTUAL_MAIN_6627_FULL_ALL6627_ORDERED_NAMES_12_SOURCE_8_CSHARP_AND_11_INTEGRATION_PASS','HistoricalOrderedNamesPreserved':6627,'ReviewedSourceInputs':1013,'ExtendedExistingHost':'runtime/direct-source-load-nif-exact-caller-capability'})
c['GuardAssertions']['RegistryGuard'].update({'Status':'ACTUAL_RAW504_CAPTURED_ONLY_EXISTING_PROCESS_INFO_DISCLOSURE_RESTORED','Registry':504,'ErlangMfas':346,'PublicMfaDelta':0,'Other503RowsFieldsAndOrderingExact':True})
c['GuardAssertions']['Allocator19Guard']={'Status':'PASS_INDEPENDENT_TERMINAL_ORIGINAL19','Original19Passed':True,'OriginalAndSdkCompared':False,'SdkCompared':False,'DisabledAllocatorProfileCovered':False}
c['GuardAssertions']['Allocator19Replay']={'Status':'PASS_ACTUAL_STAGED_ONLY_PASSIVE_REPLAY','StagedOnlyReplayPassed':True,'Original19Passed':True,'OriginalAndSdkCompared':False,'SdkCompared':False,'DisabledAllocatorProfileCovered':False}
c['MetadataPaths']=['AGENTS.md','docs/PROGRESS.md','docs/NEXT_STEPS.md','docs/COMPATIBILITY.md','docs/ERLANG_COMPATIBILITY.md','docs/DECISIONS.md']
c['StageMetadataPaths']=c['MetadataPaths']+['CHANGELOG.md','docs/changelog/0046.md','docs/module-readiness-scope.json','docs/module-readiness.json','docs/MODULE_READINESS.md','docs/supported-mfas.json']
c['ChangelogIndexOldLabel']='parts302–306 (active)';c['ChangelogIndexNewLabel']='parts302–307 (active)'
(b/'config-unbound.json').write_text(json.dumps(c,indent=2)+'\n')
for p in b.glob('*.py'):ast.parse(p.read_text(encoding='utf-8-sig'))
print(json.dumps({'EvidenceRows':len(rows),'Verified306Kinds':verified,'AllScriptAstParsed':True,'Actual307GuardsRequireRootBinding':True}))
