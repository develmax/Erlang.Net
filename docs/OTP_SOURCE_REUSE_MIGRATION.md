# OTP module alternatives and runtime admission plan

## Local implementation successor — 2026-10-18

The owner subsequently authorized local implementation of the measured twenty opcode gaps and reached shared runtime dependencies. This supersedes audit-only scope for this increment; publication/merge remain excluded. All twenty linker/validator/interpreter roles are implemented. The exact unchanged queue image prepares, publishes in isolated runtimes and executes all 38 queue API smoke cases with explicit `BeamDependencySelection([lists])`. The current selected lists implementation is maintained C#; reverse/member remain native runtime primitives. Default boot still selects the existing backend, and no source queue body is translated or substituted.

Actual execution checks include poison C# queue handlers, prepared-image owner identity, long queues, callbacks, cancellation and reference cleanup. Synthetic opcode edge images separately check aliasing, trim-zero/tail continuation, BIF guard destination preservation and malformed selection-table refusal. Final native comparison covers6384/6384 observations:6011 strict,337 bound identities and36 source-bound own-entry states, with raw36 differences retained. All1064 result terms match(1057 strict/seven bound); no stack frames are deleted or rewritten. [Per-opcode coverage and remaining partitions](validation/part-373/local-completion/OPCODE_MATRIX.md) distinguish implemented/source-reviewed/module-exercised from independently edge-tested. Full regression/package acceptance is recorded separately; neither this packet nor queue completion proves a complete BEAM VM.

The matrix and 699-MFA crosswalk below are the **pre-implementation audit snapshot at accepted head 54dc6993**. Their original linker refusals and file hashes remain historical evidence; they are not the current runtime's refusal status or a fresh hash guard. Current implementation acceptance is recorded separately after final native/regression gates.

### Current runtime admission matrix and next priority

| Pinned module | Current measured state | Remaining gate / next minimum result |
| --- | --- | --- |
| queue | Exact unchanged image prepares; all38 APIs executed with explicitly selected maintained C# lists dependency;6384 original observations covered under distinct raw/identity/own-state rules | Final full regression and local package receipts. Twenty opcode implementations are not exhaustive opcode semantics; see the separate coverage matrix. Default boot is still unchanged. |
| ordsets | Existing original image10796bytes/SHA28F740F20AF350D97B0FCF54691FD978CA3B2760849B42241822E7A575608FA4 decodes377 instructions; actual Prepare refuses bif2(3sites),is_ne_exact(2sites) | Later measured candidate after original lists: implement these two general instructions plus equality BIF imports, edge/error/frame tests, all-export admission; then original21-API execution/values/full-stack parity with explicit lists dependencies. |
| proplists | Existing original image18920bytes/SHAB027D227385C342C4153C0CA8627E6E8428FAA4A88F879E3C85A098067FF1882 decodes973 instructions; actual Prepare refuses additional roles | Reuse bif2/is_ne_exact work, then is_map/has_map_fields/put_map_assoc/put_map_exact and explicit sets/maps/lists dependencies; required operand/validator/import closure still needs review. No whole module execution claim. |
| Other OTP modules | Current C# API presence and contract evidence remain available; upstream image admission is module-specific | Inventory exact artifacts/includes/native boundaries before new opcode/import work. Parser/epp/metadata barriers belong to the source path and do not block an already compiled image. |

The [next-module probe](validation/part-373/local-completion/next-beam-admission.json) is read-only decoder/Prepare evidence from the architecture discussion; Root independently verifies its current runtime/role-source hashes and both input hashes. It is not guest execution, a build or instruction implementation. Queue was the first measured runtime increment, not the endpoint of VM development.

## Decision and scope

The owner explicitly confirmed on 2026-10-18: OTP modules remain Erlang; C# implements runtime, native BIFs and host integration. The subsequent module-alternatives requirement relayed from the architecture discussion preserves both original Erlang/BEAM and existing handwritten C# libraries. Preserve upstream algorithms instead of translating more pure library bodies. Existing C# implementations remain available as selectable alternatives, not scheduled deletions. This audit changes neither bindings nor production execution. No implementation, deletion, new publication or default backend switch is performed in this planning slice.

