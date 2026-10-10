# Module readiness

Baseline **OTP-29.1.1**. Local tests **1247 passed**. Runtime MFAs **65** (previous snapshot **65**, delta **0**), in **4** modules. Reference inventory: **1289** source-module rows.

API presence is the registered, directly tested MFA count divided by the union of pinned explicit exports and BIF declarations. It is not semantic completion or test coverage. Preprocessing, generated/NIF/conditional/platform exports and complete contracts remain unresolved. Component milestones have no invented weighted percentage.

## C# components

| Component | Status | Related passed tests | Delivered | Next milestone |
| --- | --- | ---: | --- | --- |
| Terms | Partially compatible | 31 | immutable terms and exact/numeric equality; codepoint ordering and integer/float coercion; bounded reuse of ten common immutable atom values; privately owned exact-key Dictionary index with sorted read-only map entries | full atom/resource limits and distributed identity |
| Serialization | Partially compatible | 4 | common bounded ETF tags and compressed input | external functions and wide identities; full vectors/resource boundaries |
| Runtime | Partially compatible | 57 | selective receive and local links/monitors; registered core MFAs | signal races, scheduler fairness and resources; distribution, ETS/DETS, ports/NIF/code loading |
| Compiler | Partially compatible | 681 | clauses, closures, case/receive and private scope analysis; if/begin/all ordinary operators; ordered guards and common branch exports; catch/try/of/catch/after and maybe/?= selected value/error/else/exception/private-scope contracts; alias and literal string/list-prefix patterns; transactional matching and incoming key/size scopes; maps and integer/binary/float/UTF bit construction/patterns; source-directed binding/error order; distinct interpreted/compiled sibling constraints and map-template safe-prefix/fallback scheduling from v3_core; lexical captures remain private; list/binary/map comprehensions, multiple list/map templates, relaxed/strict list/binary/map generators, filters and private shadowing; consuming binary mismatch and strict remaining-tail errors; map iterator/tuple/none/partial-source handling; exact keys and last repeated key wins; generated C# AST/evaluator, module-local tail-call trampoline and native hybrid/PackageReference integration; selected contracts verified against exact-source OTP-29.1.1 at7a35cdd:695 expressions/316 compiled modules/73 diagnostics=1084;1247/full same-head CI; full Core lowering remains incomplete | records/macros and full grammar; full iterator/resource contracts, zip and assignment qualifiers; full maybe pattern/lint/resource contracts; full try lint/stack/resource contracts; complete source spans/debug mappings; full binary generator literal-string grouping/size/resource contracts; empty patterns unsupported and zero-progress local system_limit policy |
| Core library modules | Partially compatible | 165 | erlang/lists/maps/io registered subset; nth/nthtail/seq, keyfind/keymember/keysearch, last/split, append/1 and duplicate/flatten; member/2 exact/early-match boundaries | remaining reference MFAs and full input/error contracts |
| gen_server host facade | Partially compatible | 4 | C# callbacks, call/cast/info/stop and monitor replies | Erlang callback adapters/exports; sys, aliases, timeouts/continue and upgrades |
| supervisor host facade | Partially compatible | 6 | strategies, policies, intensity and ordered shutdown | Erlang exports/callback adapters; dynamic specs and shutdown edge cases |
| Hybrid and MSBuild | Partially compatible | 21 | native Erlang blocks and .erl generation; incremental/clean/disabled-preprocessing/package consumer | cross-block bindings, full diagnostics and IDE debugging |
| Tooling and packages | Implemented | 2 | local CLI/compiler/preprocessor/inventory; regenerated module readiness report | stable public release contract; no NuGet publication yet |
| Differential validation | Partially compatible | 395 | pinned source-built OTP-29.1.1 value/class/reason comparator; no production Erlang dependency; 695 expressions,316 generated C#/compiled-reference modules and73 normalized rejections verified at7a35cdd;1084/1084 complete/version verified; historical631 expressions/252 module contracts/generated hashes/67 diagnostics unchanged;64 independent map/iterator/template-order expressions/modules and six new diagnostics verified; initial map run1066/1068 retained; two original compiled order mismatches resolved, only two provisional expectations corrected to retained official outcomes; all308 initial source/generated hashes unchanged; same-head1247/full CI and unavailable-oracle negative protocol; immutable initial failures and successful checkpoints retained | full resource/input contracts; broader compiled-module/optimizer/stacks/signals |
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
| lists | 19 | 0 | 91 | 20.9% | Partially compatible |
| maps | 5 | 0 | 34 | 14.7% | Partially compatible |
| rpc | 0 | 0 | 35 | 0.0% | Not started |
| string | 0 | 0 | 71 | 0.0% | Not started |
| supervisor | 0 | 0 | 23 | 0.0% | Not started |
| timer | 0 | 0 | 36 | 0.0% | Not started |
| unicode | 0 | 0 | 22 | 0.0% | Not started |

gen_server and supervisor have C# host facades; their Erlang-module exports are not registered. Full zero-registration inventory rows and implemented MFA names are preserved in module-readiness.json. Related test counts can overlap components and are not summed as independent coverage.
