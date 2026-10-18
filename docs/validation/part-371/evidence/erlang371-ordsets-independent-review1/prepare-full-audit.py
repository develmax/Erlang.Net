from pathlib import Path
import hashlib,json
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'artifacts/erlang371-ordsets-full-source-audit1'
OUT.mkdir(exist_ok=True)
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest().upper()
def dump(p,o): p.write_text(json.dumps(o,indent=2)+'\n',encoding='utf-8')
src=ROOT/'artifacts/next-api371-proposal1/ordsets.erl.pinned.txt'
rows=[
 ('from_list',1,194,'Unconditional tail delegation to lists:usort/1. No ordsets argument guard. Correct raw argument/error/caller behavior belongs to usort, no extra ordsets continuation on failure. Sorting/unique representative behavior and arbitrary term ordering must match pinned usort, not an abstract mathematical set.',True),
 ('del_element',2,263,'Cons branches test greater then less then numeric equivalence; greater preserves head in pending non-tail continuation; less returns original residual Set; equal returns residual Tail unchanged; Nil returns Nil. Invalid residual raises function_clause at263 with [E,residual] plus pending263frames.',True),
 ('union',2,288,'Both Cons: less pushes E1 and recurses(Es1,Set2); greater pushes E2 and recurses(Es2,Set1), deliberately swapping roles; equal pushes E1 and recurses(Es1,Es2). First Nil returns second raw term; second Nil returns first raw term. Neither-base invalid patterns function_clause288; pending288constructors retain frames. Raw invalid branch must not eagerly validate both list spines.',True),
 ('union',1,318,'Unconditional tail delegation to lists:umerge/1; preserve complete umerge helper/error behavior. It is not repeated abstract union fold; pair grouping can change numeric representatives, raw argument failure and remaining helper frames.',True),
 ('intersection',2,344,'Both Cons: less tail-recurse(Es1,Set2); greater tail-recurse(Es2,Set1), swapping roles; equal pushes E1 and recurses(Es1,Es2), pending constructor frame at349. Either Nil returns Nil regardless other raw term. Otherwise function_clause344 effective residual arguments.',True),
 ('intersection',1,380,'Two-or-more Cons heads: compute intersection(S1,S2) then call intersection1(result,Ss); singleton proper list returns its sole raw S unchanged. Nil/nonlist/one improper Cons fail public line380. intersection1 tail-repeats intersection(acc,S2), Nil returns accumulator; improper tail fails helper384 [acc,tail]. Compute inner intersection before testing outer remainder, preserving error priority and effects.',True),
 ('is_disjoint',2,412,'Both Cons: less tail-recurse(Es1,Set2); greater tail-recurse(Es2,Set1), switching roles; numeric equivalent heads false. Either Nil true regardless other raw term. Otherwise function_clause412 with actual swapped/residual args. No pending source frames for tail scans.',True),
 ('subtract',2,443,'Both Cons: less pushes E1 and recurse(Es1,Set2); greater tail-recurse(Set1,Es2), NO argument switch; equal tail-recurse(Es1,Es2). First Nil Nil; second Nil returns raw first term. Malformed residual function_clause443; pending preserved less-branch sourceframe444.',True),
 ('is_subset',2,473,'Both Cons: less false immediately without inspecting suffix; greater tail-recurse(Set1,Es2), NO argument switch; equal tail-recurse(Es1,Es2). First Nil true, second Nil false. Otherwise function_clause473 effective residual args. Early false may avoid improper suffix error.',True),
 ('fold',3,501,'Unconditional tail delegation to lists:foldl(F,Acc,Set). Compare exact callback arity/type guard, empty/nonlist/proper/improper handling, callback argument order [Element,Accumulator], once-only left-to-right effects, arbitrary accumulator identity, actual error class/reason/effective args and helper source frames. No ordsets tail frame should survive.',False),
 ('filter',2,521,'Unconditional tail delegation to lists:filter(F,Set). Callback must accept arity1. It must run once per reached element in order, require exactly boolean true/false, retain original accepted term identity and handle malformed tail only after preceding callback effects. Full badfun/badarity/case_clause/function_clause frame distinctions are mandatory.',False),
 ('map',2,542,'Non-tail lists:map(F,Set) first (ordsets:map continuation at543); on success tail from_list/1 ->lists:usort/1. All callbacks complete before sorting. Duplicate mapped values choose native usort representative, sorting may fail after callback effects. Reject wrong callback/list exactly as lists:map; do not eagerly enforce proper list or function before native source does.',True),
 ('filtermap',2,580,'Non-tail lists:filtermap(F,Set) first (ordsets:filtermap continuation581); on success tail from_list/1->lists:usort/1. Callback true retains input element, false drops, {true,Value} replaces, every other return raises native lists helper error. Complete callback traversal precedes usort, no premature sorting/dedup.',True),
]
audit={'Status':'PINNED_SOURCE_PARTITION_AWAITING_FINAL13_IMPLEMENTATION_AND_ACTUAL_COMPARISONS','PinnedCommit':'ad05823719d77c8faee87348ea39513d4e2f99c5','PinnedPath':'lib/stdlib/src/ordsets.erl','PinnedSha256':sha(src),'ExistingEightIndependentReview':'artifacts/erlang371-ordsets-independent-review1/review.json','Remaining13':[{'Mfa':f'ordsets:{n}/{a}','Line':l,'CompleteBranchObligation':s,'OrderingOrNumericEqualityDependency':c,'EligibleNow':False} for n,a,l,s,c in rows],'NoOriginalOrMainExecution':True,'NoMainEdits':True,'NoWholeModulePromotionFromPresence':True,'PotentialAdditionalComparisonFreeContracts':['ordsets:fold/3','ordsets:filter/2'],'PotentialAdditionalRequirements':'Exhaustive delegated lists source graph and error/frame/callback effect proof, strict actual final Main comparison. Existing lists Needs verification does not automatically prohibit or establish a narrow independently proven wrapper contract.'}
dump(OUT/'contract-partitions.json',audit)
controls=[
 ('union_role_switch_numeric','ordsets:union([2.0],[1,2])','union/2'),
 ('intersection_role_switch_numeric','ordsets:intersection([2.0],[1,2])','intersection/2'),
 ('union_role_switch_residual_error','ordsets:union([2.0|left_tail],[1,3|right_tail])','union/2'),
 ('intersection_role_switch_residual_error','ordsets:intersection([2.0|left_tail],[1,3|right_tail])','intersection/2'),
 ('disjoint_role_switch_residual_error','ordsets:is_disjoint([2.0|left_tail],[1,3|right_tail])','is_disjoint/2'),
 ('union_nil_raw','ordsets:union([],raw)','union/2'),
 ('union_raw_nil','ordsets:union(raw,[])','union/2'),
 ('intersection_nil_raw','ordsets:intersection([],raw)','intersection/2'),
 ('subtract_raw_nil','ordsets:subtract(raw,[])','subtract/2'),
 ('subset_nil_raw','ordsets:is_subset([],raw)','is_subset/2'),
 ('intersection_singleton_raw','ordsets:intersection([raw])','intersection/1'),
 ('intersection_outer_improper_after_inner','ordsets:intersection([[a],[a]|outer_tail])','intersection/1'),
 ('fold_invalid_callback_empty','ordsets:fold(invalid,seed,[])','fold/3'),
 ('fold_wrong_arity_empty','ordsets:fold(fun(X)->X end,seed,[])','fold/3'),
 ('fold_raw_list','ordsets:fold(fun(E,A)->{E,A} end,seed,raw)','fold/3'),
 ('filter_invalid_callback_empty','ordsets:filter(invalid,[])','filter/2'),
 ('filter_wrong_arity_empty','ordsets:filter(fun(X,Y)->{X,Y} end,[])','filter/2'),
 ('filter_nonboolean','ordsets:filter(fun(_)->{true,replacement} end,[value])','filter/2'),
 ('filtermap_invalid_shape','ordsets:filtermap(fun(_)->{true,value,extra} end,[value])','filtermap/2'),
 ('map_callback_error','ordsets:map(fun(_)->erlang:error(callback_failure) end,[value])','map/2'),
 ('filtermap_callback_error','ordsets:filtermap(fun(_)->erlang:error(callback_failure) end,[value])','filtermap/2'),
 ('fold_callback_error','ordsets:fold(fun(_,_)->erlang:error(callback_failure) end,seed,[value])','fold/3'),
 ('filter_callback_error','ordsets:filter(fun(_)->erlang:error(callback_failure) end,[value])','filter/2'),
]
dump(OUT/'required-controls.json',{'ExpectedOutcomesSeeded':False,'MandatoryFullFieldErrorComparison':True,'Controls':[{'Name':n,'Expression':e,'Mfa':'ordsets:'+m} for n,e,m in controls],'AdditionalOpaqueTermControls':['Nested maps with multiple distinct FUN keys whose same module/index OLD_UNIQ ties but NEW_UNIQ differs; preserve exact key identity despite comparison tie.','Multiple FUN keys with reversed NEW_UNIQ vs OLD_UNIQ order; inspect map comparison rather than assuming canonical key storage order equals native comparison order.','Native large map retaining equal reference aliases with differing externally encoded count1/2/5; preserve own-state HAMT protocol, do not change reference equality globally.','Real local source closures and external FUNs in payloads/keys; own hosting FUN code identity must have genuine provenance, never borrowed native pointer IDs.','Unaligned bitstrings, native record schema order, all signed integer/float ties and negative zero in nested payloads.','Callback side-effect ledger for improper inputs records reached prefix and exact error; preserve full raw capture, no dropped error field.']})
dump(OUT/'source-freeze.json',{'Files':[{'Path':p.name,'Sha256':sha(p),'Bytes':p.stat().st_size} for p in sorted(OUT.glob('*.json')) if p.name!='source-freeze.json']})
print(json.dumps({'Rows':len(rows),'Controls':len(controls),'FreezeSha256':sha(OUT/'source-freeze.json')}))