The central question is **what our runtime needs to execute unchanged upstream OTP modules**, not how many more library algorithms to rewrite. Source admission and BEAM admission are distinct candidate paths. Neither copying `.erl` files nor recognizing a BEAM opcode establishes executable module compatibility.

## OTP and current .NET execution

OTP normally compiles Erlang modules into BEAM object code, loads them through its code system and executes them in ERTS. BeamAsm translates BEAM instructions to native code at load time while reusing runtime services. These are architectural facts, not a requirement to copy OTP's implementation language or JIT into this project. [OTP code loading](https://www.erlang.org/doc/system/code_loading.html), [BeamAsm](https://www.erlang.org/doc/apps/erts/BeamAsm.html). Those live manuals currently describe OTP 29.1.2; compatibility evidence here remains pinned to **OTP-29.1.1 / ad05823719d77c8faee87348ea39513d4e2f99c5**.

Our source path is already real: `.erl` -> Parser/checked AST -> generated C# constructing a `ModuleDefinition` -> Roslyn assembly -> .NET evaluator/runtime. See `build/Erlang.Net.targets:10`, `tools/Erlang.Tool/Program.cs:33`, `src/Erlang.Compiler/CodeGeneration.cs:151`, `tools/Erlang.Differential/GeneratedModuleCompiler.cs:12` and `src/Erlang.Compiler/ModuleDefinition.cs:39`. Generated C#/IL is not a manual library port and is not evidence that every Erlang function is lowered into optimized direct C#.

Our BEAM path is also real: structure/instruction/link readers -> validation -> register interpreter -> existing .NET process/runtime services. `src/Erlang.Runtime/ModuleRegistry.PreparedBeam.cs` prepares and publishes linked code; `PreparedBeamImage.cs` creates actual interpreter-backed bodies. `BeamRegisterInterpreter.cs` executes instructions and reductions. `tests/Erlang.Tests/ActualMinimalBeamGuestSendHostChecks.cs:19` prepares the actual `guest_connected_bang322.beam`, invokes it and checks connected sends, backpressure/register retention and cleanup. The accepted part-372 CI contains this test. This proves a useful executable subset, **not arbitrary OTP-29.1.1 module execution**.

The current BEAM import allowlist is a concrete general-purpose blocker: `BeamExternalImports.cs:10` admits only eleven MFA shapes: `erlang:self/0`, `whereis/1`, `error/1`, `call_on_load_function/1`, `nif_error/1`, `+/2`, `-/2`, `put/2`, `load_nif/2`, `code:is_loaded/1`, `os:getenv/1`. Validation additionally restricts guard arithmetic to `+/2` and `-/2`, and the dedicated zero-argument BIF instruction to `self/0`. Hundreds of registered runtime APIs do not automatically become callable BEAM imports. Other instructions, operand shapes, frames and receives can be refused by independent validators even when their names occur in the opcode catalog.

## Runtime admission matrix: first measure, then implement shared gaps

Status is capability-specific. **Supported subset** means code plus a named exercised path; **partial** means explicit restrictions; **unmeasured** means no unchanged target-module execution or opcode closure was acquired. No row below is a whole-module compatibility percentage.

