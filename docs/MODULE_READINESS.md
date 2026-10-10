# Module readiness

Baseline **OTP-29.1.1**. Local tests **1427 passed**. Runtime MFAs **66** (previous snapshot **66**, delta **0**), in **4** modules. Reference inventory: **1289** source-module rows.

API presence is the registered, directly tested MFA count divided by the union of pinned explicit exports and BIF declarations. It is not semantic completion or test coverage. Preprocessing, generated/NIF/conditional/platform exports and complete contracts remain unresolved. Component milestones have no invented weighted percentage.

## C# components

| Component | Status | Related passed tests | Delivered | Next milestone |
| --- | --- | ---: | --- | --- |
| Terms | Partially compatible | 31 | immutable terms and exact/numeric equality; codepoint ordering and integer/float coercion; bounded reuse of ten common immutable atom values; privately owned exact-key Dictionary index with sorted read-only map entries | full atom/resource limits and distributed identity |
| Serialization | Partially compatible | 4 | common bounded ETF tags and compressed input | external functions and wide identities; full vectors/resource boundaries |
| Runtime | Partially compatible | 57 | selective receive and local links/monitors; registered core MFAs | signal races, scheduler fairness and resources; distribution, ETS/DETS, ports/NIF/code loading |
| Compiler | Partially compatible | 774 | clauses, closures, case/receive and private scope analysis; if/begin/all ordinary operators; ordered guards and common branch exports; catch/try/of/catch/after and maybe/?= selected value/error/else/exception/private-scope contracts; alias and literal string/list-prefix patterns; transactional matching and incoming key/size scopes; maps and integer/binary/float/UTF bit construction/patterns; source-directed binding/error order; distinct interpreted/compiled sibling constraints and map-template safe-prefix/fallback scheduling from v3_core; lexical captures remain private; list/binary/map comprehensions, multiple list/map templates, relaxed/strict list/binary/map generators, filters and private shadowing; consuming binary mismatch and strict remaining-tail errors; map iterator/tuple/none/partial-source handling; exact keys and last repeated key wins; generated C# AST/evaluator, module-local tail-call trampoline and native hybrid/PackageReference integration; selected contracts verified against exact-source OTP-29.1.1 at7a35cdd:695 expressions/316 compiled modules/73 diagnostics=1084;1247/full same-head CI; full Core lowering remains incomplete; Grouped list/binary/map zip && generators with strict/shared row constraints; separate erl_eval traversal and v3_core joint/skip/tail clauses, raw compiled iterator versus normalized interpreted remainders; Sequential lint/compiled zip-source bindings and isolated interpreted source bindings; source bindings do not enter patterns/templates | records/macros and full grammar; full iterator/resource contracts, full zip input/iterator/Core contracts and assignment qualifiers; full maybe pattern/lint/resource contracts; full try lint/stack/resource contracts; complete source spans/debug mappings; full binary generator literal-string grouping/size/resource contracts; empty patterns unsupported and zero-progress local system_limit policy |
| Core library modules | Partially compatible | 168 | erlang/lists/maps/io registered subset; nth/nthtail/seq, keyfind/keymember/keysearch, last/split, append/1 and duplicate/flatten; member/2 exact/early-match boundaries; source-directed asynchronous lists:sort/2 with natural-run/alternating merges; maps:iterator/2 FunctionTerm comparators | remaining reference MFAs and full input/error contracts; large-map comparator callback order follows unspecified initial traversal; opaque HAMT paths/full sort and map suites |
| gen_server host facade | Partially compatible | 4 | C# callbacks, call/cast/info/stop and monitor replies | Erlang callback adapters/exports; sys, aliases, timeouts/continue and upgrades |
| supervisor host facade | Partially compatible | 6 | strategies, policies, intensity and ordered shutdown | Erlang exports/callback adapters; dynamic specs and shutdown edge cases |
| Hybrid and MSBuild | Partially compatible | 22 | native Erlang blocks and .erl generation; incremental/clean/disabled-preprocessing/package consumer | cross-block bindings, full diagnostics and IDE debugging |
| Tooling and packages | Implemented | 2 | local CLI/compiler/preprocessor/inventory; regenerated module readiness report | stable public release contract; no NuGet publication yet |
| Differential validation | Partially compatible | 484 | pinned source-built OTP-29.1.1 value/class/reason comparator; no production Erlang dependency; 722 expressions,343 generated C#/compiled-reference modules and73 normalized rejections verified at124af8c;1138/1138 complete/version verified; historical695 expressions/316 module source/expected/generated-hash contracts and73 diagnostics unchanged;27 comparator expressions/modules added without expectation correction; initial map1066/1068 and two original scheduling discrepancies remain retained in part077 and resolved in part078; new comparator increment zero mismatches; same-head1304/full CI; initial1300/1304 reserved-atom fixture parser failures retained; unavailable-oracle negative protocol and immutable snapshots; Part081 local1423/full pipeline;54 independent zip expressions/modules and six diagnostics;776/397/79=1252 planned, original execution pending publication; Part082 retains complete1245/1252 initial zip oracle and1423/full CI; five new fixture source bodies now explicitly order atom-map traversal with unchanged expected values; two numeric default pairs; local1427/full pipeline,1256 plan awaiting new head | full resource/input contracts; broader compiled-module/optimizer/stacks/signals |
| Style checks | Implemented | 2 | SDK syntax-aware layout and validation gate; XML documentation comment preservation and idempotence | broader SDK matrix |
| Benchmarks | Partially compatible | 0 | exploratory local harness; reproducible Release old/new C# integer-map lookup samples/allocations; no BEAM performance proof | representative workloads and BEAM comparison |

## Erlang API modules

| Module | Registered MFAs | Change | Reference declarations | API presence | Status |
| --- | ---: | ---: | ---: | ---: | --- |
| application | 0 | 0 | 38 | 0.0% | Not started |
| binary | 0 | 0 | 31 | 0.0% | Not started |
| code | 0 | 0 | 69 | 0.0% | Not started |
| dets | 0 | 0 | 64 | 0.0% | Not started |
| erlang | 40 | 0 | 351 | 11.4% | Partially compatible |
| ets | 0 | 0 | 76 | 0.0% | Not started |
| file | 0 | 0 | 69 | 0.0% | Not started |
| gen_event | 0 | 0 | 43 | 0.0% | Not started |
| gen_server | 0 | 0 | 41 | 0.0% | Not started |
| gen_statem | 0 | 0 | 39 | 0.0% | Not started |
| io | 1 | 0 | 53 | 1.9% | Partially compatible |
| lists | 20 | 0 | 91 | 22.0% | Partially compatible |
| maps | 5 | 0 | 34 | 14.7% | Partially compatible |
| rpc | 0 | 0 | 35 | 0.0% | Not started |
| string | 0 | 0 | 71 | 0.0% | Not started |
| supervisor | 0 | 0 | 23 | 0.0% | Not started |
| timer | 0 | 0 | 36 | 0.0% | Not started |
| unicode | 0 | 0 | 22 | 0.0% | Not started |

gen_server and supervisor have C# host facades; their Erlang-module exports are not registered. Full zero-registration inventory rows and implemented MFA names are preserved in module-readiness.json. Related test counts can overlap components and are not summed as independent coverage.
