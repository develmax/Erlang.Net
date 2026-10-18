from pathlib import Path
import hashlib,json
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'artifacts/erlang371-ordsets-full-runtime-review1'
OUT.mkdir(exist_ok=True)
PROPOSAL=ROOT/'artifacts/ordsets371-full-proposal1'
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest().upper()
files=[{'Path':str(p.relative_to(ROOT)).replace('\\','/'),'Sha256':sha(p),'Bytes':p.stat().st_size} for p in sorted((PROPOSAL/'source/src/Erlang.Runtime').glob('*.cs'))]
assert len(files)==8
review={
 'Status':'INDEPENDENT_SOURCE_BRANCH_REVIEW_ACCEPTED_WITH_EXPLICIT_OPEN_COMPATIBILITY_GATES','PinnedCommit':'ad05823719d77c8faee87348ea39513d4e2f99c5','ProposalFreezeSha256':sha(PROPOSAL/'source-freeze.json'),'NoMainBuildNetworkOrGit':True,'SourceBindings':files,
 'BranchResults':[
  {'Area':'Binary set operations','Review':'Argument role switches in union/intersection/disjoint greater-head branch exactly preserved; subtract/subset deliberately retain roles. Numeric equal representative is current native left head after any previous swaps. Comparison-less bool short circuits and both empty/raw operand cases match pinned source. Prefix constructors preserve source head payload without reconstructing it.'},
  {'Area':'Malformed recursion and pending source frames','Review':'Final effective residual arguments, not original input lists, are used for function_clause. union stores branch-specific289/291/293 continuations; intersection stores349; subtract444; tail-only branches add none. Pending trace coalescing and emitted line numbers still require actual full-stack native equality; source resemblance is not an execution result.'},
  {'Area':'Delete','Review':'Greater branch stores original head, less returns residual set unchanged, equal returns its tail even if malformed, Nil returnsNil. Residual invalid argument is captured only after traversed comparisons. Prefix reconstruction preserves order.'},
  {'Area':'Intersection outer list','Review':'Singleton proper outer list returns arbitrary sole payload unchanged. Two+ computes inner intersection before inspecting outer remainder; public381 and helper385 continuations present on inner failure. Improper outer remainder emits helper384 effective accumulator/tail. Empty outer is function_clause380.'},
  {'Area':'Fold','Review':'Function arity2 guard precedes initial list case; first callback lists:foldl frame2465 then tail helper foldl_1 frame2470; accumulator updated exactly once per visited cell; malformed reached tail retains actual final accumulator. Exception/cancellation scopes unwind. No comparator or payload equality invoked.'},
  {'Area':'Filter','Review':'Arity1 guard precedes comprehension; exact true retains original input term, false drops, other return raises bad_filter rather than case_clause. Improper suffix raises bad_generator after preceding callbacks. Pending constructor only for kept elements. Synthetic comprehension name/arity/line must be verified by native packet.'},
  {'Area':'Map/filtermap','Review':'Temporary ordsets source continuation543/581 retained during list traversal, removed before tail from_list/usort. map stores every callback result; filtermap accepts true/false/{true,value} only, stores original or replacement appropriately and preserves keep/replace continuations2610/2612. Callbacks finish before any sorting.'},
  {'Area':'Delegated lists usort/umerge','Review':'from_list/union1 use actual ModuleRegistry.Call under removed ordsets source wrapper; respect real registered lists module overrides. Complete original helper/error metadata remains inherited and must be observed rather than inferred.'},
 ],
 'ConfirmedCompatibilityBlockers':[
  {'Area':'Dynamic lists code replacement','Affected':['ordsets:fold/3','ordsets:filter/2','ordsets:map/2','ordsets:filtermap/2'],'Source':'OrdsetFullCallbacks executes own optimized lists kernels directly after removing ordsets wrapper; it never selects actual registered lists foldl/filter/map/filtermap body. Pinned Erlang calls current ordinary lists module. Replacing lists functions changes native ordsets result, but this kernel remains old behavior.','RequiredClosure':'Either source-faithful corrected registered lists paths with actual dispatch, or atomically verify canonical body/image at dispatch and route genuine replacements through actual ModuleRegistry.Call. A before/after canonical check risks ABA and is insufficient. Preserve no-replacement optimized normal behavior.'},
  {'Area':'Arbitrary map/FUN key ordering','Affected':['ordsets:is_set/1','ordsets:is_element/2','ordsets:add_element/2','ordsets:del_element/2','ordsets:union/1,2','ordsets:intersection/1,2','ordsets:is_disjoint/2','ordsets:subtract/2','ordsets:is_subset/2','ordsets:from_list/1','ordsets:map/2','ordsets:filtermap/2'],'Source':'Current MapTerm canonical storage uses NEW_UNIQ while direct native FUN ordering uses OLD_UNIQ. Comparing maps with shared FUN keys but reversed key-order fields may compare different first value. Existing managed MapSteps zips canonical storage without native key-order worklist.','RequiredClosure':'Execute portable full-raw14-case candidate artifacts/ordsets371-map-fun-control-proposal1 against original/current Main. Native result decides exact protocol. No generic FUN projection/creator normalization; creator write preservation remains a visible producer gate.'},
 ],
 'NoOtherConfirmedLocalBranchBugFound':True,
 'Classification':'All13new APIs implemented after Rootintegration, but no additional full contract is granted by source-only review. First8pure3 remain conditional complete candidates; size requires separate length closure; callback4 require replacement dispatch plus complete source/actualerrorframes; ordering APIs require genuine comparator closure.',
 'LinkedProofs':['artifacts/erlang371-ordsets-full-source-audit1/contract-partitions.json','artifacts/erlang371-ordsets-full-source-audit1/required-controls.json','artifacts/erlang371-ordsets-independent-review1/review.json'],
}
(OUT/'review.json').write_text(json.dumps(review,indent=2)+'\n',encoding='utf-8')
(OUT/'source-freeze.json').write_text(json.dumps({'Files':[{'Path':'review.json','Sha256':sha(OUT/'review.json'),'Bytes':(OUT/'review.json').stat().st_size}]},indent=2)+'\n',encoding='utf-8')
print(json.dumps({'ReviewSha256':sha(OUT/'review.json'),'FreezeSha256':sha(OUT/'source-freeze.json')}))
