# Module readiness

Baseline **OTP-29.1.1**. Local tests **1026 passed**. Runtime MFAs **62** (previous snapshot **62**, delta **0**), in **4** modules. Reference inventory: **1289** source-module rows.

API presence is the registered, directly tested MFA count divided by the union of pinned explicit exports and BIF declarations. It is not semantic completion or test coverage. Preprocessing, generated/NIF/conditional/platform exports and complete contracts remain unresolved. Component milestones have no invented weighted percentage.

## C# components

| Component | Status | Related passed tests | Delivered | Next milestone |
| --- | --- | ---: | --- | --- |
| Terms | Partially compatible | 31 | immutable terms and exact/numeric equality; codepoint ordering and integer/float coercion; bounded reuse of ten common immutable atom values; privately owned exact-key Dictionary index with sorted read-only map entries | full atom/resource limits and distributed identity |
| Serialization | Partially compatible | 4 | common bounded ETF tags and compressed input | external functions and wide identities; full vectors/resource boundaries |
| Runtime | Partially compatible | 57 | selective receive and local links/monitors; registered core MFAs | signal races, scheduler fairness and resources; distribution, ETS/DETS, ports/NIF/code loading |
| Compiler | Partially compatible | 565 | clauses, closures, scopes, maps and bit syntax; generated C# AST and tail-call trampoline; bit construction sequential value/size evaluation and binding export after successful construction; if ordered guards/alternatives, if_clause and common branch binding export; begin and all ordinary expression operator spellings; catch logical stacks and corrected precedence/operand bindings; source-directed OTP expression-list/cons binding port for tuples/lists/call arguments; explicit per-routine provenance and CLR deviations; distinct compiled expression-list/cons conflict policy; independent capture scopes retained; full Core lowering remains unported; source-directed map base/field runtime scopes and independent static scopes; compiled subset kept distinct; Source-directed sequential binary value/size and compiled map base/key/value pre-expressions; independent static checks and distinct interpreter map/float phases; selected captures verified against OTP at 2435ce9; Compiled empty integer/float string segments: dynamic nonnegative integer size precheck before later callbacks, elimination without storage and binding export; selected cases verified at 77c4bf7 against OTP-29.1.1; Explicit compiled sibling match constraints remain separate from lexical captures; structural badmatch uses whole RHS and stops following effects; selected matches verified atdf42a94 against OTP-29.1.1; six old-implementation failures preserved; try/of/catch/after grammar, protected-body-only catch, original handler/after scopes, unsafe exports and fresh stack restrictions; selected cases verified at f202bca against OTP-29.1.1; Signed catch reason prefix operands retain :Stack boundary; three new expression/module regressions verified against OTP at5cbf0e8; same-head800 full CI/hybrid/package consumer passes; Catch stack-variable taint restricted to temporary catch guard scope; handler nested guards/captured and shadowed fun variables valid; verified against OTP-29.1.1 at c2c516b; same-head815 full CI passes; maybe/?= source-directed sequence, mismatch/else, exception and no-export binding contracts across interpreted/generated/hybrid paths; 30 independent expression/module fixtures and eight normalized diagnostics verified against OTP-29.1.1 at 2b99ef0; same-head892 full CI passes; Alias and literal string/integer-list prefix patterns across maybe/case/fun/function/catch; independent incoming key/size scopes and transactional matching; 24 expression/25 compiled module/six diagnostic contracts verified against OTP-29.1.1 at64579d4; same-head949 full CI passes; List comprehensions: relaxed/strict list generators, multiple templates, filters, nested/dependent generators, fresh private bindings and original map-key/bit-size scopes; 36 expression/module fixtures and five normalized rejections; 36 expressions/modules and five normalized diagnostics verified against OTP-29.1.1 at e2cee56; same-head1026 full CI passes | records/macros and full grammar; binary/map comprehensions/generators, zip and assignment qualifiers; full maybe pattern/lint/resource contracts; full try lint/stack/resource contracts; complete source spans/debug mappings |
| Core library modules | Partially compatible | 162 | erlang/lists/maps/io registered subset; nth/nthtail/seq, keyfind/keymember/keysearch, last/split, append/1 and duplicate/flatten; member/2 exact/early-match boundaries | remaining reference MFAs and full input/error contracts |
| gen_server host facade | Partially compatible | 4 | C# callbacks, call/cast/info/stop and monitor replies | Erlang callback adapters/exports; sys, aliases, timeouts/continue and upgrades |
| supervisor host facade | Partially compatible | 6 | strategies, policies, intensity and ordered shutdown | Erlang exports/callback adapters; dynamic specs and shutdown edge cases |
| Hybrid and MSBuild | Partially compatible | 19 | native Erlang blocks and .erl generation; incremental/clean/disabled-preprocessing/package consumer | cross-block bindings, full diagnostics and IDE debugging |
| Tooling and packages | Implemented | 2 | local CLI/compiler/preprocessor/inventory; regenerated module readiness report | stable public release contract; no NuGet publication yet |
| Differential validation | Partially compatible | 285 | Pinned source-built OTP-29.1.1 value/class/reason comparator; 470 expressions,87 generated-assembly/compiled-reference modules and35 normalized rejection comparisons verified atdf42a94; All previous467/81/35 contracts and generated hashes retained; structural whole-value failures/effect timing independently match; 496 expressions,116 compiled modules and41 diagnostic contracts verified at f202bca; try boundaries/scopes, all prior470/87/35 contracts and generated hashes retained, no expectation corrections; 499 expressions,119 compiled modules and41 diagnostics verified at5cbf0e8; all496/116/41 previous contracts/hashes preserved; signed integer/float/parenthesized catch reasons match, zero corrections; Three independent normalized stacktrace_guard/stacktrace_bound rejection contracts added; historical41 variable diagnostic outcomes unchanged; 44 normalized diagnostic contracts verified against OTP-29.1.1 at c2c516b; 505 expressions,125 compiled modules and44 diagnostics verified at c2c516b; all499/119/41 historical contracts and generated hashes preserved; six independent stack guard scope expressions/modules and three rejection contracts match, zero corrections; 535 expressions,155 compiled modules and52 diagnostics verified at 2b99ef0; all505/125/44 historical contracts and generated hashes preserved; maybe conditional match/else/exception/scope cases match, zero corrections; 559 expressions,180 compiled modules and58 diagnostics verified at64579d4; all535/155/52 previous contracts and generated hashes retained; alias/literal-prefix matching and rejection cases match, zero expectation corrections; 595 expressions,216 compiled modules and63 normalized diagnostics verified at e2cee56; all559/180/58 prior contracts and generated hashes preserved; 36 list-comprehension expression/module contracts and five scope rejections match without expectation corrections | full resource/input contracts; broader compiled-module/optimizer/stacks/signals |
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
