from pathlib import Path
import json,hashlib
R=Path(__file__).resolve().parents[2];O=Path(__file__).resolve().parent
def sha(p):return hashlib.sha256((R/p).read_bytes()).hexdigest().upper()
def read(p):return json.loads((R/p).read_text(encoding='utf-8-sig'))
def out(n,x):(O/n).write_text(json.dumps(x,indent=2)+'\n',encoding='utf-8')
sources=['src/Erlang.Runtime/OrdsetFullCallbacks.cs','src/Erlang.Runtime/OrdsetFullModule.cs','src/Erlang.Runtime/ListCallbackSourceExecution.cs','src/Erlang.Runtime/OrdsetFullTrace.cs','src/Erlang.Runtime/OrdsetFullScope.cs','src/Erlang.Runtime/ProcessContext.GeneratedFrames.cs','src/Erlang.Runtime/RuntimeExceptionCapture.cs','src/Erlang.Runtime/ModuleRegistry.cs','src/Erlang.Terms/FunctionTerm.cs','artifacts/next-api371-proposal1/ordsets.erl.pinned.txt','artifacts/erlang371-root/lists.erl.pinned.txt']
out('source-bindings.json',[{'Path':p,'Sha256':sha(p)} for p in sources])
common=[
 'Both ordsets functions have one unconditional pinned source clause and no own validation: fold/3 tail-calls lists:foldl/3 at501-502; filter/2 tail-calls lists:filter/2 at521-522. Native annotations are source dependency metadata, not ordsets frames retained by these tail calls.',
 'Current OrdsetFullCallbacks enters EnterSourceTailCall before the genuine Runtime.Modules.Call. The tail helper removes only the matching current SourceWrapper, restoring it on scope disposal. ModuleRegistry resolves the actual currently selected lists entry, so no optimized bypass or pre-validation can intercept replacement modules. Permanent replacement HOST from371 remains applicable.',
 'On the ordinary selected lists path ListFunctions.Run dispatches to ListCallbackSourceExecution.Run, which removes matching lists synthetic wrapper then uses source kernels. All callbacks receive the same ProcessContext through FunctionTerm.Invoke and the exact stored Head/accumulator arguments without conversion or payload traversal.',
 'FunctionTerm.Invoke checks argument count against declared arity; earlier is_function guard already proves required arity. Valid Erlang local and external functions are both FunctionTerm; undefined selected code fails at actual invocation, not at the is_function guard. Host-only malformed FunctionTerm constructors are not Erlang input terms.',
 'OrdsetFullTrace.Invoke installs source continuation frames around each callback, awaits completion, captures ErlangException with original class/reason/preexisting callback frames, then disposes OrdsetFullScope and current source scope. Other failures (OperationCanceledException) unwind the using scopes instead of being rewritten as Erlang errors.',
 'Fold has only one current continuation; filter keeps non-tail true branches and omits false tail branches. Pending identical continuations are coalesced by source identity; nonidentical continuations remain in order and pending frame storage is bounded to maximum observable backtrace depth. Actual late/mixed-prefix native controls are still required to validate this lowering beyond previous first-error witnesses.',
 'No callback argument or result is compared except filter checking Atom.Name true/false. All payload term kinds, nested compounds, maps, function values, PID/reference values and improper returned accumulator terms remain opaque. TermOrder, NumericEquality, hashing, erlang:length and lists:usort are not dependencies of either complete logical contract.',
 'Iteration uses explicit while/await, never recursive CLR traversal. ReduceAsync admits scheduler/cancellation boundaries; result allocation builds only the selected output list and retains original head objects. Resource capacity and precise scheduling timing are not claimed identical to OTP, but cancellation must release source scopes and shared execution context must remain usable.'
]
proof={'Status':'SOURCE_CLOSURE_PROPOSAL_NOT_EXECUTED_NOT_PROMOTED','PinnedOtpCommit':'ad05823719d77c8faee87348ea39513d4e2f99c5','BasePublishedCommit':'230de4cafb1a67f34855fbea6b089e4b399a3c5d','CommonProof':common,'Contracts':[
 {'Mfa':'ordsets:fold/3','TermPartitions':[
  {'Condition':'F is not a function of arity2, any Acc and any List including Nil','Action':'lists:foldl/3 function_clause with original [F,Acc,List], first source line2463. F guard runs before list branch.'},
  {'Condition':'F valid arity2 and List Nil','Action':'Return exact Acc object; do not invoke callback.'},
  {'Condition':'F valid arity2 and List root non-Cons/non-Nil','Action':'error:{case_clause,List}, lists:foldl/3 line2464; list is not a function_clause at public entry.'},
  {'Condition':'First Cons','Action':'Invoke F(Head,Acc) once at lists:foldl/3 line2465, then transfer result and Tail to foldl_1. Callback arbitrary result becomes next exact accumulator.'},
  {'Condition':'Subsequent Cons','Action':'Invoke F(Head,Acc) once at lists:foldl_1/3 line2470, left-to-right. Successful transfer removes old tail call.'},
  {'Condition':'Final Nil','Action':'Return exact latest accumulator.'},
  {'Condition':'Final improper nonlist suffix','Action':'lists:foldl_1/3 function_clause with actual [F,latestAcc,residual], first source line2469.'},
  {'Condition':'Callback error/exit/throw','Action':'Propagate original class and reason, preserve callback source frames then current first/helper continuation and real non-tail caller. No ordsets continuation.'}],
  'Invariant':'Before each iteration set is the original unvisited suffix and accumulator is exactly the last successful callback return; visits equal consumed Cons count. No callback is called on malformed residual or after callback failure.'},
 {'Mfa':'ordsets:filter/2','TermPartitions':[
  {'Condition':'Pred is not a function of arity1, any List including Nil','Action':'lists:filter/2 function_clause with original [Pred,List], source2526. Guard precedes generator.'},
  {'Condition':'Valid Pred and Nil','Action':'Return Nil; no callback invocation.'},
  {'Condition':'Valid Pred and Cons, callback true','Action':'Append exact original Head to retained output; create non-tail comprehension continuation at lists2527.'},
  {'Condition':'Valid Pred and Cons, callback false','Action':'Discard Head; comprehension continues by tail call without new non-tail continuation.'},
  {'Condition':'Valid Pred and Cons, callback any other Erlang term','Action':'error:{bad_filter,Result}; helper -filter/2-lc$^0/1-0-/2 source2527. Tuple {true,V} is invalid here, unlike filtermap.'},
  {'Condition':'Valid Pred and nonlist generator root or improper suffix','Action':'error:{bad_generator,Residual} at comprehension helper source2527. Do not validate whole list before earlier predicates execute.'},
  {'Condition':'Callback error/exit/throw','Action':'Original class/reason/callback frames retained; helper and pending true-prefix continuation before genuine caller. No ordsets continuation.'}],
  'Invariant':'values contains exactly original successful true-accepted heads in encounter order; set is original unvisited suffix; false outcomes add no pending return, repeated true return identities coalesce.'}],
 'RuntimeBugsConfirmedBySourceReview':[],
 'RemainingEvidenceGates':['Append-only native late helper/mixed-prefix outcomes and full default/0/1/2/8 stacks against exact final source. Existing371 finite witnesses retained.','Root build and permanent focused HOST controls from this proposal; no execution credit claimed here.','Final source bindings and affected canonical regression after any actual production fixes.'],
 'CompatibilityDeltaIfAcceptedLater':{'ImplementedApis':0,'VerifiedContracts':2,'OrdsetsVerified':5,'OrdsetsPending':16},
 'NoMainBuildRunNetworkOrPublication':True}
