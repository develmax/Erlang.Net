from pathlib import Path
import hashlib
import json

ROOT=Path(__file__).resolve().parents[2]
OUT=Path(__file__).resolve().parent
PIN='ad05823719d77c8faee87348ea39513d4e2f99c5'
def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest().upper()
def dump(path,data): path.write_text(json.dumps(data,indent=2)+'\n',encoding='utf-8')
def line(path,needle):
    return next(i for i,s in enumerate(path.read_text(encoding='utf-8-sig').splitlines(),1) if needle in s)

deps=[
 'src/Erlang.Runtime/CoreModules.cs', 'src/Erlang.Runtime/ErlangBasicRegistrations.cs',
 'src/Erlang.Runtime/ErlangSizeErrorFrames.cs','src/Erlang.Runtime/ModuleRegistry.cs',
 'src/Erlang.Runtime/RuntimeExceptionCapture.cs','src/Erlang.Runtime/RuntimeExceptionNames.cs',
 'src/Erlang.Runtime/ProcessContext.cs','src/Erlang.Runtime/ErlangBasicBifNames.cs',
 'src/Erlang.Terms/ErlangModuleNames.cs','src/Erlang.Terms/ErlangException.cs',
 'src/Erlang.Compiler/Execution.cs','src/Erlang.Compiler/Execution.AsyncGuards.cs',
 'src/Erlang.Compiler/Semantics.cs','src/Erlang.Compiler/AutoImportSignatures.cs',
 'src/Erlang.Compiler/ExpressionBindings.cs','src/Erlang.Compiler/CompiledExpressionBindings.cs','src/Erlang.Compiler/CompiledBindingScope.cs',
 'src/Erlang.Compiler/CodeGeneration.cs','src/Erlang.Compiler/SourceCallLocations.cs',
 'src/Erlang.Compiler/SourceFunctionCode.cs','src/Erlang.Compiler/Execution.InvocationLoop.cs',
 'src/Erlang.Runtime/ErlangExecutionFrame.cs',
 'src/Erlang.Terms/Cons.cs','src/Erlang.Terms/TupleTerm.cs','src/Erlang.Terms/Integer.cs',
 'src/Erlang.Terms/Term.cs',
]
bindings=[]
for rel in deps:
    p=ROOT/rel
    assert p.exists(), rel
    bindings.append({'Path':rel,'Bytes':p.stat().st_size,'Sha256':sha(p)})
dump(OUT/'source-bindings.json',{'PinnedOtpCommit':PIN,'GuardOnlyNoSourceEdits':True,'Files':bindings})
ledger=json.loads((ROOT/'docs/mfa-compatibility.json').read_text())
guards={r['Path']:r['Sha256'] for r in ledger['VerifiedSourceFiles']}
guard_delta=[r for r in bindings if r['Path'] not in guards]
assert all(guards.get(r['Path'],r['Sha256'])==r['Sha256'] for r in bindings)
dump(OUT/'source-guard-proposal.json',{'BaseLedgerSha256':sha(ROOT/'docs/mfa-compatibility.json'),'ExistingGuardCount':len(guards),'NoChangedExistingGuardHashes':True,'AdditionalGuardRows':guard_delta,'ProposedGuardCount':len(guards)+len(guard_delta),'Reason':'Bind argument evaluation helpers already used by compiled BIF calls. Their BIF-relevant effect is left-to-right once-only value capture; no complete Core lowering or compiler contract promotion is claimed.'})

authority=ROOT/'artifacts/erlang370-inventory-audit1/authority'
upstream=[('bif.c','erts/emulator/beam/bif.c'),('erl_bif_guard.c','erts/emulator/beam/erl_bif_guard.c')]
auth=[]
for local,path in upstream:
    p=authority/local
    auth.append({'Path':path,'LocalPath':str(p.relative_to(ROOT)).replace('\\','/'),'Sha256':sha(p),'Commit':PIN})
dump(OUT/'authority.json',auth)

