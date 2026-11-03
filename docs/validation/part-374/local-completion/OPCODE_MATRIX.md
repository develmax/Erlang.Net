# Stage374 measured instruction coverage

Authority: exact pinned OTP-29.1.1 source, commit ad05823719d77c8faee87348ea39513d4e2f99c5. Initial unchanged installed lists image contains8343instructions,52opcode names and93exports. The seven initially refused role families below are now implemented. This is not a catalogue-wide execution claim or an instruction hit trace.

| Role | Runtime semantics implemented | Focused execution evidence | Limits |
|---|---|---|---|
| bif2 | Import arity/native validation; source snapshots; success-only destination write; body error versus guard branch | element/2 source/destination alias, actual badarg arguments/error_info, failed guard preserving Y sentinel | No exhaustive proof for every possible native import |
| call_fun | Callable X(arity), ordinary frame/continuation, arbitrary callable, actual badfun/badarity | Passed callback arguments/result continuation and two invalid callable cases; higher-order lists scenarios | Not every environment/arity/resource partition |
| is_eq | Existing recursive arithmetic equality, preserving exact map-key contexts | Compound integer/float equality and same-term branch | Shared equality source/regressions retained; no physical VM identity claim |
| is_ne_exact | Negated exact equality | Compound integer/float distinction and exact same-term failure | No per-site execution trace |
| is_float | FloatTerm only, branch without writes | Float success/integer failure | Not every malformed failure-target shape |
| has_map_fields | Nonempty keys, literal/unique multi-key validation, exact key lookup, no destination write | Integer versus float key lookup/miss; malformed empty/duplicate/dynamic multi-key refusals | Arbitrary nonmap bytecode violates the source type precondition; no maps BIF badmap contract claimed |
| put_map_assoc | Failure0/even nonempty operands; immutable exact-key updates; read snapshots; live roots/Y initialization | Source/value/destination alias, original map persistence, distinct integer/float keys; malformed operand/GC-root refusals | No physical native GC/allocation or exhaustive malformed-image compatibility claim |

Source semantics and exact pinned anchors: artifacts/lists374-seven-opcode-semantics1. Fresh production review: source-review.json,52bound sources/all33changed production paths. Focused edge/native/caller/reload execution: focused-edge-tests.json. Whole-image smoke:91API scenarios (86pure/five native), plus38queue scenarios using original lists and long/callback/cancellation checks. Success cases do not by themselves prove full-domain contracts for lists.

Five original native stubs are explicit immutable primitive bindings: keyfind/3,keymember/3,keysearch/3,member/2,reverse/2. Registry prepared/published images own every export; native bodies come only from exact admitted runtime primitives, not maintained pure-library fallbacks. Repeat preparation reuses only the previous exact current image's captured raw primitive. Standalone mixed-image execution is deliberately refused. Generated module_info/0,1 retains the existing separate registry metadata route; ordinary standalone imports do not claim own-image metadata support.

Original queue+lists comparison remains full-field:6384observations,6003strict,337bound identities,44first-frame saved-entry argument relations and zero residual differences. Native caller frame loss in the first capture is a preserved real defect, repaired and checked strictly in the second capture. No frame truncation, raw-output sorting or native cutpoint seeding.
