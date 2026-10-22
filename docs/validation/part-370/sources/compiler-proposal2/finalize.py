from pathlib import Path
import json,hashlib
BASE=Path(__file__).resolve().parent;ROOT=BASE.parents[1]
def h(p):return hashlib.sha256(p.read_bytes()).hexdigest().upper()
def save(n,v):(BASE/n).write_text(json.dumps(v,indent=2)+'\n',encoding='utf-8')
plan=json.loads((BASE/'copy-plan.json').read_text())
direct=json.loads((ROOT/'artifacts/erlang370-element-continuation-proposal1/copy-plan.json').read_text())
old={r['Target']:r for r in direct['Rows']}
for row in plan['Rows']:
 assert h(BASE/row['Source'])==row['Sha256']
 if row['Existing']:
  assert row['BaseSha256']==old[row['Target']]['BaseSha256']==h(ROOT/row['Target']),row['Target']
assert len(plan['Rows'])==14 and sum(row['Existing'] for row in plan['Rows'])==7
save('source-proof.json',{
 'Status':'SOURCE_REVIEW_ONLY_UNBUILT',
 'NativeAuthority':{'Path':'artifacts/erlang370-large-tuple-peer1/finding.json','Sha256':h(ROOT/'artifacts/erlang370-large-tuple-peer1/finding.json'),'PinnedOtp':'ad05823719d77c8faee87348ea39513d4e2f99c5'},
 'NewNativeObservation':'Parent reports actual new264 comparison258strict, remaining6 solely exported-index helper. Runtime-only generic invoke summary6strict proves primitive operations correct. No raw artifact was changed or relabelled by this proposal.',
 'OldObservedQuirkMandatory':True,
 'PrimitiveSemanticsChanged':False,
 'DirectRule':'Unprotected ordinary function-body Expr.Call, valid builtin resolution, literal index>2^24-1; actual canonical element delegate selected and executed inside source frame.',
 'ForwarderRule':'One-clause distinct nonunderscore variables, no guard, direct builtin element body forwarding variables; literal out-of-domain caller argument mapped to index variable; actual analyzed terminal call/args/context/module observed under canonical native dispatch; actual identical result returned by helper.',
 'NativeOverrideGuard':'Canonical actual delegate+image stamped only by CoreModules; selected proof captured under same ModuleRegistry gate as actual dispatch. No before/after race, no dispatch-enum changes. Custom host Register/InstallPreloaded/source body lacks canonical identity. Current module NIF override disables forwarded proof.',
 'LexicalPartition':{
  'FunctionOrAnonymousBody':'eligible direct/forwarder rule',
  'GuardOrPatternExpression':'blocked',
  'TryExpressionBeforeOf':'blocked',
  'TryOfOrCatchBodyWithoutAfter':'inherits outer protection; own current handler inactive',
  'AnyTryWithAfter':'conservative block within region',
  'CatchOperand':'blocked',
  'NestedAnonymousBody':'fresh function return boundary',
  'Comprehension':'blocked; generated helper boundary requires separate analysis',
  'SharedASTAllowedAndBlockedOccurrence':'blocked',
  'DynamicIndexOrUnknownFunction':'no syntax proof, no transfer',
  'MultiClauseGuardedEffectfulRecursiveForwarder':'declined by one-hop summary'
 },
 'ErrorFramePreservation':'Actual BIF invoked before any return transfer; exception path unchanged. Normal result only transferred. Existing RuntimeTailCall deliberately not reused because it unwinds source frame/lease before BIF execution.',
 'ObservationLifetime':'Per async call-flow scope; exact target/args/context; locked state; late events rejected; dispose clears large-term/module/context/result fields and restores parent. No native handle/domain ownership.',
 'NoHardcodes':'No fixture/case/helper name or returned atom is present in production changes; only pinned native element/arity/type-bound contract and actual result.',
 'BaseFilesMatchCurrent':True,
 'SourceCount':14,
 'RequiredActualGates':['Root independent HOST proposal direct/forwarder, original-body override, realbadarg fullcaller/file/line, trybody/of/after, anonymous scope','Build complete shared solution and relevant existing continuation/source-call/override regressions','Replay old9582+new264 actual whole outcomes, record every strict/relational field without frame projection','Original new runtime-only6 remain exact; both direct and forwarded observed quirks6each close from actual outputs','Refresh reviewed source guards and current DLL/source manifests; no claim frozen earlier capture uses new binaries'],
 'NoExecution':True
})
files=[p for p in BASE.rglob('*') if p.is_file() and p.name!='source-freeze.json' and '__pycache__' not in p.parts]
save('source-freeze.json',{'Files':[{'Path':p.relative_to(BASE).as_posix(),'Sha256':h(p),'Bytes':p.stat().st_size} for p in sorted(files)]})
print(json.dumps({'CopyPlanSha256':h(BASE/'copy-plan.json'),'FreezeSha256':h(BASE/'source-freeze.json'),'ProofSha256':h(BASE/'source-proof.json'),'Sources':len(plan['Rows'])}))
