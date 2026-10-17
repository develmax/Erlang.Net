"""Artifact-only prefull metadata; no production/candidate/Main/Git/SDK/network mutation."""
from pathlib import Path
from collections import Counter
import json,hashlib
R=Path.cwd();O=R/'artifacts/priority351-prefull-metadata-proposal';overlay=O/'overlay';assert not overlay.exists();sha=lambda b:hashlib.sha256(b).hexdigest().upper();enc=lambda v:(json.dumps(v,indent=2)+'\n').encode();read=lambda p:json.loads(p.read_bytes());C=R/'artifacts/spawn-priority351-permanent-candidate';files=[]
def add(target,b,origin):
 p=overlay/target;p.parent.mkdir(parents=True,exist_ok=True);p.write_bytes(b);files.append({'SourcePath':p.relative_to(R).as_posix(),'TargetPath':target,'Bytes':len(b),'Sha256':sha(b),'ParentSha256':sha((R/target).read_bytes()) if (R/target).exists() else None,'Origin':origin})
def proof(p,target):add('docs/validation/part-351/'+target,p.read_bytes(),p.relative_to(R).as_posix())
oldraw=(R/'docs/mfa-compatibility.json').read_bytes();old=json.loads(oldraw);ledger=json.loads(oldraw);registryraw=(R/'docs/supported-mfas.json').read_bytes();registry=json.loads(registryraw)
assert Counter(e['Status'] for e in old['Entries'])=={'Verified compatible':10,'Known differences':23,'Needs verification':554} and len(old['VerifiedSourceFiles'])==59
audit=R/'artifacts/priority351-verified-contract-source-review/report.json';planpath=audit.with_name('proposed-source-guards.json');assert sha(audit.read_bytes())=='4F93CBF3F3FDAD9930DC6A6D6A944E201904611178DC5C0C99A55AB6AE42677C';guards=read(planpath)['ProposedRuntimeBindings'];assert len(guards)==74
for row in guards:assert row['Path'].startswith('src/') and sha((C/row['Path']).read_bytes())==row['Sha256']
prod=R/'artifacts/spawn-priority351-preparation/runtime-styled-source-freeze2.json';assert sha(prod.read_bytes())=='F8A3CB00D45BD9491573B36F7C8857C925794AC4B1A320F39DD7B6EE90948404'
for row in read(prod)['Files']:
 p=C/row['Path'];assert sha(p.read_bytes())==row['Sha256'];proof(p,'verified-preservation/production-source/'+row['Path'])
for p,target in [(audit,'source-assessment.json'),(planpath,'proposed-source-guards.json'),(prod,'production-source-freeze.json')]:proof(p,'verified-preservation/'+target)
focus=R/'artifacts/spawn-priority351-focused-independent/report.json';fr=read(focus);assert fr['ManagedPassed']==24 and fr['Failed']==0 and fr['ParseAll']==10
for name in ['report.json','review.py','source-freeze.json','source-freeze2.json','reader-exit.json','reader.stdout','reader.stderr']:proof(focus.parent/name,'verified-preservation/focused-independent/'+name)
actual=R/'artifacts/spawn-priority351-focus-actual1'
for p in sorted(actual.iterdir()):
 if p.is_file():proof(p,'verified-preservation/focused-actual/'+p.name)
 elif p.is_dir() and p.name.startswith('target-'):
  for q in sorted(p.iterdir()):assert q.is_file();proof(q,'verified-preservation/focused-actual/'+p.name+'/'+q.name)
