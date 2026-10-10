# OTP source port register

Baseline: **OTP-29.1.1**, commit `ad05823719d77c8faee87348ea39513d4e2f99c5`. The sibling checkout remains read-only. Part 048 changes the implementation policy: use source-directed ports for missing or divergent algorithms, preserving evaluation order, state transitions and error construction. Document CLR adaptations explicitly. Existing code is not retrospectively classified as a direct port.

## First structural compiler audit

| Area | Pinned source | Current implementation and finding | Disposition |
| --- | --- | --- | --- |
| Expression grammar | `lib/stdlib/src/erl_parse.yrl`, ordinary productions, `inop_prec`, `preop_prec` | A handwritten Pratt parser implements relative ordinary precedence; OTP uses generated grammar. Missing records, comprehensions, try and maybe remain substantive gaps. | Retain working subset; next grammar additions must map productions and binding powers before coding. No claim of parser port or complete grammar. |
| Tuple/call argument scopes | `erl_eval:expr_list/5,8`; `erl_lint:expr_list/3`, `vtupd_export_expr_list/4` | Sequential shared dictionaries/sets wrongly allowed sibling variables and returned a different tuple conflict reason. | Ported common-input evaluation, per-child binding merge, conflict short-circuit, and independent static checking in this part. |
| List construction scopes | `erl_eval:expr/6` cons clause; `erl_lint:expr/3` cons clause | Flat AST representation obscured nested cons evaluation and binding merges. | Ported head/tail isolation and inner-to-outer merge order; iterative adaptation avoids CLR recursion. |
| Binary/short-circuit expressions | `erl_eval:expr/6` operator clauses and `merge_bindings/4`; `erl_lint` operator clauses | Part 046 already isolated strict operands, preserved short-circuit left scope and marked RHS exports unsafe. | Audited correspondence; existing routines are contract-driven implementations, not newly ported here. Full traces and variable-table attributes remain absent. |
| Begin/catch/if | `erl_eval:exprs`, catch clause, `if_clauses`, `guard0/guard1`; `erl_lint` block/catch/branch clauses | Begin sequences and catch rollback resemble source control flow; guards are stored as expression trees, and branch states are name sets with unsafe markers. | Preserve tested behavior; replace approximations incrementally. Source annotations, full stacks, warning/export/usage states are unfinished. |
| Maps | `erl_eval:eval_map_fields` and map clauses; `erl_lint:map_fields` | OTP isolates the map base from the field sequence, then evaluates field keys/values sequentially. Lint checks keys/values/fields from independent input scopes. Current base and fields share runtime bindings, and static checking is sequential. | Confirmed base/static scope gaps; next binding port. Sequential field execution already follows erl_eval and must be retained. |
| Bit construction | `erl_eval` bin clause, `eval_bits:expr_grp1/eval_field`; `erl_lint` bin clauses | Static isolation and sequential evaluation are distinct source contracts: eval_bits passes updated bindings through values/sizes/segments, while lint restricts sibling references. A shared runtime dictionary alone establishes no divergence. | Audit deferred binary creation/error timing and compiler lowering before changing callbacks. Preserve static size dependency distinctions and historical fixtures. |
| Compiled execution | OTP compiler pipeline versus generated C# AST interpreted by `Execution` | Sharing an evaluator does not reproduce BEAM optimizations, stack frames or every compiled error choice. | Keep both interpreted and optimized compiled-reference tracks; preserve mismatches rather than changing expectations to fit the port. |

This audit covers the listed compiler paths, not all runtime/OTP components. It is a prioritized source comparison, not a proof that all remaining bugs have been identified.

Part 049 dated correction: the initial part-048 table overgeneralized map/bit scope isolation from the static checker. Reading the complete eval_map_fields/expr_grp1/eval_field routines distinguishes sequential execution from independent static checking. The corrected rows above supersede that description; immutable part-048 source reports and packages are retained. No map/bit executable change follows from this clarification.

## Port 048: expression-list and cons binding algorithms

Destination: `src/Erlang.Compiler/ExpressionBindings.cs`, called by `Execution` and `Semantics` for tuples, lists, static calls and dynamic calls.

- `EvaluateList`: adaptation of `erl_eval:expr_list/5,8`. Snapshot the input; evaluate children in source order with independent copies; merge each child's bindings into the accumulated bindings before continuing; publish successful bindings. The source's reverse value accumulator becomes a CLR array indexed in source order.
- `Merge`: adaptation of the ordered-dictionary branch of `erl_eval:merge_bindings/4`. Fold the first environment into the second; compare values exactly; on conflict return `{badmatch,ExistingSecondValue}`. Iterate variable names using Erlang atom/codepoint ordering. This register does not claim equivalence to all map-environment error iteration choices.
- `EvaluateCons`: adaptation of `erl_eval:expr/6` for cons nodes. Evaluate all heads and the explicit tail from the original environment in source order, then merge scopes from the innermost cons outward. The flat AST is reconstructed iteratively; the tail remains an arbitrary term. Side effects are retained even if an outer merge subsequently fails.
- `ValidateList`: adaptation of the independent-input walk in `erl_lint:expr_list/3` and export update. CLR name sets retain the current subset's bound/unsafe information; complete variable-table state, usage counts, annotations and warnings are not ported. Dynamic-call static checking includes the function expression among siblings, as in `erl_lint`.

The async callback is a CLR adaptation, not concurrent execution. Children execute left to right. Process reductions, allocations, complete source stack frames and optimizer behavior are outside this port's equivalence claim.

Provenance: [part-048 source audit](validation/part-048/reference-audit.json) records pinned blob IDs, actual line ranges and consulted routine bodies. Historical selected OTP evidence is 484/484 at `b7b8c72`; the enlarged corpus must be verified separately at the new implementation head.

## License and redistribution

The adapted routines originate in Ericsson's Apache-2.0 files `lib/stdlib/src/erl_eval.erl` and `lib/stdlib/src/erl_lint.erl`, copyright Ericsson AB 1996–2026. The destination retains SPDX, copyright and license text, and identifies modifications. [OTP-LICENSE.txt](OTP-LICENSE.txt) retains the full upstream license; this register accompanies local packages. No root `NOTICE` or `NOTICE.txt` occurs in the pinned source tree (checked with `git ls-tree`); the source-file notices above are preserved.

This provenance does not select a public license for the repository's unrelated original code, import unreviewed third-party implementations or authorize public package publication.