out('source-contract-proof.json',proof)
profile=read('artifacts/ordsets371-remaining-original-proposal3/tool/profile.json');rows=read('artifacts/ordsets371-fixed-comparison2/rows.json')
witnesses=[]
for mfa in ['ordsets:fold/3','ordsets:filter/2']:
 cases=[(f['Id'],c['Name']) for f in profile['Fixtures'] for c in f['Cases'] if mfa in c['Mfas']]
 indices=[i for i,x in enumerate(rows) if (x['Group'],x['Name']) in cases]
 assert all(rows[i]['Strict'] for i in indices)
 witnesses.append({'Mfa':mfa,'Cases':[{'Group':g,'Name':n} for g,n in cases],'StrictOriginalRowsZeroBased':indices,'StrictObservationCount':len(indices),'FiniteEvidenceOnly':True})
out('existing-original-witnesses.json',{'RowsPath':'artifacts/ordsets371-fixed-comparison2/rows.json','RowsSha256':sha('artifacts/ordsets371-fixed-comparison2/rows.json'),'Contracts':witnesses})
files=[]
for p in sorted((O/'source').rglob('*')):
 if p.is_file():
  dest=p.relative_to(O/'source').as_posix()
  assert not (R/dest).exists()
  files.append({'Source':p.relative_to(R).as_posix(),'Destination':dest,'BaseSha256':None,'Sha256':hashlib.sha256(p.read_bytes()).hexdigest().upper()})
anchor='    Test(Erlang.Tests.OrdsetListsDispatchHostNames.Test, Erlang.Tests.OrdsetListsDispatchHostChecks.Run);'
assert (R/'tests/Erlang.Tests/Program.cs').read_text().count(anchor)==1
lines=['    Test(Erlang.Tests.OrdsetFoldFilterHostNames.'+a+', Erlang.Tests.OrdsetFoldFilterHostChecks.'+b+');' for a,b in [('LateTest','LateFailures'),('IdentityTest','IdentityAsync'),('DeepTest','Deep'),('CancellationTest','Cancellation')]]
out('copy-plan.json',{'Status':'ROOT_INTEGRATES_AFTER_REVIEW_NO_EXECUTION_CREDIT','Files':files,'RegistrationEdit':{'Destination':'tests/Erlang.Tests/Program.cs','BaseSha256':sha('tests/Erlang.Tests/Program.cs'),'ExactUniqueAnchor':anchor,'InsertAfter':'\n'.join(lines)},'BuildOrRun':False,'HostScope':'Four new permanent test names; no existing name/order removed. No production edits or resource fixture needed.'})
out('freeze.json',{'Files':[{'Path':p.relative_to(O).as_posix(),'Sha256':hashlib.sha256(p.read_bytes()).hexdigest().upper()} for p in sorted(O.rglob('*')) if p.is_file() and p.name!='freeze.json'],'NoMainBuildRunNetworkOrPublication':True})
print('Proposal prepared; no build or test executed. '+str([(x['Mfa'],x['StrictObservationCount']) for x in witnesses]))
