# Source audit and candidate patch: ordsets NumericEquality and size

Status: artifact-only proposal. No Main edit, build, subject execution, network, CI or branch mutation. Part371 actual2616/2616 is baseline, not evidence that the new counterexamples pass. No full-compatibility promotion is claimed.

Pinned source bindings name eight exact blobs in ad05823719d77c8faee87348ea39513d4e2f99c5. `source-partitions.json` enumerates every source branch relevant to size and is_equal plus recursive term families; this is a source branch audit, not exhaustive input enumeration.

## Confirmed source gap and minimal candidate

Native ordsets:is_equal/2 guards is_list and evaluates arithmetic equality, not term ordering or exact equality. erl_bif_op.c seqeq_2 calls CMP_EQ; erl_utils.h CMP_EQ_ONLY calls erts_cmp(...exact=0,eq_only=1). utils.c flat-map numeric comparison traverses physical keys in native exact term key order, switching exact=1 for key phase and restoring exact=0 for values. erl_map.c sorts nonatom flat-map keys through erts_cmp_flatmap_keys -> erts_cmp(...exact=1,eq_only=0). Local FUN ordering uses module/index/OLD_UNIQ, while exact code identity uses NEW_UNIQ/arity.

Clean source-derived witness uses four distinct NEW_UNIQs to avoid native fun-entry interning:

- A:NEW00,OLD1
- B:NEWff,OLD2
- C:NEW40,OLD2
- D:NEW80,OLD1

All have same module/index/arity and zero captured variables. Native key order is left(A,B),right(D,C). Main canonical exact code order is left(A,B),right(C,D). Therefore `ordsets:is_equal([#{A=>0,B=>1}],[#{C=>1,D=>0}])` source-predicts true in native and false in present Main. This remains a prediction until actual native execution. erl_fun.c fun_cmp interns by module/index/arity/NEW_UNIQ, not OLD_UNIQ: reusing NEW_UNIQ for C/D would accidentally introduce a different intern-table state question. The final fixture uses allfour distinct values.

The proposed two-file production change reuses the iterative flat-map native-order view for NumericEquality, while carrying the incoming exact flag into values. The latter is essential: a nested map used as a key must not equate integer and float values. Existing ordinary comparison calls preserve their behavior. ExactEquality/canonical dictionary storage are deliberately untouched.

This is a narrow general primitive repair, not an ordsets-only fixture patch. Root should compare actual native controls, compile it, and run relevant equality/ordsets/maps/queue/proplists regressions before integration. It does NOT establish whole NumericEquality closure: exact ordering of nested mapkeys and HAMT hash-order alignment remain open. Do not broaden the conditional to every exact comparison without source review of native atom-index key array order.

`NumericMapEqualityHostChecks.Run()` is the permanent host runner proposal. It verifies clean witness bothdirections, list/tuple containment, wrong pairedvalues rejection, ordinary map numericvalues, nested map-key exactvalues and NumericEquality versus exactFUNidentity distinction. Root must register it once in the existing host runner; no Main file has been edited. Copy plan binds before/after source hashes.

## Size source closure

ordsets:size/1 is exactly `length(S)` at source line120. Its input need not be a set or sorted, and heads are never inspected. ErlangListLength.ReadAsync uses CDR traversal, original/tail/count state, release-mode sixteen cells per reduction, trap at remaining Cons, improper error before partial charging and partial floor(cells/16) accounting. Those branches match erl_bif_guard.c length_1/erlang_length_trap/erts_trapping_length_1. OrdsetCoreTrace.Length retains the original input in the length BIF frame plus the source size continuation and caller depths. No production size patch is warranted by this audit.

The41-case executable oracle fragment includes proper/improper lengths0,1,15,16,17,31999,32000,32001,64001 and raw atom/tuple/binary; ordinary error stack comparison must use default/0/1/2/8 depths through the existing admitted worker. These sizes straddle16-cell and fresh2000-reduction slices, while actual available budget can be less because worker setup consumes reductions. Do not equate absolute VM scheduler timings across implementations. Native DEBUG/VALGRIND chunk50 is a diagnostic compile path, not release semantic output. Cancellation/cooperative resource release is a separate own-runtime regression obligation.

## Oracle integration

`ordsets372_equality_size.erl` exports cases/0 and probe/1 only. It has41 names; Root/oracle helper adds the already admitted compiled support run/run_stacks wrappers using the same declared source metadata. No expected values are embedded. Every result is a bool, integer, simple tuple or ordinary error; no FUN is returned, so no new raw FUN decoding waiver is needed. Preserve all full stack fields. Existing support five depth profiles gives41values+205stack observations=246 new rows.

Dependency priority matrix names actionable next work: NumericEquality flat-map alignment first, exact nested map order next, own-state HAMT simultaneous traversal and stored reference-count aliases, reachable native-record field-order normalization, then large synchronous comparator fairness. Each has explicit gates; none may be marked complete by association with successful tests in another family.
