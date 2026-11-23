# Part1984 captured callback failure ordinals

Input corpus is the exact Part1982 local acceptance and final labels, compared against Windows OTP-29.1.1 at ad05823719d77c8faee87348ea39513d4e2f99c5. The prior pairing identifies 102 missing inlined helper keys across 65 modules. Eighty-seven keys have one-to-one pairs with the same normalized instructions and lines across 58 modules, and nine more without line instructions. The earlier equivalence normalizes physical labels, self-identity and fun table fields shown by disassembly. No build or test ran in this step.

## Two counters

- v3_core:fun_tq at 1727–1740 translates the nested clauses before allocating the anonymous Core identifier. v3_core:expr at 715–730 and 907–931 evaluates list/tuple items and call operands in source order through safe_list. The v3_core per-function counter also names generated Core letrec syntax, independently of lifted SSA slots.
- compile.erl:2572–2576 runs sys_core_inline for explicit target inlining and cerl_inline for the bare automatic inline option. cerl_inline:i_fun at 886 removes source IDs on surviving value funs. sys_core_fold.erl:818–832 also supports independent inline_list_funcs, which can change these callbacks to recursive definitions through sys_core_fold_lists.erl:105–155.
- beam_core_to_ssa:translate_fc_args at 574–597 reserves an inlined failure ordinal only if the original function_clause operands differ from the current owner's arguments and no named inlining annotation is present. The per-owner counter is also used to name letrec local functions (509–529) and otherwise unnamed anonymous SSA values (2448–2468). A closed callback with a partial head can consume the failure counter before a later translation drops its separate failure stub. The given opaque list calls require closure values and the closed callback retains its own entry failure.
- The pinned match compiler can partition and reorder alternatives (beam_core_to_ssa:match_con at 1333 onwards). Lexical source events are insufficient across case/if or mixed branch constructors. The current C# source traversal is LocalFunctionEntries.Children/Walk. ComprehensionSourceBindings stamps closures after their child expressions. Current EmitGeneralClosure has three separate paths for planned comprehension failure ordering, a narrow earlier closed callback reservation and the sequential emitted-failure fallback.
- The exact q82 diameter_codegen owner establishes why the failure counter cannot be modeled from the surviving pinned Core names. Its first closed callback targets fun-1 while its second captured callback targets fun-0. The captured failure is nonetheless inlined-1. The C# candidate emitted the closures in the same evaluation relationship under Core fun-0 and fun-1 respectively and gave the captured failure inlined-0.

## Measured scope

| Module and owner | Source context | Reference identity | Frozen candidate identity |
| --- | --- | --- | --- |
| yecc:compute_state/2 | Two ordinary imported foreach calls in a sequence | inlined-1 | inlined-0 |
| dbg_wx_winman:inform_all/1 | Closed map call and captured foreach call with ordinary record tuple heads | inlined-1 | inlined-0 |
| diameter_codegen:gen_hrl/2 | Two map calls in a returned proper list | inlined-1 | inlined-0 |

The Part1982 disassembly for each has exactly two physical MakeFun operations in owner order. The first captures nothing, and the second captures one value. Failure-body instructions and line equality are already witnessed. The dbg_wx_winman source uses ordinary records, which the current parser's RecordExpressions.Pattern lowers to tuple patterns. sofs:join/4 produces the same symbol offset in alternative branches and is excluded. Mixed LC, named fun, automatic inline and protected owners account for a wider remainder and need their own counter proofs.

All 512 Part1982 Equal control images contained readable abstract forms. None had a qualifying linear pair in the source scan. None had the wider disassembly condition of a first closed unary MakeFun and an existing inlined stub for the same owner. The adjacent Equal controls count 409 functions in 147 modules with at least one MakeFun and a lists:map/2 or foreach/2 call, including ct_event:handle_event/2 (single captured callback), diameter_misc_sup:init/1 (declared reference), diameter_sup:ets_new/1 (declared foreach callback), and ASN.1 dynamicsort_SETOF/1 (single closed mapper). This is projected zero drift. Replay is needed for post-change corpus evidence.

## Proposal

Retain the old reservation and add a bounded source proof for two ordinary callback values in a single source clause with linear sequence/list/tuple operand ordering. Resolve imported calls through existing call bindings, excluding local shadowing. Require closed first callback from the existing capture analysis, variable list input, partial tuple heads, two original source ordinals in the C# traversal, and an actual captured failure being emitted for the second fun. The existing SSA/module optimization gates apply. Honor profile-driven automatic and list inlining, avoid on_load and branch/protected or nested helper generation, leave sole callback and unconditional first callback unchanged.

The existing permanent compiler/beam/reserves-a-closed-closure-s-failure-ordinal fixture gains a captured succeeding and malformed call under a selected managed lists dependency, using an imported then a qualified call in a list result. The total first mapper and earlier local route remain controls. The patch passed strict forward application against source-1983-helper-recovery, strict private reverse and git diff --check. SHA-256 6E4DBEED6FE5570E19B2394F6BE504359604F6834DE4B299A80DC6C39AFB97AA.