| Pinned target / requirement | Current status and evidence | Smallest end-to-end gate | Modules unlocked |
| --- | --- | --- | --- |
| queue, OTP-29.1.1: exact artifact and instruction/import inventory | Existing original CI asset located and hash-verified: 27,544 bytes; 2,068 instructions, 45 opcode names, 18 imports and 40 exports including two module_info exports. Whole-image inventory is measured; executable reachability/parity remains unproven. | Reuse the archived asset and its pinned source/build provenance; no duplicate original build is needed. Record reachable paths as actual admission advances. | Establishes the first reusable admission baseline. |
| queue: BEAM decoding, linking, literal/export/local-call resolution | Actual current structure/instruction readers successfully decode the asset. `BeamModule.Prepare` fails in `BeamLinkedCodeReader.ValidateRoles:294`; first unsupported role is `is_tuple` at instruction index 10 / Code offset 25. | Close measured generic role/operand gaps, then verify every exported entry and reachable private function, not only `new/0`. | queue, then proplists/ordsets and other pure modules. |
| queue: list/tuple tests, branching, arithmetic and construction instructions | Partial: exact image has 20 opcode names absent from linker role cases, with 456 occurrences. See measured list below. Validator/executor cases, operand/frame restrictions are further independent gates. | First `queue:new/0` and a nonempty `in/out` round trip; then every reachable instruction and failure edge. Add missing general opcode semantics, never queue-specific algorithm substitution. | Shared list/tuple/control-flow requirements across libraries. |
| queue: external imports and native BIF dispatch | Partial: measured 87 ordinary external callsites target shapes outside the eleven-shape whitelist; two module_info callsites have an existing special metadata route. The image also has 18 length gc_bif1 sites, one is_list bif1 site and 15 div gc_bif2 sites beyond current guard arithmetic. | Resolve actual queue imports to explicitly selected module backends or native BIF bindings; exercise successful, invalid and callback paths. Explicit C# lists dependency selection is allowed; silent fallback is not. | queue and most other pure modules; lists dependency closure remains explicit. |
| queue/proplists/ordsets: local calls, tail calls, closures and callback arities | Supported subset: interpreter has call frames, local/tail paths and lambda handling; no unchanged target closure proof. | Long recursion/tail loops; captured closures, wrong arity, callback throw/error/exit and nested calls through the actual BEAM export. | Higher-order queue, ordsets, proplists and lists families. |
| all modules: exception class/reason, source lines, continuations and full stacks | Partial: existing runtime error/stack machinery and interpreter handling; prior C# contract results do not transfer to a new execution backend. | Compare values plus complete error stacks against the same pinned original worker/context; preserve callback and helper frames and caller metadata. | Every source-admitted full contract. |
| all modules: reductions, cancellation, process resources | Existing runtime services; BEAM interpreter calls `ReduceAsync`; guest-send test checks suspension/backpressure and reference cleanup. Whole-module scheduling equivalence unmeasured. | Run long queue/callback work with progress, suspend/resume, cancellation and post-cancel resource checks on the actual loaded artifact. | Safe long-running library use, then process-oriented modules. |
| gen_server/proc_lib/supervisor: mailbox, receive, signals, monitors and timers | Existing runtime capabilities plus restricted BEAM receive planner/matcher; arbitrary upstream receive/control flow is not established. | Exact module artifact and transitive dependency inventory, selective receive timeout and startup/monitor/error lifecycle parity. | Behaviours after library and process-core gates. |
| all modules: explicit loading, ownership, current/old code and purge | Loader lifecycle primitives exist (`ModuleRegistry.PreparedBeam`, code images/source ownership); default C# library bindings already exist. No source replacement chosen. | Isolated runtime boot with one selected upstream module; assert active handler owner/hash, no accidental fallback; then replacement/purge/on_load lifecycle checks. | Reproducible source-vs-native binding and later controlled migration. |
| binary/maps/erlang: native stubs and mixed source/native exports | Partial by design: binary has 22 native entries/stubs plus real wrappers. Native operations remain C#; upstream stub `nif_error(undef)` must not replace their implementation. | Per-MFA binding crosswalk, actual native-call path from unchanged Erlang wrapper, source/value/error/effect parity. | Mixed wrappers without breaking working native primitives. |
| each later module: includes, macros, parse transforms, generated sources and native dependencies | Module-specific and unmeasured until inventoried at the pinned commit. queue/proplists/binary three-source probes do not prove other modules dependency-free. | Record compiler inputs/options, generated files, includes and native bindings; classify every external dependency before admission. | Reproducible widening beyond the first modules. |

### Measured unchanged queue artifact

[Detailed runtime audit](validation/otp-source-reuse-migration373/beam-audit/README.md), [machine receipt](validation/otp-source-reuse-migration373/beam-audit/receipt.json), [actual failed preparation](validation/otp-source-reuse-migration373/beam-audit/root-prepare-receipt.json).