profile=json.loads((ROOT/'docs/validation/part-370/sources/original-first/tool/profile.json').read_text())
comparison=ROOT/'docs/validation/part-370/comparison/final-old/rows.json'
rows=json.loads(comparison.read_text())
supplement=json.loads((OUT/'cases.json').read_text())
definitions=[
 ('hd',1,'bif.c','hd_1(', 'src/Erlang.Runtime/CoreModules.cs','ErlangModuleNames.Head,',
  'Cons -> return the exact stored Head term without traversal, conversion or reconstruction. Any non-Cons term (Nil included) -> error:badarg.',
  ['Cons singleton/proper/improper: Head is identical reference to payload; arbitrary nested/cyclic host-only terms are not traversed.', 'Nil, Atom, Integer small/big, Float, Tuple, Map, BitString aligned/partial, Pid, Port, Reference, Function and NativeRecord all fail the same single type predicate.', 'No integer indexing, allocation limit, list length, recursion, trap or secondary payload comparison branch.']),
 ('tl',1,'bif.c','tl_1(', 'src/Erlang.Runtime/CoreModules.cs','ErlangModuleNames.Tail,',
  'Cons -> return the exact stored Tail term, including an improper non-list tail. Any non-Cons term (Nil included) -> error:badarg.',
  ['Cons singleton returns Nil; proper returns remaining Cons; improper returns arbitrary Tail exactly.', 'Nil and every non-Cons kind fail one type predicate. Valid improper tails are deliberately accepted and are not validated as lists.', 'No traversal, copying, reductions/trap state, tail equality, integer conversion or capacity branch.']),
 ('tuple_size',1,'bif.c','tuple_size_1(', 'src/Erlang.Runtime/CoreModules.cs','ErlangModuleNames.TupleSize,',
  'Tuple -> integer arity; every non-Tuple term -> error:badarg.',
  ['Empty tuple ->0; every nonempty tuple -> immutable Items.Count as exact Integer.', 'Arity depends only on count, never element kind, identity, comparison, hash or structure.', 'TupleTerm snapshots constructor input into Array.AsReadOnly(items.ToArray()); Items.Count cannot change after construction.', 'NativeRecord is a distinct kind and is not treated as legacy tagged Tuple; tested in guard mode without dropping native-record error payload.', 'Tuple construction/allocation capacity is excluded exactly as in existing complete logical API contract definition; no claim of identical memory capacity or VM header layout. No new tuple constructor limit is imposed by this BIF.']),
 ('is_integer',3,'erl_bif_guard.c','is_integer_3(', 'src/Erlang.Runtime/ErlangBasicRegistrations.cs','ErlangModuleNames.IsInteger,',
  'Validate bounds first: either bound noninteger -> error:badarg, regardless of Value. Otherwise noninteger Value ->false. Otherwise compare inclusive Min <= Value <= Max with arbitrary-precision signed integer comparisons.',
  ['Invalid Min/valid Max, valid Min/invalid Max, both invalid ->same badarg; no conversion to float/native small integer.', 'Valid integer bounds and noninteger Value ->false for all noninteger kinds, including genuine NativeRecord.', 'Integer Value inside inclusive boundaries ->true; less than Min, greater than Max, and reversed bounds ->false.', 'All small/big signed cross-representations collapse to BigInteger total signed order; no numeric term-order coercion, allocation, summation overflow or hash path.', 'Both comparisons short-circuit left-to-right, but are pure and error-free after bound validation.']),
]
contracts=[]
for function,arity,up,anchor,managed,managedanchor,contract,branches in definitions:
    mfa=f'erlang:{function}/{arity}'
    witnesses=[]
    names=set()
    for fixture in profile['Fixtures']:
        for case in fixture['Cases']:
            if mfa in case.get('Mfas',[]):
                names.add(case['Name'])
                found=[(i,r) for i,r in enumerate(rows) if r['Group']==fixture['Id'] and r['Name']==case['Name']]
                assert len(found)==6 and all(r['Status']=='strict' for i,r in found),(mfa,case['Name'],len(found))
                witnesses.append({'Name':case['Name'],'Group':fixture['Id'],'Source':fixture['Path'],'ComparisonRowsZeroBased':[i for i,r in found],'Depths':[r['Depth'] for i,r in found]})
    tests=[c['Name'] for c in supplement if mfa in c['Mfas']]
    contracts.append({'Mfa':mfa,'CurrentStatus':'Needs verification','ProposedStatusAfterFinalGate':'Verified compatible','Contract':contract,'TermBranchProof':branches,'PinnedSource':{'Path':next(a['Path'] for a in auth if a['Path'].endswith('/'+up)),'Line':line(authority/up,anchor),'Sha256':sha(authority/up)},'ManagedSource':{'Path':managed,'Line':line(ROOT/managed,managedanchor),'Sha256':sha(ROOT/managed)},'ExistingOriginalWitnesses':witnesses,'ExistingStrictObservations':len(witnesses)*6,'SupplementCases':tests,'MissingAcceptance':['Execute additive50/250 packet under pinned original GitHub CI and actual final Main; exact full-field comparison for this MFA must be zero-difference.','Root independently accepts source partition and exact current source guard bindings; if source changes after capture, rerun affected observations and review delta.','Retain actual badarg BIF frame, original arguments, error_info formatter and source caller file/line; no stack normalization or stale source substitution.'],'Limits':'Complete observable Erlang logical API contract on supported valid term model; resource capacity, timing, performance and host-only invalid term extensions are not claimed identical.'})