ci=R/'artifacts/successor350-ci-independent/report.json';assert sha(ci.read_bytes())=='0DE0E08451CDEBD26BC3E247245DA38D79F869CC37A331E510785AEE6B537562';proof(ci,'verified-preservation/ci350-independent.json')
original=R/'artifacts/part345-ordinary-independent/selected-five-predicate-witnesses.json';assert sha(original.read_bytes())=='9667E452C32EF697CF139208FD3A13C49712DAEC39DE241D84D3EE519B02070A';proof(original,'verified-preservation/existing-original345-five-witnesses.json')
review={'Status':'PREFULL351_OLD_TEN_PRESERVATION_SOURCE_AND_MANAGED_FOCUS_APPROVED_INPUT_NOT_FULL_PASS','ProductionFreezeSha256':sha(prod.read_bytes()),'CurrentRuntimeSourceFiles':guards,'Prior350LedgerSha256':sha(oldraw),'PreservedVerifiedMfas':[f"{e['Module']}:{e['Function']}/{e['Arity']}" for e in old['Entries'] if e['Status']=='Verified compatible'],'OldScopesAndLimitsUnchanged':True,'OldEvidencePrefixesUnchanged':True,'NewApiPromotions':0,'SourceAssessmentSha256':sha(audit.read_bytes()),'ActualFocused24IndependentSha256':sha(focus.read_bytes()),'PriorPublished350CiSha256':sha(ci.read_bytes()),'OriginalEvidenceAuthority':'Retain each existing original/complete-contract evidence chain. Original345 selected five witnesses are copied for convenience; earlier self/node/map/binary/bitstring proofs remain at unchanged committed evidence paths. No new original350 or original351 success is asserted.','SourceArgument':'Exact ten primitive handlers and type categories unchanged; node/Self use the selected explicit context, not worker identity. New scheduler-aware checkpoints/admission/continuations are bound by five refreshed source hashes and fifteen added dependencies, with existing guard/match-spec routes preserved. Focus24 includes queue/context/source-image/length/retirement controls, not an exhaustive old-ten test claim.','Full351ValidationPending':True,'BeforePublicationGate':'Complete current-source full/registry/readiness regression before publication.','AfterPublicationGates':['Exact published351 CI','New priority original three portable cases and remaining relevant original comparison'],'SameHead351CiOrOriginalRequiredBeforeStartingFull':False,'MainModified':False,'SdkExecutedByMetadataAuthor':False}
reviewbytes=enc(review);add('docs/validation/part-351/verified-preservation/prefull-review.json',reviewbytes,'authored conditional preservation argument')
preservation={'Kind':'SourceReview','Path':'validation/part-351/verified-preservation/prefull-review.json','Sha256':sha(reviewbytes),'Scope':'Preserve existing ten verified observable contracts/scopes under the exact351 production source with74 dependency bindings, unchanged primitive handlers/prior original evidence and actual24 managed focus. Full351 remains required before publication; same-head CI/new priority original follow publication. This is not a new API promotion.'}
for e in ledger['Entries']:
 if e['Status']=='Verified compatible':e['Evidence'].append(preservation.copy())
