from pathlib import Path
import json, hashlib, re

root=Path(__file__).resolve().parents[2]
out=Path(__file__).resolve().parent
def read(p): return json.loads((root/p).read_text())
def digest(p): return hashlib.sha256((root/p).read_bytes()).hexdigest().upper()
def save(n,v): (out/n).write_bytes((json.dumps(v,indent=2)+'\n').encode())

old='docs/validation/part-371/evidence/erlang371-erlang-old-comparison1/rows.json'
oldrows=read(old)
length=[x for x in oldrows if x['Group']=='size_type' and (x['Name'].startswith('length_') or x['Name'] in ['guard_failure_length','non_tail_error_length'])]
assert len(length)==120 and all(x['Status']=='strict' and x['Proof'] is None for x in length)
baseline='docs/validation/part-345/published343-ordinary/artifact-12077509369/differential.json'
modules=[x for x in read(baseline)['ModuleResults'] if x['Name'].startswith('erlang-length-')]
assert len(modules)==9 and all(x['Passed'] and x['Expected']==x['Actual'] for x in modules)
current_sources=dict((k,json.loads(v)) for k,v in re.findall(r'public const string (\w+) = ("(?:[^"\\]|\\.)*");',(root/'tools/Erlang.Differential/ListLengthCaseSources.cs').read_text()))
assert len(current_sources)==9
assert {x['Source'] for x in modules}==set(current_sources.values())
baseord='docs/validation/part-371/evidence/ordsets371-fixed-comparison2/rows.json'
ordrows=[x for x in read(baseord) if x['Name'].startswith('size_')]
assert len(ordrows)==96 and all(x['Strict'] for x in ordrows)
neword='artifacts/part372-after-comparison2/rows.json'
newrows=[x for x in read(neword) if x.get('Cohort')=='ordsets' and x['Group']=='equality_size_edges' and x['Name'].startswith('size_')]
assert len(newrows)==126 and all(x['Strict'] for x in newrows)
save('selected-native-witnesses.json',{'Status':'SOURCE_SELECTED_EXISTING_NATIVE_EVIDENCE_NOT_FINAL_SOURCE_REPLAY','PinnedOtp':'ad05823719d77c8faee87348ea39513d4e2f99c5','Length':{'DirectValues':20,'FullStackProfiles':100,'Rows':length,'SourceRows':old,'SourceRowsSha256':digest(old),'AdditionalCompiledModules':modules,'ModuleSource':baseline,'ModuleSourceSha256':digest(baseline),'AllNineModuleSourcesMatchCurrentCSharpConstants':True},'OrdsetSize':{'Part371Values':16,'Part371FullStackProfiles':80,'Part371Rows':ordrows,'Part371SourceRows':baseord,'Part371SourceRowsSha256':digest(baseord),'Part372Values':21,'Part372FullStackProfiles':105,'Part372Rows':newrows,'Part372SourceRows':neword,'Part372SourceRowsSha256':digest(neword),'Part372ParentRun':39119539009,'Part372ParentConclusion':'failure','Part372CohortAndSizeRowsAdmittedSeparately':True,'NoParentRelabeling':True},'FiniteEvidenceIsNotWholeProof':True})

review=read('artifacts/length372-contract-proposal1/source-review.json')
guards={p:digest(p) for p in review['ReviewedMainSha256'] if p.startswith('src/')}
for p in ['src/Erlang.Runtime/RuntimeExceptionNames.cs','src/Erlang.Runtime/RuntimeExceptionCapture.Overrides.cs']:
    if (root/p).exists(): guards[p]=digest(p)
save('current-source-guards.json',{'Status':'CURRENT_REVIEW_SNAPSHOT_ROOT_MUST_REVALIDATE_AT_FINAL_HEAD','Files':[{'Path':p,'Sha256':h} for p,h in sorted(guards.items())],'TestGuards':[{'Path':p,'Sha256':digest(p)} for p in ['tests/Erlang.Tests/ListLengthHostChecks.cs','tests/Erlang.Tests/ListLengthHostNames.cs','tests/Erlang.Tests/ListLengthYieldContext.cs']],'NoBlindGuardRefreshPermitted':True})
scopes={
 'erlang:length/1':'Complete logical length contract for finite immutable Erlang lists: Nil zero, opaque-head Cons traversal, exact count, every non-Nil terminus badarg with original argument, native error_info and configured full caller stack. Includes ordinary/indirect calls, guards, dynamic pattern and receive contexts, separate non-trapping match-spec path, own-VM cooperative progress, cancellation and suspend/resume. No claim of identical physical memory, native scheduling quantum, timing or performance.',
 'ordsets:size/1':'Complete logical source contract size(S)->length(S), including unsorted/nonunique arbitrary opaque list elements, Nil and proper count, invalid and improper-tail original length error, ordsets.erl:120 source continuation and configured full caller stack, own-VM progress/cancellation/suspend/resume. Independent of term comparison, numeric equality, map construction/cardinality and raw cursor protocols. No physical VM/timing/performance identity claim.'
}
save('promotion-candidates.json',{'Status':'TWO_CANDIDATES_PENDING_ROOT_FINAL_GATES_NOT_A_REGISTRY_EDIT','Entries':[{'Mfa':m,'ProposedStatusAfterAllGates':'Verified compatible','Scope':s,'Proof':'artifacts/length372-contract-proposal1/README.md','ProofSha256':digest('artifacts/length372-contract-proposal1/README.md')} for m,s in scopes.items()],'RequiredFinalGates':['Actual four new HOST checks pass final source; existing lifecycle and receive timer checks retained','Strict final-source comparison preserves all120 length fields and126 current size fields plus retained96 size fields','Source guards independently reviewed at actual final source','Full final canonical, packaging and publication-head CI pass before published completion'],'PromotionDeltaOnlyIfAllGatesPass':{'erlang':1,'ordsets':1,'Total':2},'ExcludedFromCandidates':['erlang:map_size/1','erlang:size/1']})
save('freeze.json',{'Files':{str(p.relative_to(out)).replace('\\','/'):hashlib.sha256(p.read_bytes()).hexdigest().upper() for p in out.rglob('*') if p.is_file() and p.name!='freeze.json'},'NoMainChanges':True,'NoBuildTestNetwork':True})
print(hashlib.sha256((out/'freeze.json').read_bytes()).hexdigest().upper())
