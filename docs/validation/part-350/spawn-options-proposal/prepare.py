from pathlib import Path
import collections
import copy
import hashlib
import json
import subprocess

R=Path(__file__).resolve().parents[2]
O=Path(__file__).resolve().parent
P=O/'proof/docs/validation/part-350/spawn-options-audit'
PIN='ad05823719d77c8faee87348ea39513d4e2f99c5'
HEAD='712933b97a654a3b6ed42250902017bde0740f17'
def h(b):return hashlib.sha256(b).hexdigest().upper()
def rd(p):return json.loads(p.read_bytes())
def save(p,v):
 p.parent.mkdir(parents=True,exist_ok=True)
 p.write_text(json.dumps(v,indent=2,ensure_ascii=False)+'\n',encoding='utf-8',newline='\n')
assert subprocess.check_output(['git','rev-parse','HEAD'],cwd=R,text=True).strip()==HEAD
audit=R/'artifacts/spawn-options-next-audit'
assert h((audit/'report.json').read_bytes())=='CFD479C7DDE0F927221A44C31EA991B2F4FEB604C95666326A3072A1BA11030F'
copy_rows=[]
for name in ['report.json','source-freeze2.json','execution-admission-addendum.json']:
 raw=(audit/name).read_bytes();target=P/('prior-'+name);target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(raw)
 copy_rows.append(dict(SourcePath=(audit/name).relative_to(R).as_posix(),TargetPath=target.relative_to(O/'proof').as_posix(),Bytes=len(raw),Sha256=h(raw)))
anchors=[]
for path,ranges in {
 'erts/emulator/beam/bif.c':[(1027,1073)],
 'erts/emulator/beam/erl_process.c':[(12222,12428)],
 'erts/emulator/beam/erl_gc.c':[(3825,3859)],
 'erts/preloaded/src/erlang.erl':[(10748,10796),(11040,11057)]
}.items():
 raw=subprocess.check_output(['git','-C',str(R.parent/'otp'),'show',PIN+':'+path])
 assert raw==(audit/'pinned-otp'/path).read_bytes()
 lines=raw.decode().splitlines()
 anchors.append(dict(Path=path,Commit=PIN,Bytes=len(raw),Sha256=h(raw),Excerpts=[dict(FirstLine=a,LastLine=b,Lines=lines[a-1:b]) for a,b in ranges]))
save(P/'pinned-source-anchors.json',dict(PinnedOtpCommit=PIN,Files=anchors))
managed=[]
for path in ['src/Erlang.Runtime/ProcessSpawnOptions.cs','src/Erlang.Runtime/ProcessSpawnOptionRegistrations.cs']:
 raw=(R/path).read_bytes();published=subprocess.check_output(['git','show',HEAD+':'+path],cwd=R);assert raw==published
 target=P/'managed-source'/path;target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(raw)
 managed.append(dict(Path=path,Bytes=len(raw),Sha256=h(raw)))
parser=(R/managed[0]['Path']).read_text();registrations=(R/managed[1]['Path']).read_text()
assert 'new[] { 2, 3, 4, 5 }' in registrations and 'ProcessSpawnOptions options = ParseOptions(context, arguments);' in registrations
assert 'return ProcessSpawnOptions.Parse(arguments[^1]);' in registrations
assert 'default:\n                    throw new ErlangException(ErlangErrorReasons.BadArgument);' in parser
for option in ['min_heap_size','min_bin_vheap_size','max_heap_size','message_queue_data','scheduler']:
 assert option not in parser
assert 'Items: [Atom { Name: ProcessSpawnOptionNames.Priority }' not in parser
proof={
 'Status':'PASS_INDEPENDENT_SOURCE_PROVEN_VALID_OPTIONS_REJECTED_ALL_FOUR_SPAWN_OPT_ARITIES',
 'PublishedHead':HEAD,'PinnedOtpCommit':PIN,'ManagedSourceBindings':managed,
 'ConcreteOptions':[
  dict(Option='{priority,normal}',Original='erts_parse_spawn_opts accepts PRIORITY_NORMAL; no duplicate/invalid options.',Managed='Unrecognized outer tuple reaches default badarg before child creation.'),
  dict(Option='{priority,high}',Original='Same branch accepts PRIORITY_HIGH.',Managed='Same default badarg.'),
  dict(Option='{message_queue_data,on_heap} or {message_queue_data,off_heap}',Original='Parser sets SPO_ON_HEAP_MSGQ or SPO_OFF_HEAP_MSGQ.',Managed='Neither tuple is recognized; badarg.'),
  dict(Option='{min_heap_size,0} or {min_bin_vheap_size,0}',Original='Nonnegative small zero accepted and rounded up to native minimum.',Managed='Neither tuple is recognized; badarg.'),
  dict(Option='{max_heap_size,0} or {fullsweep_after,0}',Original='Native zero max-heap accepted, with final minimum comparison skipped for zero; nonnegative fullsweep_after accepted.',Managed='Neither tuple is recognized; badarg.'),
  dict(Option='{scheduler,0}',Original='Zero accepted as unbound scheduler within0..erts_no_schedulers.',Managed='Tuple unrecognized; badarg.')],
 'AffectedMfas':['erlang:spawn_opt/'+str(a) for a in [2,3,4,5]],
 'ConcreteLocalInputs':['spawn_opt(fun()->ok end,[{priority,normal}])','spawn_opt(node(),fun()->ok end,[{priority,high}])','spawn_opt(erlang,self,[],[{message_queue_data,on_heap}])','spawn_opt(node(),erlang,self,[],[{scheduler,0}])'],
 'AllFourArityProof':'Native/2 delegates to/4; native/3 with node() delegates to/2; native/5 with node() delegates to/4. Managed allfour registrations parse the final options before any SpawnWithOptions/connected request. Same-node witnesses avoid assumptions about remote tuning support.',
 'PriorityDistinction':'Existing link/monitor priority controls signal delivery; it does not implement the outer process scheduling priority tuple.',
 'EvidenceLevel':'Independent pinned-source semantic proof. No new native/managed execution and no runtime test failure claimed.',
 'Prototype351Excluded':True,'Frozen350InputsMustRemainUnchanged':True,'MainModified':False,'SdkExecuted':False,'NetworkUsed':False}