ledger['VerifiedSourceFiles']=guards
refinement={'Status':'PREFULL351_PRIORITY_IMPLEMENTED_OTHER_SPAWN_TUNING_STILL_KNOWN','ProductionFreezeSha256':sha(prod.read_bytes()),'ManagedFocus24Sha256':sha(focus.read_bytes()),'CurrentPrioritySlice':'{priority,low|normal|high|max}, last valid option wins/default normal; live priority flag/query and logical dispatcher admission across existing local/connected paths. Signal priority aliases/links/monitors are a separate option domain.','RemainingSourceKnownRefusals':['message_queue_data on_heap/off_heap','min_heap_size/min_bin_vheap_size/fullsweep_after','max_heap_size','scheduler affinity options'],'NoWholeNativeSchedulerOrThroughputParityClaim':True,'Historical350UnsupportedPriorityEvidenceRemainsHistorical':True,'CurrentFullAndPriorityOriginalPending':True}
refinebytes=enc(refinement);add('docs/validation/part-351/spawn-option-refinement/prefull-source-review.json',refinebytes,'authored source-slice refinement')
for e in ledger['Entries']:
 if e['Module']=='erlang' and e['Function']=='spawn_opt' and e['Arity'] in [2,3,4,5]:
  assert e['Status']=='Known differences';e['Scope']='Local and existing connected spawn paths now accept process scheduling priority low/normal/high/max and use logical priority admission. Heap/GC tuning, message_queue_data and scheduler affinity options remain source-proven valid OTP refusals, so the full option contract stays Known differences. Signal priority remains distinct; native scheduler timing/throughput is not certified.'
  e['Limits']=['SPAWN_VALID_TUNING_REJECTED: process scheduling priority is implemented in the351 candidate, but valid message_queue_data on_heap/off_heap, nonnegative min_heap_size/min_bin_vheap_size/fullsweep_after, max_heap_size and scheduler-affinity options remain rejected. Historical350 evidence includes the then-unsupported priority slice and is retained with its original scope.','Priority source/focus evidence supports the implemented logical queue slice, not complete option compatibility, native heap layout, scheduler timing/throughput or new original351 execution.']
  e['Evidence'].append({'Kind':'SourceReview','Path':'validation/part-351/spawn-option-refinement/prefull-source-review.json','Sha256':sha(refinebytes),'Scope':'Current source implements priority while remaining heap/GC/MQD/affinity refusals preserve Known status; prior unsupported-priority evidence is historical and unchanged.'})
stats=R/'artifacts/statistics350-known-classification-proposal';statsledger=read(stats/'ledger-candidate.json');priorstats=next(e for e in statsledger['Entries'] if (e['Module'],e['Function'],e['Arity'])==('erlang','statistics',1))
for row in read(stats/'proposal.json')['ProofFiles']:
 source=R/row['SourcePath'];b=source.read_bytes();assert len(b)==row['Bytes'] and sha(b)==row['Sha256'];add(row['TargetPath'],b,row['SourcePath'])
for index,e in enumerate(ledger['Entries']):
 if (e['Module'],e['Function'],e['Arity'])==('erlang','statistics',1):ledger['Entries'][index]=priorstats
for path in ['src/Erlang.Runtime/RuntimeStatisticsRegistrations.cs','src/Erlang.Runtime/RuntimeStatisticsNames.cs']:assert (C/path).read_bytes()==(R/path).read_bytes()
counts=dict(Counter(e['Status'] for e in ledger['Entries']));assert counts=={'Verified compatible':10,'Known differences':24,'Needs verification':553}
changedledger=[]
for x,y in zip(old['Entries'],ledger['Entries'],strict=True):
 if x!=y:
  key=(x['Module'],x['Function'],x['Arity']);changedledger.append(key)
  if x['Status']=='Verified compatible':assert y['Status']==x['Status'] and y['Scope']==x['Scope'] and y['Limits']==x['Limits'] and y['Evidence'][:-1]==x['Evidence']
  elif key==('erlang','statistics',1):assert x['Status']=='Needs verification' and y['Status']=='Known differences' and y['Evidence'][:len(x['Evidence'])]==x['Evidence']
  else:assert key[0]=='erlang' and key[1]=='spawn_opt' and key[2] in [2,3,4,5] and x['Status']==y['Status']=='Known differences' and y['Evidence'][:-1]==x['Evidence']
assert len(changedledger)==15
oldregistry=json.loads(registryraw);changedregistry=[]
for row in registry:
 if row['Module']=='erlang' and row['Function']=='spawn_opt' and row['Arity'] in [2,3,4,5]:
  before='other unsupported priority/allocator spawn options';assert before in row['Limits'];row['Limits']=row['Limits'].replace(before,'remaining unsupported heap/GC, message_queue_data and scheduler-affinity options')+' Current351 priority slice accepts low/normal/high/max, defaults normal and uses last valid option, initialized before child execution on existing local/connected routes. Logical priority admission is implemented; complete native scheduling policy/performance and remaining tuning options are not certified.'
 elif row['Module']=='erlang' and (row['Function'],row['Arity']) in [('process_flag',2),('process_info',1),('process_info',2)]:
  row['Limits']+=' Current351 scheduling priority: process_flag(priority,low|normal|high|max) returns the previous priority; supported live local process_info priority queries/default logical entry expose actual per-process state. This does not implement physical heap/GC fields, scheduler affinity or native timing parity.'
