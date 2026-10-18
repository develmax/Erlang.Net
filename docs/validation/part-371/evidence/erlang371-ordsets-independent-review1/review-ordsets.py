from pathlib import Path
import hashlib,json
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'artifacts/erlang371-ordsets-independent-review1'
OUT.mkdir(exist_ok=True)
PROPOSAL=ROOT/'artifacts/next-api371-proposal1'
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest().upper()
def dump(p,o): p.write_text(json.dumps(o,indent=2)+'\n',encoding='utf-8')
rows=[
 {'Mfa':'ordsets:new/0','DecisionAfterActualOriginalGate':'Verified compatible candidate','Proof':'Pinned line69 returns literal []; managed body returns immutable Nil.Value. Arity0 has no argument validation, term ordering, equality, numeric conversion, error or side-effect branch.','ComparisonDependency':False},
 {'Mfa':'ordsets:is_empty/1','DecisionAfterActualOriginalGate':'Verified compatible candidate','Proof':'Pinned line138 S =:= []. Since only Nil is exactly equal to empty list, managed arguments[0] is Nil is exhaustive for every valid term kind. Improper Cons and every non-Nil scalar/compound/native record return false, without traversing or comparing payload. No runtime error branch.','ComparisonDependency':False},
 {'Mfa':'ordsets:to_list/1','DecisionAfterActualOriginalGate':'Verified compatible candidate','Proof':'Pinned line178 returns S unconditionally; managed body returns identical arguments[0]. Valid and raw/nonlist terms are deliberately preserved. No traversal, conversion, equality, hash, sorting or error branch.','ComparisonDependency':False},
 {'Mfa':'ordsets:size/1','DecisionAfterActualOriginalGate':'Needs verification until ordinary length source closure separately accepted','Proof':'Pinned line120 calls erlang:length/1, so return branch proper-spine length, invalid/improper badarg and original argument retention are inherited. Managed reuses ErlangListLength.ReadAsync; catch only badarg reconstructs actual length BIF frame then ordsets:size/1 line120. Source caller added at captured depth max1. This is a faithful bounded source adaptation but does not independently close length trapping/zero-budget/cancellation obligations.','ComparisonDependency':False},
 {'Mfa':'ordsets:is_equal/2','DecisionAfterActualOriginalGate':'Needs verification','Proof':'Pinned line160 shallow is_list guards accept Cons and Nil, including improper Cons; guard failure function_clause retains original two arguments. Managed matches guards then NumericEquals, which is correct numeric rather than exact equality intention. Complete compound-map/reference/FUN equivalence remains unclosed and cannot follow from numeric list examples.','ComparisonDependency':True},
 {'Mfa':'ordsets:is_set/1','DecisionAfterActualOriginalGate':'Needs verification','Proof':'Pinned96-103 outer Cons enters helper is_set/2; Nil true; every nonlist false. Helper requires strictly ascending E1<E2, stops false before traversing malformed tail if ordering fails, improper suffix function_clause args [remaining,previous], line100. Managed loop follows branches but relies on unclosed TermOrder for every comparison.','ComparisonDependency':True},
 {'Mfa':'ordsets:is_element/2','DecisionAfterActualOriginalGate':'Needs verification','Proof':'Pinned214-217 tests E>H then E<H then true for equivalence; Nil false; malformed suffix function_clause args [element,residual], line214, tail recursion removes preceding frames. Managed compare sign branches match a total Erlang term ordering; whole-domain comparator dependencies remain pending.','ComparisonDependency':True},
 {'Mfa':'ordsets:add_element/2','DecisionAfterActualOriginalGate':'Needs verification','Proof':'Pinned240-243 recurses on greater, prepends on less, reuses existing numeric-equivalent representative on equal, Nil singleton. Managed explicit prefix preserves payload and suffix sharing and tracks pending non-tail frames at line240. Actual full-stack packet must settle any continuation-frame differences; comparator map/FUN domain still pending after finite equality.','ComparisonDependency':True},
]
sources=[]
for p in sorted((PROPOSAL/'source/src/Erlang.Runtime').glob('OrdsetCore*.cs')):
 sources.append({'Path':str(p.relative_to(ROOT)).replace('\\','/'),'Sha256':sha(p),'Bytes':p.stat().st_size})