The existing original receipt binds source `61B322302ECF74578D72B442D2317EA200A5DEE4E0994B58C5E1E961F252B101` at the pinned commit to installed `queue.beam` SHA-256 **759B1FE208F8A0DE292147EB00FFCE98F929263ADCFCD4AABB485E3B3EB4538D**. Asset and producer metadata: [original queue capture](validation/part-368/original-receipts-and-raw/artifact-12117323576/capture/inputs.json). The current .NET runtime independently decodes its exact bytes; no new compiler build, CI run, opcode implementation or guest execution was needed for this audit.

The twenty measured missing linker role names are `is_nil`, `is_nonempty_list`, `is_list`, `is_tuple`, `test_arity`, `get_list`, `get_tl`, `gc_bif1`, `jump`, `trim`, `swap`, `is_function2`, `is_atom`, `select_val`, `case_end`, `get_hd`, `is_ge`, `is_lt`, `bif1`, `call_last`. All 456 occurrences fail the current private role validator when independently probed. Fixing the first refusal alone does not admit the image. Catalog size (191 definitions), validator cases (34) and direct interpreter cases (30 plus four receive-plan opcodes) are different measurements and are not a compatibility percentage.

Plan queue gates in dependency groups: decode/link operand roles; list/tuple/control flow; generic BIF/import dispatch; frame/call/tail/FUN semantics; full stack and lifecycle checks. Some supported names still reject required operand forms, imports or call shapes. Preserve the initial failed preparation and repeat only the affected gates after a future implementation change.

### Separate source-front-end gates

These are **not blockers for execution of an already compiled `.beam`**. The exact whole-source CLI probes are retained in `artifacts/architecture373-upstream-source-probe1/receipt.json`: queue fails at 23:12 (`ERL002`, expected parenthesis), proplists at 24:12 (`ERL003`, unsupported moduledoc), binary at 1121:28 (`ERL001`, unterminated quoted literal). None reached execution. Preserve raw source/spans while accepting inert docs/spec/type syntax; preserve functional compile/import/on_load/NIF directives. The current hybrid Roslyn preprocess command is not Erlang `epp`. General includes/macros require their own implementation and dependency audit. The three pinned files have no include/define/ifdef directives; this does not generalize to OTP.

`ModuleDefinition.Register` adds source handlers while the default boot already registers C# queue/proplists/binary. Merely including an upstream file does not switch execution. A later explicit admission variant must prove ownership and avoid a Runtime -> Compiler -> Runtime bootstrap cycle. No default binding is switched in this audit.

### Build bootstrap versus production execution

An upstream `erlc` used **at build time** can produce immutable `.beam` assets for our .NET loader; this need not introduce a production `erl` subprocess. It is a candidate packaging route, not a completed bootstrap. Running the OTP compiler itself on our runtime later requires admission of its compiler/stdlib dependency closure, required instructions, BIFs and code-loading semantics. That milestone is unproven. Retain our current compiler for source/hybrid development and diagnostics; do not delete it to pursue this experiment.

## Ordered, measurable migration gates

1. **Read-only baseline (this slice):** cover all 699 registered MFAs, classify upstream/native boundaries and current C# routes, document unresolved evidence and runtime gates. No algorithm rewrite, backend implementation or publication.
2. **Queue artifact admission proposal:** reuse the already archived exact pinned `queue.beam`, its measured instruction/import set and current loader refusal. Do not repeat original acquisition merely to rediscover the same gaps. Determine actual reachable paths during admission; no extra VM architecture rewrite precedes this measurement.
3. **Small executable subset:** run `new/0` plus nonempty `in/out` through that unchanged artifact, prove actual code ownership/hash and list every temporary native/C# dependency. A subset is not 38/38 closure.
4. **All reachable queue paths:** fill shared instruction/import/runtime gaps until all 38 registered queue exports and reachable private helpers execute. Distinguish upstream Erlang dependencies from retained native BIFs. Require original values/errors/full stacks/effects and lifecycle regression on final sources. New admission acceptance does not inherit old C# compatibility credit.
5. **Widen by shared capability:** original lists is the next executable priority after queue373; preserve its five native BIF boundaries. Then consider proplists/ordsets and lists dependencies after measured instruction/import closure; maps/binary mixed bindings after native boundary proof; proc_lib/gen_server/supervisor after process/receive/loading dependencies. Reorder based on measured blockers rather than API counts.
6. **Explicit per-module alternatives:** design configuration selecting original Erlang/BEAM or handwritten C# under the same MFA API. Expose the chosen backend and code identity in diagnostics/provenance. Generated C#/IL remains a separate path. Native BIF calls are explicit primitive dependencies, not hidden fallback to a handwritten library. This audit implements no binding switch.
7. **Backend-specific acceptance and comparison:** reuse the same contract cases against pinned OTP, but keep results for each backend separate. Compare speed and allocation only after parity, with measured workloads; no C# speed advantage is assumed. Preserve both implementations. Live hot-switching or migration of state in running modules requires a separate design and is not promised.