save(P/'source-known-review.json',proof)
basepath=R/'docs/mfa-compatibility.json';rawbase=basepath.read_bytes();base=rd(basepath);candidate=copy.deepcopy(base)
assert len(base['Entries'])==587 and len(base['VerifiedSourceFiles'])==59
assert collections.Counter(e['Status'] for e in base['Entries'])=={'Verified compatible':10,'Known differences':19,'Needs verification':558}
classification={
 'Status':'PASS_SOURCE_ONLY_REPORTING_PROPOSAL_FOUR_SPAWN_OPT_PENDING_TO_KNOWN',
 'ParentHead':HEAD,'PinnedOtpCommit':PIN,'SourceKnownReviewSha256':h((P/'source-known-review.json').read_bytes()),
 'ChangedMfas':proof['AffectedMfas'],'PreservedOtherEntries':583,
 'OldCounts':{'Verified compatible':10,'Known differences':19,'Needs verification':558},
 'ProposedCounts':{'Verified compatible':10,'Known differences':23,'Needs verification':554},
 'RuntimeSourceGuards':59,'OldTenVerifiedEvidencePreservedExact':True,
 'ReportingTiming':'Apply only after unchanged full350 completes. That full froze10/19/558; subsequent reporting CLI must bind a distinct output directory and10/23/554. Do not rewrite its initial sourcefreeze, ledger or claimed input counts.',
 'NoNativeOrSdkExecutionClaimed':True,'NoRuntimeOrFixtureChanges':True,'StatusPromotionPerformed':False,'RootReviewRequired':True}
save(P/'classification.json',classification)
changed=[]
for before,after in zip(base['Entries'],candidate['Entries']):
 if before['Module']=='erlang' and before['Function']=='spawn_opt' and before['Arity'] in [2,3,4,5]:
  assert before['Status']=='Needs verification'
  after['Status']='Known differences'
  after['Scope']='Local and connected spawn paths exist, but the full OTP-29.1.1 option contract is not compatible. Current published349 rejects source-proven valid process scheduling priority, heap/GC, message_queue_data and scheduler options before child creation; same-node forms establish the difference for this arity. Signal-priority link/monitor support is distinct from process scheduling priority.'
  after['Limits']=['SPAWN_VALID_TUNING_REJECTED: valid {priority,normal}/{priority,high}, {message_queue_data,on_heap|off_heap}, nonnegative min_heap_size/min_bin_vheap_size/full­sweep_after, {max_heap_size,0} and {scheduler,0} reach managed badarg; native pinned parser accepts the bounded examples.','This is direct source-known incompatibility; no new native execution, runtime failure, complete option implementation or whole-MFA compatibility claim.']
  after['Limits'][0]=after['Limits'][0].replace('full\u00adsweep','fullsweep')
  for target in ['classification.json','source-known-review.json','pinned-source-anchors.json','prior-report.json']:
   after['Evidence'].append(dict(Kind='CompatibilityAudit' if target=='classification.json' else 'SourceReview',Path='validation/part-350/spawn-options-audit/'+target,Sha256=h((P/target).read_bytes()),Scope='Independent source-known valid-option refusal at published349 against pinned OTP-29.1.1; reporting-only classification, no execution or complete option-parity claim.'))
  assert after['Evidence'][:len(before['Evidence'])]==before['Evidence'];changed.append(before['Arity'])
 else: assert before==after
assert sorted(changed)==[2,3,4,5] and candidate['VerifiedSourceFiles']==base['VerifiedSourceFiles']
assert collections.Counter(e['Status'] for e in candidate['Entries'])==classification['ProposedCounts']
save(O/'ledger-candidate.json',candidate)
assert basepath.read_bytes()==rawbase
save(O/'proposal.json',dict(Status='SOURCE_ONLY_POST_FULL350_REPORTING_PROPOSAL_READY_FOR_ROOT_REVIEW',ParentHead=HEAD,BaseLedgerSha256=h(rawbase),CandidateLedgerSha256=h((O/'ledger-candidate.json').read_bytes()),ClassificationSha256=h((P/'classification.json').read_bytes()),Other583EntriesUnchanged=True,Old10VerifiedEvidenceUnchanged=True,All59RuntimeSourceBindingsUnchanged=True,ChangedMfas=proof['AffectedMfas'],Counts=classification['ProposedCounts'],MainModified=False,RuntimeModified=False,Frozen350Modified=False,SdkExecuted=False,NetworkUsed=False,RootReviewRequired=True))
files=[dict(Path=p.relative_to(O).as_posix(),Bytes=p.stat().st_size,Sha256=h(p.read_bytes())) for p in sorted(O.rglob('*')) if p.is_file() and p.name!='source-freeze.json']
save(O/'source-freeze.json',dict(Files=files))
print(json.dumps(dict(ProposalSha256=h((O/'proposal.json').read_bytes()),LedgerSha256=h((O/'ledger-candidate.json').read_bytes()),FreezeSha256=h((O/'source-freeze.json').read_bytes()),Files=len(files))))