sources += [{'Path':str((PROPOSAL/'ordsets.erl.pinned.txt').relative_to(ROOT)).replace('\\','/'),'Sha256':sha(PROPOSAL/'ordsets.erl.pinned.txt'),'Bytes':(PROPOSAL/'ordsets.erl.pinned.txt').stat().st_size}]
report={
 'Status':'INDEPENDENT_SOURCE_ONLY_REVIEW_PENDING_ACTUAL_ORIGINAL_AND_FINAL_MAIN','PinnedCommit':'ad05823719d77c8faee87348ea39513d4e2f99c5','PinnedPath':'lib/stdlib/src/ordsets.erl',
 'NoMainBuildNetworkOrGitActions':True,'PresenceDeltaAfterIntegration':8,'ConditionalVerifiedDeltaAfterActualGates':3,'RemainingPendingAmongNewEight':5,'Rows':rows,'SourceBindings':sources,
 'ReviewedCommonErrorPath':[
  'CoreModules.InstallPreloaded binds ordinary-module entries with runtimeErrorBinding.Native=false because NativeBuiltinCatalog has no ordsets BIF entries; ModuleRegistry uses InvokeWrapper, not erl_erts_errors native formatting, for ordinary ordsets wrapper entry.',
  'OrdsetCoreTrace strips exactly the current ordsets source wrapper then builds pinned ordinary source frames and appends caller SourceLocation/arity/continuation frames. Explicit captured stack prevents duplicate wrapper capture. Exact original stack validation remains mandatory.',
  'Function-clause frame source lines100,160,214,240 and size source line120 match exact pinned source. add_element pending repetitions are bounded by backtrace depth and must match original observations before accepting even the sampled stack behavior.',
  'Size reconstructs native length frame with original set (not residual malformed tail) and erl_erts_errors error_info; appends ordinary ordsets:size line120 when depth permits. It catches only badarg and therefore does not convert scheduler cancellation into list argument errors.',
 ],
 'NoConfirmedLocalCodeBug':'No local branch mismatch found in the four proposed files by source review. This does not assert zero observed differences before actual execution.',
 'MandatoryDoNotPromoteByAssociation':[
  'is_empty and to_list must not acquire TermOrder/MapTerm requirements: their code deliberately performs Nil-kind predicate and identity only.',
  'is_equal uses numeric rather than exact equality, and permits improper lists because Erlang is_list is shallow. Do not replace NumericEquals with Term.Equals or require a proper list.',
  'is_set/is_element/add_element depend on current total term ordering, including nested map key ordering and local FUN identity/order. Part370 finite FUN corrections do not close all compound-map/reference alias cases.',
  'Do not label all eight verified merely because672 selected comparisons pass. The three comparison-free candidates alone have exhaustive independent source branches here.',
 ],
 'FutureRegistrationOnlyReview':'Existing erlang four-candidate proof source hashes will be renewed in a successor review only after Root signals final CoreModules bytes. Compare exact old CoreModules binding A538B78EE3F0B0E8A206C69BD0B6A3883F6A8B2F2B310A951C64E4382DBE788A to final source and assert insertion OrdsetCoreModule.Register(r) only; no hash-only acceptance.',
}
dump(OUT/'review.json',report)
dump(OUT/'source-freeze.json',{'Files':[{'Path':'review.json','Sha256':sha(OUT/'review.json')}]})
print(json.dumps({'Path':str(OUT.relative_to(ROOT)).replace('\\','/'),'ReviewSha256':sha(OUT/'review.json'),'ConditionalVerified':3,'Pending':5}))
