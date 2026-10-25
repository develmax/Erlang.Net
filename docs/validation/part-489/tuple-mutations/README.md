# Part489 — tuple construction and mutation contracts

Pinned OTP-29.1.1, ad05823719d77c8faee87348ea39513d4e2f99c5. Complete source/type/bounds/reference/effect audits close setelement/3, append_element/2, insert_element/3, delete_element/2, make_tuple/2, make_tuple/3 and list_to_tuple/1 on the supported valid Erlang term model. Production source is unchanged; no whole compiler/module, physical allocation-ceiling or timing equivalence is claimed.

## Source closure

Pinned bif.c and erl_term.h define the authorities. The native max arity used by append/make/list conversion is `(1<<24)-1`, exactly16777215 in ErlangTupleLimits. Any valid index accepted by a supported tuple is smaller than the native small-integer range; managed arbitrary-precision range checks precede Int32 narrowing.

- setelement validates tuple/index1..arity, copies the tuple and replaces exactly the chosen field. Both source paths preserve untouched element references and do not mutate the input.
- append_element validates tuple and the explicit native arity bound, copies fields in order and appends the actual supplied value reference.
- insert_element validates tuple/index1..arity+1 and inserts between preserved prefix/suffix fields. Pinned insertion does not contain append's extra arity-refusal branch; no extra rule is invented in the managed audit.
- delete_element validates tuple/index1..arity, preserves surviving fields and returns canonical empty storage when deleting the only element.
- make_tuple validates integer size0..16777215 and repeats the actual default reference. Zero produces canonical empty storage. The three-argument version then validates a proper initializer list of two-tuples and indices1..size, applying entries left-to-right so the last duplicate wins. Pinned source also allocates/fills before checking initializers; the managed order follows it.
- list_to_tuple validates a finite proper list and the same maximum count, then copies its references in order. Improper/nontype/oversize cases raise badarg; nil yields canonical empty storage. Early managed refusal after the maximum count and native complete length scanning produce the same pure error and original argument metadata, with no callback or input mutation.

TupleTerm owns an immutable array; list traversal reads sealed Cons cells. ToList/ToArray and the constructor copy references, not guest values. No user iterator, callback or comparison is invoked. Source hashes and every partition are recorded in source-audit.json. These arguments establish the general contracts; finite witnesses alone are not the proof.

## Original comparison and bounded regression groups

All344 original outcomes and1720 complete stack outcomes match (2064 whole comparisons), including invalid types/indices, large integer bounds, zero/ordinary sizes, duplicate initializer order and malformed initializer lists. Function atoms pass through the same opaque runtime barrier. Original dispatch-proof.etf records actual apply instructions. All6427 provider files match before/after and every owned original process settles normally. No field normalization or post-capture projection is applied.

The first aggregate local group exceeded its unchanged15-second outer budget after15083ms. It remains a failed run in initial-aggregate-timeout.json. Split the same cases by seven APIs; each gets its own unchanged15-second budget and existing75-second worker fence. The seven final groups pass in3793ms total measured test time, maximum1473ms. This is an observed validation-organization result, not a controlled runtime-performance benchmark or a claim about the censored aggregate's true completion time. Original data is reacquired for the actual separated subjects; case labels, values, native errors and full source/caller stacks remain complete.

Local readiness is explicitly composite8906: the parent's8899 catalog plus seven new passing groups, with production sources unchanged. It does not assert a continuous8906 final-source run. Exact publication-head CI must run that full catalog and cold packages before delivery. The generic collector now accepts an explicit subject and golden-file prefix, so independent groups share existing admission/ownership/serialization code rather than adding per-API runners. CI aggregates independent capture failures.

Registry708 unchanged. Verified130->137, pending553->546, known25 unchanged. Original559 cohort19 closed/540 remaining (3.40%), plus six additional full audits. erlang40 verified/298 pending/13 known of351; other module counts unchanged. The broad requested work is still unfinished. LICENSE.local.txt is excluded.
