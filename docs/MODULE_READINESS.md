# Module readiness

Baseline **OTP-29.1.1**. Local tests **716 passed**. Runtime MFAs **62** (previous snapshot **62**, delta **0**), in **4** modules. Reference inventory: **1289** source-module rows.

API presence is the registered, directly tested MFA count divided by the union of pinned explicit exports and BIF declarations. It is not semantic completion or test coverage. Preprocessing, generated/NIF/conditional/platform exports and complete contracts remain unresolved. Component milestones have no invented weighted percentage.

## C# components

| Component | Status | Related passed tests | Delivered | Next milestone |
| --- | --- | ---: | --- | --- |
| Terms | Partially compatible | 29 | immutable terms and exact/numeric equality; codepoint ordering and integer/float coercion; bounded reuse of ten common immutable atom values; privately owned exact-key Dictionary index with sorted read-only map entries | full atom/resource limits and distributed identity |
| Serialization | Partially compatible | 4 | common bounded ETF tags and compressed input | external functions and wide identities; full vectors/resource boundaries |
| Runtime | Partially compatible | 57 | selective receive and local links/monitors; registered core MFAs | signal races, scheduler fairness and resources; distribution, ETS/DETS, ports/NIF/code loading |
| Compiler | Partially compatible | 394 | clauses, closures, scopes, maps and bit syntax; generated C# AST and tail-call trampoline; bit construction sequential value/size evaluation and binding export after successful construction; if ordered guards/alternatives, if_clause and common branch binding export; begin and all ordinary expression operator spellings; catch logical stacks and corrected precedence/operand bindings; source-directed OTP expression-list/cons binding port for tuples/lists/call arguments; explicit per-routine provenance and CLR deviations; distinct compiled expression-list/cons conflict policy; independent capture scopes retained; full Core lowering remains unported; source-directed map base/field runtime scopes and independent static scopes; compiled subset kept distinct; Source-directed sequential binary value/size and compiled map base/key/value pre-expressions; independent static checks and distinct interpreter map/float phases; selected captures verified against OTP at 2435ce9; Compiled empty integer/float string segments: dynamic nonnegative integer size precheck before later callbacks, elimination without storage and binding export; selected cases verified at 77c4bf7 against OTP-29.1.1 | records/macros/comprehensions/try and full grammar; complete source spans/debug mappings |
| Core library modules | Partially compatible | 162 | erlang/lists/maps/io registered subset; nth/nthtail/seq, keyfind/keymember/keysearch, last/split, append/1 and duplicate/flatten; member/2 exact/early-match boundaries | remaining reference MFAs and full input/error contracts |
| gen_server host facade | Partially compatible | 4 | C# callbacks, call/cast/info/stop and monitor replies | Erlang callback adapters/exports; sys, aliases, timeouts/continue and upgrades |
| supervisor host facade | Partially compatible | 6 | strategies, policies, intensity and ordered shutdown | Erlang exports/callback adapters; dynamic specs and shutdown edge cases |
| Hybrid and MSBuild | Partially compatible | 15 | native Erlang blocks and .erl generation; incremental/clean/disabled-preprocessing/package consumer | cross-block bindings, full diagnostics and IDE debugging |
| Tooling and packages | Implemented | 2 | local CLI/compiler/preprocessor/inventory; regenerated module readiness report | stable public release contract; no NuGet publication yet |
| Differential validation | Partially compatible | 122 | Pinned source-built OTP-29.1.1 value/class/reason comparator; 467 expressions,81 generated-assembly/compiled-reference modules and35 normalized rejection comparisons verified at77c4bf7; All previous461/73/35 contracts and compiled hashes retained; independent empty-string error/effect/elimination outcomes match | full resource/input contracts; broader compiled-module/optimizer/stacks/signals |
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
| maps | 2 | 0 | 34 | 5.9% | Partially compatible |
| rpc | 0 | 0 | 35 | 0.0% | Not started |
| string | 0 | 0 | 71 | 0.0% | Not started |
| supervisor | 0 | 0 | 23 | 0.0% | Not started |
| timer | 0 | 0 | 36 | 0.0% | Not started |
| unicode | 0 | 0 | 22 | 0.0% | Not started |

gen_server and supervisor have C# host facades; their Erlang-module exports are not registered. Full zero-registration inventory rows and implemented MFA names are preserved in module-readiness.json. Related test counts can overlap components and are not summed as independent coverage.