proof={
 'PinnedOtpCommit':PIN,'Status':'SOURCE_CLOSURE_PROPOSAL_PENDING_ADDITIONAL_ORIGINAL_AND_FINAL_MAIN_EXECUTION',
 'NoMainEdits':True,'NoExecutedSupplementCredit':True,'ProposedApiPresenceDelta':0,'ProposedVerifiedContractDeltaAfterGate':4,
 'ProposedErlangVerifiedAfterGate':20,'GlobalVerifiedAfterGate':111,
 'CommonExecutionProof':[
  'All four entries are preloaded native runtime entries. ModuleRegistry.InvokeAdmittedSelection wraps native body through RuntimeExceptionCapture.InvokeNative, preserving admitted body and original immutable saved argument array.',
  'InvokeNative captures synchronous ErlangException and asynchronous failure identically. Call emits error:badarg, erlang MFA, complete original argument list and [{error_info,#{module=>erl_erts_errors}}].',
  'RuntimeExceptionCapture.Call captures caller SourceLocation, arity and continuation chain, removes duplicate continuation identities only, and applies depth max(1,BacktraceDepth). Existing actual original stack observations at default/0/1/2/8 verify these fields.',
  'tuple_size pre-captures native failure using ErlangSizeErrorFrames.Invalid ->ProcessContext.CaptureNativeFailure ->same Call; AutomaticStackTraceCaptured prevents a duplicate BIF frame in outer wrapper.',
  'hd,tl,is_integer3 throw ordinary ErlangException; native wrapper captures same complete native failure. No special error/exit/throw caller suppression branch applies.',
  'Compiler GuardValueAsync handles all four as registered guard BIF calls and evaluates arguments before dispatch. GuardAsync catches ErlangException and yields false, matching BIF-failure guard rejection; no normal failure is converted to a guard result outside guard execution.',
  'All four appear in Semantics.GuardBifs (is_integer3 via ErlangBasicBifNames.Guards) and AutoImportSignatures. Explicit erlang calls and normal auto-import resolve to the same runtime entry unless caller declares no_auto_import or overrides, which is outside any intrinsic BIF contract.',
  'These contracts do not invoke term equality/ordering/hash on argument payloads. Known managed map hashing/order/reference alias gaps do not affect type test and direct member/count/BigInteger operations.',
 ],
 'Rows':contracts,
 'OtherCandidatesNotPromoted':[
  {'Mfa':'erlang:is_function/2','Reason':'Existing external FUN witnesses alone do not cover local FUNs, restored ETF closure and arity guard contexts; FunctionTerm.Arity public host constructor accepts out-of-domain host arities. Add valid local/external closure controls and define only valid Erlang FUN domain before promotion.'},
  {'Mfa':'erlang:bit_size/1;erlang:byte_size/1;erlang:size/1','Reason':'The arithmetic source branch is clear, but logical length Int32 and large bitstring representability require explicit resource-model scope review. Do not transfer binary module complete classification mechanically.'},
  {'Mfa':'erlang:map_size/1','Reason':'Managed map canonicalization and native externally encoded reference word-count lookup anomalies remain explicit broader map-model gates. Count-only bounded review could be possible, but this proposal does not waive construction invariants.'},
 ],
 'ExistingEvidence':{'RowsPath':str(comparison.relative_to(ROOT)).replace('\\','/'),'RowsSha256':sha(comparison),'AcceptancePath':'docs/validation/part-370/comparison/final-old/acceptance.json','AcceptanceSha256':sha(ROOT/'docs/validation/part-370/comparison/final-old/acceptance.json'),'OriginalRetrospectiveAdmissionPath':'docs/validation/part-370/history/old-original-failure/admission/acceptance.json','OriginalFailedRun':'https://github.com/develmax/Erlang.Net/actions/runs/39106192411','FailedRunRetainedAsFailed':True},
}
# Find exact durable old retrospective acceptance rather than guessing path.
admissions=[p for p in (ROOT/'docs/validation/part-370').rglob('acceptance.json') if sha(p)=='E98CFAF61B16E2C63C14F53DDBEB3DC1563E6333285D49C62E47EEA9AF9A2C38']
assert len(admissions)==1
proof['ExistingEvidence']['OriginalRetrospectiveAdmissionPath']=str(admissions[0].relative_to(ROOT)).replace('\\','/')
dump(OUT/'contract-proof.json',proof)
dump(OUT/'proposed-classifications.json',{'Status':'NOT_APPLIED_CONDITIONAL_ON_FINAL_ACTUAL_GATES','ExistingStatusesPreservedUntilGate':True,'Changes':[{'Mfa':c['Mfa'],'From':'Needs verification','To':'Verified compatible','Scope':c['Contract']+' '+c['Limits']} for c in contracts]})
files=[p for p in OUT.rglob('*') if p.is_file() and p.name!='source-freeze.json']
dump(OUT/'source-freeze.json',{'Files':[{'Path':str(p.relative_to(OUT)).replace('\\','/'),'Sha256':sha(p),'Bytes':p.stat().st_size} for p in sorted(files)]})
print(json.dumps({'Contracts':len(contracts),'ExistingStrictObservations':sum(c['ExistingStrictObservations'] for c in contracts),'SupplementValues':len(supplement),'SupplementStacks':len(supplement)*5,'FreezeSha256':sha(OUT/'source-freeze.json')}))
