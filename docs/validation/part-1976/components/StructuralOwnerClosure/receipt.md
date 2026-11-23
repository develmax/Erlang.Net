# Part 1972 missing local helper audit

The full uncached Part 1972 function rank has 3,192 missing pinned keys across 365 modules. The topology script joined each affected generated image via the frozen `all-final-label-paths.tsv` and compared actual LocT and ExpT keys with pinned OTP 29.1.1. The two frozen input hashes are in the receipt. Group totals are physical key counts. Owner counts are distinct release module/function/arity identities.

| Group | Missing keys | Modules | Owners | Observation |
|---|---:|---:|---:|---|
| Plain list comprehension lc$^ | 1,880 | 310 | 987 | 1,795 have no emitted same-owner family counterpart, 21 have the same symbol at another arity, 40 have a same-owner and arity peer, 24 have a same-owner peer at another arity. |
| Anonymous closure naming family | 638 | 51 | 454 | Classify after joining the same-owner fun spelling, which can represent the corresponding closure. |
| Explicit fun family | 274 | 71 | 104 | 238 have the same emitted symbol at a different arity. |
| Owner-qualified inlined family | 109 | 70 | 98 | Distinct from list-generator capture and traversal. |

`topology.stdout.log` holds complete per-key rows and exact counts. Of the plain list helpers, 1,665 uniquely join the pinned BEAM's first helper line to its raw abstract comprehension or generator annotation. Those joined rows cover 300 modules and 896 owners. Shapes can overlap. A context marked effect also includes used assignments in a sequence, so it cannot prove a discarded result.

| Source subset | Keys | Modules | Owners |
|---|---:|---:|---:|
| Multiple generators | 319 | 57 | 132 |
| Exactly two ordinary generators before other source restrictions | 239 | 52 | 105 |
| Two unfiltered variable or flat-variable-tuple generators and static operands | 56 | 17 | 24 |
| Two unfiltered variable generators and static operands | 38 | 11 | 16 |
| Variable mapper call with one ordinary generator and an owner containing a sibling fun | 127 | 52 | 74 |
| Remaining direct-left append occurrences | 99 | 52 | 77 |
| Exact wildcard discard | 40 | 27 | 33 |

## Candidate 1. Reuse structural-head scheduling with a sibling closure

The five-key cohort is in `closure-structural-census.stdout.log`. The two representative releases and their actual Part 1972 counterparts are in `inspect-closure-siblings.stdout.log`. Both `asn1ct_check:categorize/1` and `asn1ct_gen:gen_exports/3` retain a matching closure key while looping inline in the parent and missing pinned `lc$^1/1-0-/2`. The former uses a tuple/record dispatch with a closed callback. The latter uses nil and cons-alias clauses with a captured callback. Additional scope images are `diameter_dict_util:opt/2` and `dialyzer_typesig:v2_solve_conj/12`. Their closures also have the expected keys. Treat `beam_core_to_ssa:partition_intersection/3` as exploratory because it also differs in anonymous and inlined ownership. The extra release disassemblies and pinned abstract clauses are in `inspect-closure-other.stdout.log`.

Pinned OTP `v3_core.erl` lines 276, 1822 and 3077–3084 assign independent Core function ordinals while visiting source. `beam_core_to_ssa.erl` lines 509, 1257–1262 and 2561–2587 allocate SSA letrec names and visit constructor alternatives separately. The existing C# constructor-head fallback can already represent aliases and tuple/record structure. This proposal also uses it for one closure and one list comprehension when both supported definitions are in a single unguarded owner clause. It leaves the existing cardinality, optimizer and constructor checks in the path. The prior closed-vs-captured callback classifier runs after successful helper construction.

The two pinned fixture helper pairs are ordered comprehension then closure, with a direct call for the closed target and a dynamic call for the retained capture. The native fixture passed seven observations, including the improper-tail `bad_generator` helper frame, default clauses and the public `function_clause` frame. The permanent test extends the existing closure mapping test. SDK confirmation should include those tests, the four named releases, the exploratory case separately and previously matching entries with mixed closure/comprehension ordering.

## Candidate 2. Computed first source for two variable generators

Only `dialyzer_typesig:vars_per_child/2` qualifies under the existing unconditional owner-entry and scoped-operand rules. It is missing pinned `lc$^0/1-0-/2` and `lc$^1/1-1-/4`. The apparently similar `dialyzer_incremental:return_value/2` has an independently unsupported record-alias parameter. Both pairs are in `inspect-computed.stdout.log`.

The existing specialized `ExtractNestedListGenerators` admits only an incoming variable or a closed list literal for the first source. The more general `ExtractRecursiveListGenerators` currently permits a static computed first source only with a two-field tuple pattern, even though its shared parent call already evaluates the source as argument zero. The draft permits it for a variable pattern, uses the existing lexical operand traversal and leaves the existing tuple, strict, discarded and multiple-sibling rules in place. It preserves apply operands on the producer path during the existing literal substitution. Pinned `v3_core:lc_tq1/5` and the real BEAM pair corroborate outer source evaluation before the first local call and inner source evaluation in the accepted outer head.

Pinned native fixtures covered dependent and independent second generators, order of effects, empty first input, both invalid source failures and their helper identities. The permanent test checks physical helper order and arity in the existing nested generator suite. Test also `dialyzer_incremental` as an exclusion and the accepted nested and tuple generator probes if linked.

## Strict replay and scope

Both independent proposals replay against checked archive entries in the exact Part 1973 edge retake source freeze 3a834f1bd054df421f7d563178599b2bd4cf868dfd0c475ee1cb810e42e20cfa. `source-stage.stdout.log` and `structural-closure-stage.stdout.log` retain verified preimage hashes. Both strict replay error streams are empty. The candidate sources require the scoped and full compiler validation gates before acceptance. No candidate-mediated output, whole-file equality or no-loss result is established here. These low-five-key mechanisms are bounded subsets of the larger shape counts, which still need owner/capture/protection classification. No root staging source or reference OTP checkout was updated.

Frozen rank input SHA256 `33247517412b37c254ba971266f335a9597f06e1b19673bd218c84a2ae3b1db7`. Frozen generated image path map SHA256 `50ed27f08a7412b883c88c43f62d15c8d3b9f9996cb88642f9e17e988058d033`. The BEAM comparison is diagnostic and does not supply compiler output bytes.
