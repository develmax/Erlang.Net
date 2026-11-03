# ordsets dependent helper review

Reference: OTP-29.1.1, ad05823719d77c8faee87348ea39513d4e2f99c5.
Reference files were read with `git -C ../otp show OTP-29.1.1:...`.
This review accompanies the atomic-native-caller handoff. It does not change
the compatibility ledger or replace final-source validation.

## Recursive state correspondence

Reviewed the 25 ListUniqueMergeState cases against lists.erl:2075–2123 and
3217–3493. The C# loop is a trampoline: a state and its argument vector denote
one Erlang helper invocation. Ordered pattern tests implement ordered clauses;
state/argument replacement implements a tail call. Cons construction preserves
the selected source representative, including numerically equal distinct terms.
Each consumed spine step is reduced through the existing runtime scheduler.

| State family | Pinned source | Reviewed obligations |
| --- | --- | --- |
| usort/1, usort_1/2 | 2075–2123 | All short-list clauses; strict ascending/descending selection; numeric duplicate elimination; arbitrary improper tail falls through to the appropriate source helper |
| usplit_1/5, usplit_1_1/6 | 3217–3247 | Guard order `>Y`, `==Y`, `>X`, `==X`, saved-run `>S`, `==S`; exact argument and run construction |
| usplit_2/5, usplit_2_1/6 | 3250–3280 | Dual descending guards and saved runs; descending terminal dispatcher |
| umergel/1, umergel/3, rumergel/3 | 3284–3321 | Empty/run-skipping clauses before multi-run dispatch; asc/desc operand permutation; all non-tail resumes |
| umerge3_1/7, umerge3_2/7 | 3326–3351 | First/second-spine consumption; numeric HdM guards; transition to two-way merge on exhaustion |
| umerge3_12/8, umerge3_12_3/6, umerge3_21/8, umerge3_21_3/6 | 3354–3383 | Third-spine consumption; <= guards before duplicate guards; exact result prefix and two-way argument order |
| rumerge3_1/6, rumerge3_12a/7, rumerge3_2/7, rumerge3_12b/8, rumerge3_12_3/7, rumerge3_21_3/7 | 3386–3451 | Ordered <= and numeric duplicate clauses; retained H2M/H3M; two-element prefix order; all exhaustion paths |
| umerge2_1/5, umerge2_2/4, rumerge2_1/4, rumerge2_2/5 | 3455–3493 | First-list representative priority; duplicate guard ordering; all six native reverse arguments and call PCs |

All state function names, arities and clause-start lines in ListUniqueMergeNames
match the pinned helper. Missing patterns fail through Clause with the current
argument vector rather than normalizing an improper input.

## Continuations and reverse dispatch

The ten non-tail sites are 3294, 3302, 3304, 3306, 3308, 3311, 3313, 3315,
3317 and 3319. Each stores the caller PC, target state, result argument index and
saved accumulator. On completion the trampoline inserts `Cons(result, savedAcc)`
into that target slot. The two reverse sites with a pending continuation resume
with `[reverseResult | Acc]`; the other eight sites resume after a merged run.
Stack enumeration reverses the pending stack for outer-to-inner scope entry and
disposes inner-to-outer. Tail transitions do not accumulate caller scopes.

The six terminal reverse PCs are 3462, 3464, 3471, 3481, 3491 and 3493. Their
arguments match the source, including the distinct duplicate/nonduplicate tails.
The separate native-marker query was a genuine defect; only the four-file atomic
selection delta closes it. Scope choice and selected body now share one registry
selection. No callback executes under the registry gate. Native success, failure,
yield, hibernate, factory cancellation and replacement controls are in the handoff.

## ordsets-level correspondence

Reviewed OrdsetCoreExecution, OrdsetFullExecution and OrdsetFullCallbacks against
the complete pinned ordsets.erl clauses. Strict order, comparison-zero selection
and numeric structural equality remain separate operations. Binary union,
intersection and disjointness swap operands on the greater-than branch exactly
as the source does; subtraction and subset advance only the second spine.
Empty clauses retain wildcard arguments instead of imposing a proper-list check.
Intersection/1 returns an arbitrary singleton head and uses its two distinct
non-tail PCs for initial and subsequent intersections. Add/delete and binary
prefix construction retain their pending source continuation when failing.

from_list/1 and union/1 dynamically tail-dispatch to lists:usort/1 and umerge/1.
fold/3 and filter/2 dynamically tail-dispatch to lists. map/2 and filtermap/2
retain their ordsets callback-call PC until lists completes, then tail-dispatch
through from_list. They do not invoke the next phase if the callback phase fails.
The actual selected lists binding remains authoritative.

## Callback and stack partitions

ListCallbackSourceExecution was also compared with pinned lists.erl:2382–2391,
2463–2473,2526–2527 and2604–2617. The outer function-arity guard precedes any list
inspection, including empty or invalid input. map/foldl distinguish an invalid
initial list (case_clause) from an improper recursive remainder (function_clause).
filtermap tail-enters its helper before testing the list. It accepts exactly true,
false or a two-tuple with first element true; other callback values cause
case_clause before inspecting the remainder. filter's comprehension instead uses
bad_filter and bad_generator. Each callback executes left-to-right before the
remaining spine is inspected. Failed callbacks do not trigger sorting.

Map stores a non-tail result continuation for every element. Filtermap stores
distinct keep/replace PCs, and no continuation for false. Filter stores its
comprehension cons-return identity only for kept values. Foldl tail-resumes at its
helper PC. Callback invocation uses InvokeGuestFunction507 for the selected arity
and retains the existing owned invocation/control transport. No replacement
binding is bypassed by pre-validating the callback inside ordsets itself.

OrdsetFullTrace deduplicates consecutive identical continuation identities and
retains at most the runtime maximum depth, keeping the innermost suffix. Scope
entry is outer-to-inner and disposal is inner-to-outer. RuntimeExceptionCapture
performs the final configured-depth truncation. OrdsetCoreTrace follows its
existing minimum-one-frame capture protocol and emits the extra pending add
continuation only when configured depth admits it. This review establishes the
source continuation correspondence; it does not claim a new oracle run for every
depth/trace/callback mutation partition.

## Evidence and remaining acceptance

The accompanying broad-replay-summary.json records both full 12,247-row corpora,
36,100 ordered term pairs and ten guest API probes per backend, plus 54 native,
HAMT, exact-equality and source-frame controls against the owner's source base
with the atomic four-file overlay. These are finite differential witnesses,
not a universal proof obtained from their count.

The state correspondence above is conditional on the admitted shared term-order,
numeric-equality, callback/exception and runtime ownership contracts. Completing
their source-domain audit, accepting this helper review in the canonical source
plan, and passing full final-source CI and post-merge regression remain gates for
21/21. No additional third-party dependency or upstream modification is proposed.