for x,y in zip(oldregistry,registry,strict=True):
 assert {k:v for k,v in x.items() if k!='Limits'}=={k:v for k,v in y.items() if k!='Limits'}
 if x['Limits']!=y['Limits']:changedregistry.append((x['Module'],x['Function'],x['Arity']))
assert set(changedregistry)=={('erlang','spawn_opt',n) for n in [2,3,4,5]}|{('erlang','process_flag',2),('erlang','process_info',1),('erlang','process_info',2)}
add('docs/mfa-compatibility.json',enc(ledger),'consciously changed prefull metadata:74guards/retained10/statisticsKnown/spawnscope')
add('docs/supported-mfas.json',enc(registry),'seven intentional priority Limits refinements; all587 nonLimits and580 otherLimits unchanged')
for row in files:
 if row['TargetPath'].startswith('docs/validation/'):assert not (R/row['TargetPath']).exists()
paths={r['TargetPath']:r for r in files};assert len(paths)==len(files)
for e in ledger['Entries']:
 for evidence in e['Evidence']:
  target='docs/'+evidence['Path'];source=overlay/target if target in paths else R/target;assert sha(source.read_bytes())==evidence['Sha256']
proposal={'Status':'PREFULL351_METADATA_OVERLAY_REQUIRES_ROOT_APPROVAL_AND_FULL_BEFORE_PUBLICATION','ParentHead':'c1f219f31726fc93e537080f400651d300c47d72','ProductionFreezeSha256':sha(prod.read_bytes()),'BaseLedgerSha256':sha(oldraw),'CandidateLedgerSha256':sha(enc(ledger)),'BaseRegistrySha256':sha(registryraw),'CandidateRegistrySha256':sha(enc(registry)),'Counts':counts,'CurrentRuntimeGuards':74,'PriorTenVerifiedScopesLimitsAndEvidencePrefixesPreserved':True,'NewVerifiedPromotions':0,'FourSpawnOptStayKnownWithPriorityImplemented':True,'StatisticsOnlyStatusTransition':'Needs verification -> Known differences','ChangedLedgerEntries':changedledger,'Other572LedgerEntriesUnchanged':True,'ChangedRegistryLimits':changedregistry,'All587RegistryNonLimitsUnchanged':True,'Other580RegistryLimitsUnchanged':True,'Full351Pending':True,'AfterPublicationOnly':['current-head351 CI','new priority original three portable cases'],'Files':files,'MainModified':False,'CandidateModified':False,'SourcePlansModified':False,'SdkExecuted':False,'Network':False}
(O/'proposal.json').write_bytes(enc(proposal));(O/'copy-plan.json').write_bytes(enc({'Status':'ARTIFACT_ONLY_PREFULL351_METADATA_COPY_PLAN','Files':files}))
freezeout={'Status':'SOURCE_ONLY_PREFULL351_METADATA_WITH_ALL_NEW_EVIDENCE_PATHS_PRESENT','Files':[{'Path':n,'Sha256':sha((O/n).read_bytes())} for n in ['prepare.py','proposal.json','copy-plan.json']],'OverlayFiles':len(files),'EvidenceReferencesRehashed':True,'NoMainCandidateSdkGitNetworkMutation':True};(O/'source-freeze.json').write_bytes(enc(freezeout));print(json.dumps({'Files':len(files),'ProposalSha256':sha((O/'proposal.json').read_bytes()),'FreezeSha256':sha((O/'source-freeze.json').read_bytes())}))