The 60/40 split now means 60% source/BEAM admission and shared runtime capabilities enabling unchanged modules, 40% existing native/runtime gaps and focused retained regression. Keep independent result families running after an unrelated failure, but do not run dependent tests after failed preparation. Do not repeat full regression for documentation-only changes.

## Why the earlier plan translated libraries into C#

The original mission explicitly requested it: [work.md:31](../../work.md:31) says "Reimplement Erlang/OTP using C# as the primary implementation language"; [work.md:40](../../work.md:40) includes "Reimplement OTP libraries, modules, behaviours, and services"; [work.md:42](../../work.md:42) says "Port missing functionality from Erlang/OTP to C#". Its line 58 requires new OTP components in C# wherever technically possible. These are the written basis, not an inferred motive. The owner's later explicit correction supersedes that approach for pure OTP modules; the historical mission remains unchanged.


## Current inventory boundary

The current registry contains **699 distinct MFAs in 18 modules**. Every entry has one statically resolved C# registration site, exact callback text, file/method/line, and pinned upstream definition. This is evidence of the maintained C# route, not fresh execution proof or a complete downstream call graph. Source-guided or mechanically emitted C# port code is not proof of compiler output from the unchanged original Erlang module.

Classification is scoped to the upstream entry body: **311 native/runtime, 382 pure Erlang, 2 mixed, 4 uncertain**. A pure entry can call native primitives; that does not itself make its implementation a native entry. An exact BIF declaration or explicit `nif_error` stub is retained as runtime/native work. `erlang:min/2` and `max/2` have both native declarations and non-stub bodies and require separate dispatch review.

| Module | Registered MFAs | Native/runtime | Pure Erlang entry | Mixed | Uncertain |
| --- | ---: | ---: | ---: | ---: | ---: |
| erlang | 351 | 264 | 81 | 2 | 4 |
| lists | 91 | 5 | 86 | 0 | 0 |
| proc_lib | 39 | 0 | 39 | 0 | 0 |
| queue | 38 | 0 | 38 | 0 | 0 |
| maps | 34 | 12 | 22 | 0 | 0 |
| binary | 31 | 22 | 9 | 0 | 0 |
| logger | 26 | 0 | 26 | 0 | 0 |
| proplists | 22 | 0 | 22 | 0 | 0 |
| ordsets | 21 | 0 | 21 | 0 | 0 |
| supervisor | 14 | 0 | 14 | 0 | 0 |
| gen_server | 10 | 0 | 10 | 0 | 0 |
| records | 8 | 8 | 0 | 0 | 0 |
| application | 4 | 0 | 4 | 0 | 0 |
| erpc | 3 | 0 | 3 | 0 | 0 |
| error_logger | 2 | 0 | 2 | 0 | 0 |
| rpc | 2 | 0 | 2 | 0 | 0 |
| sys | 2 | 0 | 2 | 0 | 0 |
| io | 1 | 0 | 1 | 0 | 0 |
| **Total** | **699** | **311** | **382** | **2** | **4** |

The four uncertain preloaded facades are `erlang:apply/2`, `apply/3`, `yield/0`, and `hibernate/0`. Their self-delegating syntax needs compiler/loader intrinsic authority; it is not treated as an ordinary reusable function body.

## Runtime-first delivery gates

First identify the concrete missing preprocessing, parser, linker, loader, instruction, native binding, code ownership, or execution capability that prevents an unchanged upstream module from running. Preserve includes, macros, conditional compilation, functional attributes/options, filename/line metadata, and the real dependency closure. Extracted snippets and stripped attributes do not establish unchanged-module admission.

For each coherent family:

1. Close the shared runtime/compiler/loader prerequisite and preserve required native BIF/NIF/intrinsic behavior.
2. Build/load and actually execute unchanged pinned original Erlang/BEAM code on our runtime; record the generated C#/IL path distinctly if used.
3. Run shared semantic cases, with independent acceptance for each backend: return values, errors, options, complete stacks, callback behavior, resources, and affected regression.
4. Review explicit per-module backend selection and same-MFA provenance, including rollback and absence of hidden fallback.
5. Preserve both maintained C# and original backends. This plan has no retirement/deletion milestone.

A passing differential suite on a maintained C# handler does not prove that the unchanged original module can parse, link, load, execute, or pass the same contract on the original backend. Existing evidence remains valuable but is attributed to its actual backend.

## Family order

| Order | Family | Current registered subset | Admission focus |
| --- | --- | ---: | --- |
| P0 | Shared frontend, linker, loader, execution and native bindings | 0 | Cross-module capabilities; do not count as additional MFAs |
| F1 | queue | 38 | First unchanged-module execution and explicit backend admission |
| F2 | ordsets | 21 | Comparisons, lists dependencies, callbacks and source stacks |
| F3 | proplists | 22 | Lists/maps dependencies and callback/source metadata |
| F4 | lists | 91 | 86 Erlang entries plus 5 native entries; preserve primitive boundary |
| F5 | maps and binary | 65 | 31 Erlang entries and 34 native entries; mixed module ownership |
| F6 | proc_lib, gen_server, supervisor, sys | 65 | Process lifecycle, callback dispatch, cancellation, ancestry, restart/resource behavior |
| F7 | logger, error_logger, application, erpc, rpc, io | 38 | Kernel/service and IO host integration; preserve native/host seams |
| F8 | erlang | 351 | 81 ordinary Erlang entry bodies, 264 native entries, 2 mixed, 4 intrinsic uncertainties |
| R | records | 8 | Native stubs/runtime support; not a pure-module migration family |

The family partition contains each current MFA exactly once. Priority is based on dependency and executable admission, not a speculative effort estimate. Actual module-specific blocker evidence and the central runtime/BEAM gaps matrix should determine the next implementation step.

## Maintained C# inventory appendix

There are **93 registration files**, **126 immediate target implementation files**, and **181 unique files in their union**. The totals overlap and must not be summed. **36 files** refer to both upstream-native and upstream-pure entry MFAs. They are shared implementation files, not deletion candidates. No transitive call-graph completeness or engineering effort is inferred from these counts.

`ModuleRegistry.Register` installs C# delegates; `RegisterSource` also records source ownership. `InstallSourceWrappers` supplies source/error-boundary metadata over existing C# delegates and is not an Erlang compilation operation. `ModuleDefinition.Register` contains a distinct compiled-source `RegisterSource`/`Execution.InvokeAsync` path alongside maintained `OtpModules` registration. The inventory keeps these mechanisms separate.

The audit establishes **zero admitted original-module implementations within this 699-handler registry inventory**. This means no such backend admission proof was established by this audit; it does not mean that the compiler, BEAM reader, generated-source pipeline, or compiled test probes are absent.

Machine-readable evidence under `docs/validation/otp-source-reuse-migration373/`:

- `crosswalk.json`: all 699 MFAs exactly once; pinned source heads/lines/arity/native declarations; exact maintained C# registration callbacks and immediate targets; explicit uncertainties and unchanged compatibility status.
- `current-csharp-file-crosswalk.json`: deduplicated file roles and associated MFAs.
- `upstream-source-bindings.json`: pinned OTP blob/SHA bindings for 18 modules and native BIF table.
- `migration-families.json`: family partition, prerequisites, backend policy and admission gates.
- `snapshot.json` and `validation.json`: source bindings, scope, passive set/hash checks, and limitations.

Pinned behavioral authority is OTP commit `ad05823719d77c8faee87348ea39513d4e2f99c5`. Current .NET registration snapshots and the compatibility ledger are bound by SHA-256 in the evidence; none of their compatibility classifications are promoted by this planning audit.
