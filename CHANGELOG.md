# Detailed work changelog

Every work part receives a separate sequential number. Entries record the objective, changes, affected files, decisions, validation results, limitations and next step. Completing a part means completing its stated scope, not the entire assignment in `../work.md`.

The first entry was reconstructed from the implementation and saved reports. Its subsections describe completed work; their order is not a precise chronology of individual edits. No dates or results are attributed to checks that did not run.

## Part 001 — 2026-10-09 — Initial implementation

**Part status:** the completed initial stage is recorded. **Project status:** the full assignment remains incomplete; Erlang/OTP compatibility is partial.

### Objective and starting conditions

- Read `../work.md`; implemented in this repository and inspected the official `../otp` reference sources.
- The target repository initially contained a README. Added the solution, projects, implementation, examples, verification and documentation.
- Pinned **OTP-29.1.1**, commit `ad05823719d77c8faee87348ea39513d4e2f99c5`, in [reference-baseline.json](reference-baseline.json). The reference checkout HEAD is newer; comparisons used the pinned tag.
- Pinned SDK **10.0.400**, **net10.0**, C# 14, nullable, deterministic builds, analyzers and warnings as errors.
- Implemented the runtime and compiler in C#. No production BEAM dependency was introduced. The reference checkout remained unchanged.

### 001.1 — Solution structure and configuration

**Files:** [Erlang.Net.slnx](Erlang.Net.slnx), [Directory.Build.props](Directory.Build.props), [global.json](global.json), [.gitignore](.gitignore).

Created 11 projects: Terms, Runtime, Serialization, Compiler, Otp.Behaviours, CSharp.MSBuild, CLI Tool, Differential, Tests, HelloHybrid and Benchmarks. Build/test infrastructure is separate from production implementation. Restricted MSBuild environments use `-m:1`; ordinary builds were also verified outside worker-process restrictions.

### 001.2 — Erlang terms and pattern matching

**Files:** [src/Erlang.Terms](src/Erlang.Terms), [tests/Erlang.Tests](tests/Erlang.Tests).

- Implemented immutable atoms, BigInteger, finite floats, PID/reference/port, function closures, tuples, nil/cons, proper/improper lists, maps, binaries and bitstrings. Buffers are copied; strings are Unicode codepoint lists.
- Separated exact and numeric equality. Comparing large integers with double avoids precision loss from converting the integer to double.
- Preserved OTP 29 exact signed-zero distinctions, codepoint atom order, type order, tuple arity-first comparison, exact map keys and integer/float key ordering.
- Structural hashes agree with exact equality.
- Pattern matching commits bindings only on success; repeated variables require exact equality. Nested tuples and lists with tails are supported.

**Validation:** permanent equality, ordering, map-key, Unicode, list and binding-rollback tests are included in the aggregate 121/121 report. The full set of Erlang terms and patterns is not implemented yet.

### 001.3 — Processes, selective receive and signals

**Files:** [src/Erlang.Runtime](src/Erlang.Runtime), [tests/Erlang.Tests](tests/Erlang.Tests).

- Mailboxes scan messages in arrival order and clauses in source order; unmatched messages remain queued.
- Added asynchronous wakeups, cancellation, zero/finite/infinite timeouts and monotonic absolute deadlines.
- Processes execute through Task continuations. Added ProcessContext, process dictionaries, name registration and exit cleanup.
- Implemented links, monitors, DOWN, EXIT, trap_exit, normal-exit handling and untrappable kill with reason killed.
- SpawnMonitor creates the process and monitor atomically under the runtime lock. Linked termination cascades use an iterative queue to avoid recursive stack overflow. Already-cancelled process bodies do not start.
- Execution yields after 2000 expression evaluations.

**Validation:** selective receive, timeout/cancellation, unmatched-message preservation, links/monitors, a 10,000-link cascade and 200 SpawnMonitor iterations. The complete BEAM scheduling model, fairness, resource ownership and all signal races remain open.

### 001.4 — External Term Format

**Files:** [src/Erlang.Serialization](src/Erlang.Serialization).

Added encoding/decoding for common ETF tags: integers, doubles, Latin-1/UTF-8 atoms, tuples, nil/list/string, improper lists, maps, binaries/bitstrings and old/new PID/port/reference forms. Compressed decoding checks sizes, depth up to 256, malformed input and trailing bytes.

**Validation:** roundtrips, exact term comparison and negative inputs in the main harness. **Limitations:** fun tags and reference IDs wider than the supported 64 bits are absent; full ETF compatibility is not claimed.

### 001.5 — Compiler and Erlang source execution

**Files:** [src/Erlang.Compiler](src/Erlang.Compiler), [tools/Erlang.Tool](tools/Erlang.Tool), [examples/HelloHybrid/arithmetic.erl](examples/HelloHybrid/arithmetic.erl).

- Implemented a lexer, Pratt parser and AST: module/export, function clauses, literals, tuples/lists, operators, guards, case, receive, fun and apply.
- Added scopes, single assignment, unsafe-variable/rebinding diagnostics, guard restrictions and local/private/export dispatch.
- Guards support comma/semicolon alternatives: an error in one alternative does not abort the next alternative.
- Spawned closures use the child process context, including self.
- Compilation generates C# AST construction code executed by an asynchronous C# evaluator.
- Local tail calls use a trampoline, including sequence/case/receive. Large list literals use Cons.From to avoid deeply nested generated C# expressions.
- Distinguished reserved words, quoted atoms and quoted operators. Unsupported escape sequences produce diagnostics instead of silent substitution.

**Validation:** source compilation, guards, scopes, closures, negative examples and 50,000 tail calls. **Limitations:** grammar and evaluator support are partial; full native lowering and the complete language are pending. The next language step is map syntax/patterns/updates.

### 001.6 — Core MFAs

**Files:** [src/Erlang.Runtime](src/Erlang.Runtime), [docs/supported-mfas.json](docs/supported-mfas.json).

Registered **42 MFAs** with individual permanent smoke tests: 33 in erlang, 6 in lists, 2 in maps and 1 in io. Support covers basic type predicates, tuple/list operations, self/spawn/link/monitor, names, process dictionaries, errors and exits. lists includes reverse/append/member/sum/map; maps includes get/size; io:format/2 supports a limited set of directives.

The exact list and test IDs are recorded in supported-mfas.json. Exact equality for lists:member was confirmed from the reference BIF. These MFAs are **Partially compatible**; a smoke test does not establish the complete function contract or whole-module compatibility.

### 001.7 — Initial OTP behaviours

**Files:** [src/Erlang.Otp.Behaviours](src/Erlang.Otp.Behaviours).

- GenServer: init/call/cast/info/terminate callbacks, state, reply/stop, $gen_call/$gen_cast messages, monitor/reference replies, errors and timeouts. Terminate is skipped after external untrappable logical process death.
- Supervisor: OneForOne/OneForAll/RestForOne, Permanent/Transient/Temporary, restart intensity/period, forward startup and reverse shutdown, timeout/kill fallback, live-child startup checks, links and snapshots.

**Validation:** call/cast, init failure, timeout/crash, restart strategies and shutdown are covered in the harness. **Limitations:** Erlang callback adapters, aliases/late-reply suppression, sys, dynamic children and complete child specifications remain absent.

### 001.8 — Mixed C#/Erlang and MSBuild

**Files:** [build/Erlang.Net.targets](build/Erlang.Net.targets), [src/Erlang.CSharp.MSBuild](src/Erlang.CSharp.MSBuild), [examples/HelloHybrid](examples/HelloHybrid).

- The hybrid preprocessor uses Roslyn tokens to recognize C# context. receive/case/fun are supported at selected statement/assignment/return/lambda boundaries and end with end.
- Blocks require an async context and a ProcessContext named erlangProcess. Each Erlang block has independent bindings; added #line mapping and preservation of the project's Nullable mode.
- The example verifies ordinary C# comments, raw/interpolated strings, methods named fun/receive, switch case, records, generics, nullable and async.
- MSBuild collects Compile/ErlangSource items, generates files under obj with stable path hashes, replaces original Compile items and tracks FileWrites plus tool DLL/target inputs for incremental builds.
- Fixed target ordering: preprocessing runs before _GenerateCompileDependencyCache/CoreCompile. Disabling preprocessing after a successful build now causes the expected error instead of being masked by the compile cache.
- Fixed nullable context in generated .g.cs files.

**Validation:** Hello World, compiled .erl result 42, signed zero/C# features, incremental timestamps, expected preprocessing-disabled failure and clean/rebuild. **Limitations:** arbitrary nested hybrid expressions, synchronous suspension and IDE language services remain absent.

### 001.9 — Local packages, tooling and integration checks

**Files:** [tools/validate.ps1](tools/validate.ps1), [tools/Erlang.Tool](tools/Erlang.Tool), [.github/workflows](.github/workflows).

- CLI supports compile/preprocess/inventory. Prepared local Erlang.Net.Tool and Erlang.Net.CSharp packages, version 0.1.0.
- The CSharp package contains buildTransitive targets, tools, runtime/compiler/terms/OTP/serialization binaries and notices.
- validate.ps1 builds the solution, runs the executable harness, checks examples/incremental/negative/clean builds, packs both packages, builds a real PackageReference consumer and installs the CLI locally under artifacts.
- Consumer restore caches are keyed by package content hashes: reusing version 0.1.0 cannot substitute stale binaries. Five consumer DLL hashes were checked against current build outputs.
- Prepared Windows validation and manual differential workflows with isolated pinned-OTP builds on Ubuntu. Remote workflows were not run in this part.

**Limitations:** packages are local prototypes; no publication or global tool installation occurred. Tests use an executable harness; dotnet test does not run it.

### 001.10 — Reference, inventory, licenses and documentation

**Files:** [docs/otp-inventory.md](docs/otp-inventory.md), [docs/otp-inventory.json](docs/otp-inventory.json), [docs/dependency-decisions.md](docs/dependency-decisions.md), [docs/license-audit.md](docs/license-audit.md), [tools/Erlang.Differential](tools/Erlang.Differential).

- Inventory was built from git archive of the pinned commit: **1289 source modules, 38,622 explicit exports, 524 BIF entries, 0 unresolved attributes**. The reference checkout was unchanged.
- Inventory describes source attributes before macro expansion. Effective conditional-compilation contracts, NIF/generated/OS exports and complete callback signatures need further work. An export does not imply an Erlang.Net implementation.
- The differential runner contains 18 source-expression cases, reads ETF from the oracle, compares terms exactly and verifies OTP_VERSION 29.1.1.
- No local erl/erlc/Docker or usable WSL oracle was available. Real differential comparison was not performed.
- Recorded decisions on BCL BigInteger, Task continuations and the custom selective mailbox. Wholesale actor-framework reuse was not selected; historical Erlang.NET candidate audits remain incomplete.
- Preserved OTP Apache-2.0 notices, the .NET Library license/third-party notices and Roslyn MIT notices. The public license for original code awaits the owner's decision; provenance verification for the exact Roslyn version remains limited.
- Added architecture, compatibility, semantic differences, decisions, roadmap, progress and next steps; partial statuses remain separate from the full definition of done.

### Validation and saved results for the entire part

The checks below ran during part 001 implementation. They were not rerun when the changelog was added; results were read from saved reports.

| Check | Command / evidence | Result |
| --- | --- | --- |
| Solution, 11 projects | dotnet build -m:1; also dotnet build outside worker restrictions | Passed, 0 warnings/errors |
| Permanent regression tests | dotnet run --project tests/Erlang.Tests --no-build -- artifacts/tests.json; [report](docs/validation/part-001/tests.json) | 121 passed, 0 failed; includes 42 direct MFA tests |
| MSBuild/package/tool integration | pwsh -File tools/validate.ps1; [report](docs/validation/part-001/integration-results.json) | Build, Tests, Incremental, CleanRebuild, PackageConsumer, LocalTool — Passed; Hello World; DisabledPreprocessing — Expected failure |
| Differential against real OTP | [runner](tools/Erlang.Differential) | Not run: local oracle unavailable |
| Remote CI | [.github/workflows](.github/workflows) | Prepared; execution unconfirmed |
| Whitespace and reference checkout | git diff --check; git -C ../otp status --porcelain | No diff errors; reference checkout clean |

Exploratory benchmark: Release, .NET 10.0.11, Windows 10 x64, 16 logical processors; one warmup and one measured run. [Results](docs/validation/part-001/benchmarks.json), [method](docs/benchmarks.md):

- 10,000 spawn/send/receive/complete lifecycles: **95.2928 ms**, allocated **23,055,808 bytes**.
- Preloading 10,000 messages and one late selective receive: **0.8435 ms**, allocated **1,680,392 bytes**.
- 10,000 ETF encode/decode/exact checks of a 100-element list: **154.2279 ms**, allocated **113,120,040 bytes**.

These are not BEAM comparisons, statistical benchmarks or scalability evidence. Allocated bytes are not retained process memory; queued-message counts are not receive throughput.

### Remaining work and handoff

The full language, OTP libraries/behaviours, distribution, complete process/signal model, IDE support and full compatibility remain unfinished. No known local regression test fails at this checkpoint; the missing oracle prevents confirmation of reference compatibility.

The next independent language part covers map literals/patterns/updates, => and :=, badmap/badkey errors, repeated variables and source/hybrid integration tests. An exact OTP development oracle is also needed. See [NEXT_STEPS](docs/NEXT_STEPS.md) for the executable sequence.

No Git commit/push or public publication occurred during this part. The later checkpoint commit is recorded in part 003.

## Part 002 — 2026-10-09 — Recording history and requiring future entries

**Objective:** record completed work before the next implementation and maintain a detailed entry for every part, as requested by the user.

**Changes:** added this CHANGELOG with reconstructed part 001 and an [entry template](docs/changelog-entry-template.md). [AGENTS.md](AGENTS.md) requires reading the log before the next part and updating it at each checkpoint; README and PROGRESS link to it.

**Decision:** the log preserves history; PROGRESS describes the current checkpoint. Numbers are sequential. Corrections to old facts must carry a date and reason. Partial results, unrun checks and blockers are explicit.

**Validation:** matched 121/0 and integration statuses against saved JSON; checked local links and whitespace diffs. Documentation-only changes did not require a rebuild, and none was run.

**Limitations:** part 001 history was reconstructed after implementation without invented timestamps. Creating the journal did not create a Git commit or alter commit/push rules.

**Next step at this checkpoint:** implement the next NEXT_STEPS item and record it as part 003. The subsequent user request assigned part 003 to English documentation and committing the completed checkpoint instead.

## Part 003 — 2026-10-09 — English changelog and checkpoint commit

**Objective:** maintain the log in English and commit the completed work in this repository, as requested by the user.

**Changes:** translated the full changelog and entry template into English, preserving scope, evidence and limitations. Updated AGENTS.md to require English entries and local commits of completed, validated parts. The checkpoint includes the implementation from part 001 and documentation from parts 002–003. Removed trailing whitespace and extra EOF blank lines found by the staged diff check; license wording and executable behavior are unchanged.

**Decisions:** this request authorizes local checkpoint commits. Push and package publication remain separate actions and are not authorized by this request. Generated artifacts, bin/obj outputs, local package caches and tool installations remain ignored.

**Validation:** checked English-only log/template text, Markdown links, staged whitespace and commit scope. Implementation evidence remains the saved 121/121 test and successful integration reports from part 001; code checks were not rerun for this documentation change.

**Limitations:** the overall assignment is still incomplete; no real OTP oracle results or remote CI results are claimed.

**Commit:** this entry is included in the local checkpoint commit titled `Implement initial Erlang.NET foundation and detailed English changelog`. Its hash is available from Git history; it is not embedded in its own contents.

**Next step:** continue NEXT_STEPS, add part 004 with detailed validation evidence, then commit the completed part locally.

## Part 004 — 2026-10-09 — Publish the foundation and implement map source syntax

**Part status:** completed for the stated map subset; **compatibility status:** Partially compatible. The complete assignment remains unfinished, estimated at roughly 5% overall. This estimate is judgment about the full scope, not measured compatibility, export coverage or the percentage of passing tests.

### Objective and publication

The user requested publication, continued implementation and an overall completion estimate. Published checkpoint **3860695** to the configured origin/main at https://github.com/develmax/Erlang.Net. Continued with map construction, updates and patterns, the next dependency-ready language task. No public NuGet release was requested or performed.

### Changes and affected files

- [Syntax.cs](src/Erlang.Compiler/Syntax.cs): added Expr.Map/MapField, postfix map updates, MapPattern/MapPatternField and parsing of #{} / => / :=. Patterns allow only :=; bare construction rejects := during semantic validation.
- [Semantics.cs](src/Erlang.Compiler/Semantics.cs): map bases/keys/values are analyzed; pattern keys must be legal supported guard expressions with variables bound before the entire pattern. Variables bound by sibling patterns cannot provide keys. Value bindings participate in existing unsafe-variable and single-assignment checks.
- [Patterns.cs](src/Erlang.Runtime/Patterns.cs): propagated optional process context and a snapshot of pre-pattern key bindings through nested patterns. Failed map matches roll back bindings. Closure key scope remains distinct from value-pattern shadowing.
- [Execution.cs](src/Erlang.Compiler/Execution.cs): constructs immutable maps, performs exact associative/update checks, permits later duplicate values to win and reports {badmap,Value}/{badkey,Key}. All field expressions evaluate before map type/key validation, preserving reference error cases where expression errors take precedence. Guard construction/update errors reject the guard; key-evaluation errors reject the pattern.
- [Term.cs](src/Erlang.Terms/Term.cs): added nonthrowing exact-key TryGet for map matching; no change to existing exact equality/order.
- [CodeGeneration.cs](src/Erlang.Compiler/CodeGeneration.cs): emits map ASTs, patterns and literal MapTerms. Fixed hybrid recognition when Roslyn hides Erlang # as directive trivia, including inline case/receive and remote calls in case scrutinees. Roslyn still supplies C# block-start boundaries; the Erlang lexer/parser reads the candidate block.
- [map_source.erl](examples/HelloHybrid/map_source.erl), example project and Program.cs: actual generated-module map construction/update/function patterns plus a hybrid map case run through MSBuild without changing the expected Hello World output.
- [Program.cs tests](tests/Erlang.Tests/Program.cs): added **32 permanent regression cases** covering exact integer/float/signed-zero keys, duplicate keys, immutable updates, missing keys, invalid bases, error precedence, empty/subset/nested/repeated patterns, guard keys and failures, scope diagnostics, rollback, selective receive, captured/shadowed closure keys and hybrid preprocessing.
- [validate.ps1](tools/validate.ps1): PackageReference consumer now copies and compiles the new map module; the installed local CLI also compiles it. Fixed the intermediate consumer failure caused by its previous arithmetic-only source list.
- [Differential runner](tools/Erlang.Differential/Program.cs): expanded from 18 to **26 cases**, adding map values, updates, patterns, keys, closures and guard failure. These new oracle cases have not run.
- Updated progress, compatibility matrix/JSON, semantic boundaries, decisions, reuse audit and README. Added the overall estimate and persistent authorization to publish completed commits to origin.
- Preserved part 001 reports under docs/validation/part-001 by extracting them from commit 3860695; historical changelog links now point to those immutable copies. This is an evidence-link correction, not a change to historical 121/121 results. Current and part-004 reports are saved separately.

### Reference, reuse and semantic decisions

Inspected the exact OTP-29.1.1 expressions manual (Map Expressions and binding/precedence rules) and lib/compiler/test/map_SUITE.erl, especially t_update_exact, variable updates and pattern cases. Reused existing MapTerm exact-key behavior and BCL Dictionary primitives; no new dependency, third-party implementation or copied reference code. See [reuse audit](docs/dependency-decisions.md).

Duplicate associations and exact updates apply in source order; an earlier => can introduce the key for a later :=, while an earlier failing := cannot be rescued by a later =>. Key/value expression evaluation order is not specified by the reference; this implementation uses left-to-right evaluation and does not claim it as a BEAM guarantee. Pattern keys use the pre-pattern scope, and matching does not require exact map size: #{} matches any map, not any term.

### Validation

| Check | Command / saved evidence | Actual result |
| --- | --- | --- |
| Full solution and permanent tests | dotnet build -m:1 --nologo; dotnet run --project tests/Erlang.Tests --no-build -- artifacts/tests.json docs/supported-mfas.json; [part report](docs/validation/part-004/tests.json) | **153 passed, 0 failed**, including 42 direct MFA cases; 0 build warnings/errors |
| Full build/package integration | pwsh -File tools/validate.ps1; [part report](docs/validation/part-004/integration-results.json) | Build/Tests/Incremental/CleanRebuild/PackageConsumer/LocalTool Passed; exact Hello World; preprocessing disabled caused expected failure |
| .erl and hybrid maps | Example and real PackageReference consumer executed by validation; packaged CLI compiled map_source.erl | Passed |
| Historical evidence | Extracted reports from Git checkpoint 3860695 into part-001 | Original 121/121 results preserved |
| Diff hygiene and reference checkout | git diff --check; git -C ../otp status --porcelain | No whitespace errors; OTP reference checkout unchanged |
| Real OTP differential / reference suites | 26-case runner prepared; local exact-version oracle absent | Not run; no compatibility claim inferred |
| Remote CI | Workflows present; no confirmed run result | Unconfirmed |

The first map build exposed hybrid # directive handling; an inline regression then exposed a hidden next token. Both were fixed. The first full package cycle exposed the missing consumer module copy, which was fixed before the successful final validation. There are no known failing local regression tests at this checkpoint.

### Limitations and handoff

Map comprehensions, complete map BIF/guard coverage, full-language guard expressions, binary source syntax, optimized map storage and real oracle verification remain pending. Error stacktrace fidelity and all other previously documented runtime/OTP gaps remain open. Benchmarks were not rerun because this part makes no new performance claim.

Next: obtain exact OTP oracle evidence and expand map guard BIFs, then implement bit syntax with source-backed tests. Keep [NEXT_STEPS](docs/NEXT_STEPS.md) as the executable queue. The completed map checkpoint is committed under `Add Erlang map source syntax and preserve checkpoint evidence` and published to origin/main; obtain its hash from Git history instead of embedding its own hash in the commit contents.

## Part 005 — 2026-10-09 — Map guard BIFs and differential error outcomes

**Status:** completed for this scope; Partially compatible overall. No new full-project completion claim.

### Objective and changes

Continue the map contracts from part 004 by implementing erlang:is_map/1, map_size/1, map_get/2 and is_map_key/2. Added all four to [runtime dispatch](src/Erlang.Runtime/Modules.cs) and the [guard whitelist](src/Erlang.Compiler/Semantics.cs), using existing immutable MapTerm exact-key operations. The registry now contains **46 MFAs**, all with direct permanent tests.

[Tests](tests/Erlang.Tests/Program.cs) add four direct MFA cases and twelve semantic regressions: positive/negative type tests, cardinality, missing/exact keys, invalid map error reasons, signed zeros, qualified erlang guards, failure and alternatives, map_get in pattern keys, and rejection of maps:get in guards. The [generated example](examples/HelloHybrid/map_source.erl) invokes all four functions in a function-clause guard and runs in both project and PackageReference consumer validation.

The [differential runner](tools/Erlang.Differential/Program.cs) now compares tagged outcomes: {ok,Value} or {error,Class,Reason}, using an Erlang try/catch wrapper for the oracle and catching ErlangException in the C# evaluator. Added eleven cases, bringing the corpus to **37**, including badmap/badkey/update errors, error/throw/exit classes and guard alternatives. This does not compare stack traces or asynchronous signal effects.

### Reference and reuse decisions

Inspected OTP-29.1.1 erts/emulator/beam/erl_map.c: map_size_1, maps_get_2/map_get_2 and maps_is_key_2/is_map_key_2; also lib/stdlib/src/erl_internal.erl for guard/type-test declarations. These establish badmap/badkey reason payloads and guard legality. Reused MapTerm.Get/TryGet/Entries and CoreModules.Boolean; no external dependency or copied implementation. See [dependency decisions](docs/dependency-decisions.md).

### Executed validation

| Check | Command and evidence | Result |
| --- | --- | --- |
| Full solution, regression harness | pwsh -File tools/validate.ps1, which executes dotnet build -m:1 and the harness; [tests](docs/validation/part-005/tests.json) | **169 passed, 0 failed**, including all 46 direct MFA cases; build 0 warnings/errors |
| Examples, incremental/negative/clean builds, packages and local CLI | Same script; [integration](docs/validation/part-005/integration-results.json) | All checks Passed; expected failure with preprocessing disabled; exact Hello World output |
| Prior published CI checkpoints | Read-only GitHub Actions API | 3860695 and f428c9f both succeeded; [f428c9f run](https://github.com/develmax/Erlang.Net/actions/runs/37933537842). This is predecessor evidence, not CI evidence for this new commit |
| Differential oracle | 37-case runner builds; exact local oracle absent | Not run at this checkpoint; no oracle result invented |
| Documentation/evidence/reference hygiene | Local links and test IDs checked, git diff --check, reference checkout status | Checked before checkpoint commit; reference checkout unchanged |

### Limitations and handoff

Complete map module coverage, binary syntax, full guard set, faithful exception stack traces and live oracle results remain pending. Performance measurements were not rerun and no new performance claim is made. Updated PROGRESS, NEXT_STEPS, compatibility JSON/matrix and semantic differences; immutable reports preserve previous parts.

Commit title: `Add map guard BIFs and differential error comparisons`. Publish to origin/main, then attempt the existing manual pinned-OTP differential workflow and record actual results in the next part. Continue binary source syntax after oracle discrepancies are resolved.

## Part 006 — 2026-10-09 — Bitstring construction and headless oracle repair

**Status:** completed for the documented construction subset; Partially compatible. Full Erlang/OTP scope remains unfinished.

### Changes and decisions

Added Expr.Bits/BitSegment and a segment parser in [Syntax.cs](src/Erlang.Compiler/Syntax.cs), scope/guard traversal in [Semantics.cs](src/Erlang.Compiler/Semantics.cs), synchronous guard and asynchronous evaluation in [Execution.cs](src/Erlang.Compiler/Execution.cs), and AST/literal emission in [CodeGeneration.cs](src/Erlang.Compiler/CodeGeneration.cs).

[BitConstruction.cs](src/Erlang.Compiler/BitConstruction.cs) implements immutable bit assembly for integer and binary segments: default sizes, explicit sizes/units, low-bit truncation including negative integers, big/little/native byte order, non-octet tails, whole/prefix binary interpolation and bitstring aliases. Native order follows the CLR host CPU. Explicit all is supported for binary sizes. A zero-sized integer contributes no bits. Whole binary interpolation must satisfy the segment unit; explicit prefixes cannot exceed the source bit length. Invalid values/sizes produce badarg; representation sizes exceeding the current int capacity produce system_limit.

The parser preserves identical repeated specifiers and rejects conflicting ones. bytes implies binary unit 8, while bits/bitstring imply binary unit 1. Explicit integer units require a size, as established by erl_bits defaults. Bare string literals expand to 8-bit integer segments; string modifiers, float/UTF segments and bitstring patterns remain explicitly diagnosed as unsupported. Nontrivial segment values/sizes are supported through parenthesized expressions.

Added **27 permanent tests**, plus compiled .erl and hybrid cases in [map_source.erl](examples/HelloHybrid/map_source.erl) and [Program.cs](examples/HelloHybrid/Program.cs). Tests exercise truncation, signed values, little-endian partial octets, native order, sizes/units, string literals, binary prefixes/interpolation/all, malformed operands, conflicting/default specifiers, pending forms and guard/map-key construction. The differential runner now contains **57 cases**, including twenty new bit construction values/errors; it has not yet produced reference results.

Reuse audit: existing immutable BitString, BCL BigInteger and the dedicated compiler pipeline; no new dependency or copied implementation. Inspected pinned Bit Syntax Expressions, lib/stdlib/src/erl_bits.erl and HOWTO/INSTALL.md. See [dependency decisions](docs/dependency-decisions.md).

### Remote oracle diagnosis and repair

Dispatched the development-only oracle workflow at b15da2f. [Run 37936825719](https://github.com/develmax/Erlang.Net/actions/runs/37936825719) failed before differential comparison: debugger's dbg_wx_filedialog_win.erl declared wx_object, but wx had been disabled; OTP treats the resulting warning as an error. Source/install guidance confirms that skipped applications' dependencies are not automatically handled.

Updated [.github/workflows/differential.yml](.github/workflows/differential.yml) to exclude debugger, observer and et alongside wx, based on pinned wx_object declarations. The reference source SHA/version is unchanged; GUI components are excluded only from the development oracle, not from the final product scope. No warnings-as-errors bypass was introduced. [Failure record](docs/validation/part-006/oracle-build-attempt.json) preserves the attempted head and cause. A repaired run is required before claiming differential results.

### Validation

| Check | Command / saved evidence | Result |
| --- | --- | --- |
| Full solution and permanent regressions | pwsh -File tools/validate.ps1; [tests](docs/validation/part-006/tests.json) | **196 passed, 0 failed**; 46 direct MFA cases; build 0 warnings/errors |
| .erl/hybrid/incremental/negative/clean/package/tool | Same script; [integration](docs/validation/part-006/integration-results.json) | All checks Passed; disabled preprocessing failed as expected; output remained Hello World |
| Predecessor remote CI | [b15da2f Windows run](https://github.com/develmax/Erlang.Net/actions/runs/37936812771) | Confirmed successful, not evidence for this new head |
| First real oracle attempt | Run 37936825719 / saved failure record | Build failure; comparison skipped; no compatibility result |
| Hygiene | git diff --check, local links/test evidence, unchanged reference checkout | Checked before commit |

### Limitations and next steps

Binary patterns, float/UTF segments, string modifiers, complete segment evaluation/error precedence, full source spans and optimized storage remain pending. Integer assembly currently uses BigInteger shifts per bit; no performance claim or benchmark rerun. Current representation/resource limits are not proven equivalent to BEAM system limits. Previous runtime/OTP/distribution gaps remain.

Updated compatibility JSON/matrix, semantic differences, progress and next steps; part reports are immutable. Commit title: `Add integer and binary bitstring construction and repair headless oracle`. Publish this checkpoint, rerun the pinned oracle workflow and resolve discrepancies before broad compatibility promotion. Next language unit: bitstring patterns and segment-size binding rules.

## Part 007 — 2026-10-09 — Bitstring predicates and size guards

**Status:** completed for the three BIF contracts; Partially compatible pending oracle/full resource and stacktrace evidence.

### Objective and changes

Added erlang:is_bitstring/1, bit_size/1 and byte_size/1 to [CoreModules](src/Erlang.Runtime/Modules.cs) and the [guard whitelist](src/Erlang.Compiler/Semantics.cs). is_bitstring accepts binaries and non-octet bitstrings; bit_size returns the number of bits; byte_size returns the rounded-up storage byte count even for partial bytes. Invalid size operands raise badarg. A long intermediate prevents rounding overflow within the current representation.

Added three direct MFA cases and seven semantic tests in [the harness](tests/Erlang.Tests/Program.cs): empty/1/8/9-bit sizes, predicate distinctions, badarg cases, qualified guards, guard-error alternatives and a byte_size-derived map pattern key. [map_source.erl](examples/HelloHybrid/map_source.erl) and [the example](examples/HelloHybrid/Program.cs) exercise all three in a generated-module guard, also executed by the PackageReference consumer. Registered MFAs now total **49**.

Added five value/error cases to [the differential runner](tools/Erlang.Differential/Program.cs); its current corpus contains **62 cases**. The already running oracle workflow uses the earlier dc91e8c head with **57**, so it does not verify these new BIFs until a later run.

### Reference and reuse

Inspected exact OTP-29.1.1 erl_bif_guard.c bit_size_1/byte_size_1, erl_bif_op.c is_bitstring_1 and erl_internal guard/type-test declarations. Source confirms that byte_size accepts any bitstring and rounds through NBYTES. Reused BitString identity/BitLength and existing Boolean/Integer terms. No new package or copied code; [audit](docs/dependency-decisions.md) updated.

### Validation

| Check | Command / saved report | Result |
| --- | --- | --- |
| Solution and permanent tests | pwsh -File tools/validate.ps1; [tests](docs/validation/part-007/tests.json) | **206 passed, 0 failed**; all 49 direct MFA cases; 0 build warnings/errors |
| .erl/hybrid/incremental/disabled/clean/package/CLI | Same script; [integration](docs/validation/part-007/integration-results.json) | Passed; expected failure without preprocessing; exact Hello World |
| Predecessor remote CI | [dc91e8c Windows run](https://github.com/develmax/Erlang.Net/actions/runs/37938137851) | Confirmed success; this does not attest the new head |
| Repaired oracle attempt | [run 37938184184](https://github.com/develmax/Erlang.Net/actions/runs/37938184184); [observed checkpoint](docs/validation/part-007/remote-checkpoint.json) | Source build in progress at dc91e8c; comparison pending, no oracle results claimed |
| Evidence/hygiene | Local links/test IDs and git diff --check; reference checkout status | Checked before commit; reference unchanged |

### Limitations and handoff

Current bit lengths remain int-sized; larger BEAM/resource limits and full error stacks are unverified. Binary patterns, float/UTF segments, string modifiers, complete library coverage and the other prior gaps remain. No new benchmark or performance claim.

Commit title: `Add bitstring predicates and size guard BIFs`. Publish the checkpoint, inspect the repaired oracle run's actual result and artifact, fix discrepancies and record the tested head precisely. Continue bitstring patterns with guard size expressions and prior-segment binding rules. Current-state and immutable per-part reports have been updated.

## Part 008 — 2026-10-09 — Unicode oracle transport and partial evidence

**Status:** completed for the runner repair and local validation; complete reference comparison pending. Full assignment remains unfinished, approximately 5% by the existing coarse engineering estimate.

### Findings and changes

[Remote run 37938184184](https://github.com/develmax/Erlang.Net/actions/runs/37938184184) successfully built OTP-29.1.1 at the exact pinned reference SHA. At implementation head dc91e8c9e63bb2b4cf97ea6961d5113f2333d2fb, its first **16 comparisons passed**, including arithmetic, exact/numeric and signed-zero equality, large numeric comparison, lists, repeated-variable patterns, guard alternatives, zero-timeout receive and anonymous functions. Comparison then aborted on the supplementary Unicode atom case: the native -eval argument was decoded incorrectly and erl_scan rejected an illegal character. Maps/bitstrings were not reached. The old runner emitted no JSON after an infrastructure failure. [Saved evidence](docs/validation/part-008/remote-oracle-attempt.json) explicitly identifies these results as log-derived, incomplete, and from the earlier 57-case head; no semantic failure was observed in those 16 cases.

Added [OracleProtocol.cs](tools/Erlang.Differential/OracleProtocol.cs): source is encoded as Base64 UTF-8, the native command remains ASCII, and the oracle explicitly converts decoded bytes to Unicode codepoints before erl_scan/erl_parse/erl_eval. This preserves quotes, backslashes, newlines and supplementary characters independently of native argument decoding. Result transfer remains ETF/Base64.

[The runner](tools/Erlang.Differential/Program.cs) now writes evidence after every completed case and on infrastructure exceptions. Reports include exact-version verification, planned/executed/pass/fail counts, completion flag, aborted source and infrastructure error. Infrastructure failures return 2, semantic mismatches return 1, and a complete match returns 0; zero mismatches in an incomplete report never imply success.

Two permanent tests compile the same protocol source into [the regression harness](tests/Erlang.Tests/Program.cs), checking ASCII transport and UTF-8 roundtrips for supplementary/BMP Unicode, quoting and newlines. [validate.ps1](tools/validate.ps1) additionally verifies an unavailable executable returns 2 and preserves an incomplete report with zero executed cases. No production code or MFA was added; registry remains 49.

### Reference and reuse audit

Inspected pinned init.erl argument decoding and unicode.erl/erl_scan/erl_parse/erl_eval public contracts. Reused BCL UTF-8/Base64 and development oracle APIs, with no new dependency or copied source; [dependency decision](docs/dependency-decisions.md). The official sibling checkout remains unchanged, baseline unchanged, and production execution requires no BEAM runtime.

### Validation

| Check | Command / evidence | Actual result |
| --- | --- | --- |
| Full solution and permanent regression harness | pwsh -File tools/validate.ps1; [tests](docs/validation/part-008/tests.json) | **208 passed, 0 failed**, 49 direct MFA cases; build 0 warnings/errors |
| Generated .erl/hybrid, incremental, disabled preprocessing, clean rebuild, local packages/consumer/tool | Same script; [integration](docs/validation/part-008/integration-results.json) | Passed; exact Hello World and expected disabled-preprocessing failure |
| Missing oracle infrastructure reporting | Same script; [negative report](docs/validation/part-008/oracle-unavailable.json) | Expected exit 2; incomplete, version not verified, 0 executed cases, error saved |
| Prior remote reference build and comparison | Run 37938184184 / saved evidence | Build succeeded; 16 passes, then infrastructure abort; 41 planned cases unexecuted |
| Predecessor Windows CI | [5a1e5cc run 37938840512](https://github.com/develmax/Erlang.Net/actions/runs/37938840512) | Success; predecessor evidence only |
| Hygiene | git diff --check, machine-readable evidence and reference status | Checked before commit; official reference unchanged |

### Limits and next step

The repaired Unicode command has local protocol evidence but still needs actual OTP execution. Current corpus has 62 cases; this part does not claim they passed. Stack traces, signals, side effects, bitstring patterns/remaining segment types and the wider runtime/OTP/distribution gaps remain pending. No benchmark rerun or new performance claim. Updated current progress/compatibility/semantic boundaries and preserved immutable reports.

Commit title: `Repair Unicode oracle transport and preserve partial differential reports`. Publish to origin/main, dispatch the existing pinned oracle workflow on the new head, inspect every result and fix genuine mismatches before continuing binary patterns. Record new-head CI and oracle outcomes in the next English work-part entry.

## Part 009 — 2026-10-09 — Bitstring matching and corrected Unicode diagnosis

**Status:** completed for the documented integer/binary pattern subset and quoted-literal fix; Partially compatible. Full assignment remains unfinished, approximately 5% by the existing coarse estimate.

### Language changes

Added [BitPattern.cs](src/Erlang.Compiler/BitPattern.cs), integrated with [parser](src/Erlang.Compiler/Syntax.cs), [scope/guard analysis](src/Erlang.Compiler/Semantics.cs) and [generated C# AST emission](src/Erlang.Compiler/CodeGeneration.cs). Patterns now extract signed/unsigned integers in big/little/native order, including partial octets and zero width. They extract binary prefixes/tails at unaligned bit offsets, enforce sizes/units, match exact integer literals without construction truncation, and support strings expanded to byte literals.

Size expressions use the guard subset and bindings from preceding segments. A fun head can read a captured size in its first segment, then use the newly extracted shadowing variable in later sizes, as in fun(<<L:L,B:L>>) with outer L=8 and extracted L=16. Forward/unbound size variables and nonguard calls are diagnosed. An unsized binary segment must be last. Nested segment patterns remain diagnosed as unsupported. Existing transactional Pattern.Match prevents bindings from leaking when a later segment fails; selective receive retains earlier nonmatching messages.

BitSegment now retains signedness and generated code preserves it; construction still uses low-bit truncation. The parser translates source bit syntax to dedicated patterns. [map_source.erl](examples/HelloHybrid/map_source.erl) adds unpack/1 with prior-segment width, signed little-endian extraction and partial tails; [the hybrid example](examples/HelloHybrid/Program.cs) matches bits and exercises unpack/1, also through the real PackageReference consumer.

There are **31 permanent pattern tests** (one replaces the previous pending-pattern diagnostic), covering all listed boundaries, captured size shadow/repeated bindings, invalid sizes, exact literals, nested map values, badmatch reasons, transactional rollback and ordered selective receive. Two additional lexer tests bring the net increase to **32**, total **240**. The differential corpus adds **28 pattern/value/error cases**, total **90**; these new cases have not yet run against OTP.

### Dated correction to part 008 and actual remote evidence

Part 008's inference that native argument decoding caused the Unicode failure was not established. [Run 37940002401](https://github.com/develmax/Erlang.Net/actions/runs/37940002401), at 4a8c379b7fed318af3122f1b5e089c2abcf73534, again built the exact reference, matched **16 of 62 planned cases**, and stopped on the same Unicode test even with explicit UTF-8 decoding. Pinned erl_scan.erl's UNICODE macro excludes U+FFFE and U+FFFF. The corpus contained a literal U+FFFF; this is the confirmed source error. Earlier entries and immutable reports are retained as history, with this correction superseding their causal diagnosis.

Replaced that corpus literal with valid U+FFFD, retaining the supplementary-versus-BMP ordering boundary. Our quoted-literal scanner now rejects U+FFFE/U+FFFF and malformed surrogate sequences, matching the audited reference range; direct term values remain separate from source literal legality. The ASCII UTF-8 transport is retained for deterministic source transfer. Unicode validation outside quoted literals and full escape support remain pending.

The repaired runner did preserve a real incomplete JSON artifact this time: [original report](docs/validation/part-009/oracle-4a8c379.json), [head/run/artifact metadata](docs/validation/part-009/remote-checkpoint.json). Downloaded artifact ZIP SHA256 matched GitHub's digest ac9c979e880a0d400a1dbdbe6bcc3175224b725828197f002d6233dd5883787b. This verifies partial-report persistence and 16 actual matches, not a complete corpus match; maps/bitstrings were still not reached.

### Reference, reuse and validation

Inspected pinned expressions.md matching/guard-size/binary-unit contracts, compiler bs_match_SUITE.erl size_shadow_1/2/3, and erl_scan.erl UNICODE. Reused BitString, BigInteger, transactional patterns and guard evaluation. No package or copied reference implementation; [audit](docs/dependency-decisions.md). Production remains independent of BEAM and the reference checkout remains unchanged.

| Check | Exact command / saved evidence | Result |
| --- | --- | --- |
| Full solution and permanent regressions | pwsh -File tools/validate.ps1; [tests](docs/validation/part-009/tests.json) | **240 passed, 0 failed**, 49 direct MFAs; build 0 warnings/errors |
| Generated .erl/hybrid/incremental/disabled/clean/package consumer/local CLI | Same script; [integration](docs/validation/part-009/integration-results.json) | Passed, exact Hello World; disabled preprocessing and missing oracle failed as expected |
| Predecessor Windows CI | [4a8c379 run 37939962905](https://github.com/develmax/Erlang.Net/actions/runs/37939962905) | Success; predecessor only |
| Remote oracle at earlier 4a8c379 head | Run 37940002401 / real artifact | 16 matches, 0 observed semantic mismatches, source-error abort; 46 cases unexecuted |
| Hygiene | git diff --check, JSON/test-evidence/link checks, reference status | Checked before commit; official checkout unchanged |

Initial validation failures were corrected before this checkpoint: new fixtures used comma sequences at a top-level single-expression entry point and an error fixture used the success-only helper. Fixtures now use case bodies or the existing captured-error helper. The generated example was changed to native hybrid syntax for its packed value. Final full validation passed.

### Boundaries and next work

Current representation lengths remain int-sized. Extraction shifts per bit and binary slices copy; no performance or resource-limit equivalence claim. Float/UTF segments, string modifiers, complete pattern-scope combinations, escapes/diagnostic spans and the wider runtime/OTP/distribution scope remain pending. No benchmark rerun. All 49 MFAs/features retain their prior partial status; no broad promotion from 16 matches.

Updated progress, compatibility, semantic boundaries, next steps and README; immutable reports preserve both local results and the actual failed oracle attempt. Commit title: `Add bitstring patterns and align quoted Unicode source validation`. Publish this tested part and dispatch the corrected 90-case corpus on its exact head; inspect all comparisons, fix mismatches, then add remaining segment types and pattern-scope cases.

## Part 010 — 2026-10-09 — Clause-local anonymous fun capture

**Status:** completed for the capture fix and local validation; Partially compatible. Full project remains unfinished, approximately 5% by the existing coarse estimate.

### Defect and fix

Auditing the new binary fun-head cases exposed an existing closure bug: [Execution.cs](src/Erlang.Compiler/Execution.cs) collected head names from every fun clause and removed them from the captured scope before trying any clause. A name bound only in a different clause therefore lost its outer captured value. For example, with outer X=42, fun({X}) -> X; (_) -> X end could not return captured 42 after the tuple head failed.

Each attempted clause now starts with a fresh copy of the captures and removes only that clause's own head names. Pattern matching and guard selection continue through the existing transactional selector; captured key/bit-size scope remains separately available. Clause order and function_clause on exhaustion are preserved. No new MFA or external dependency.

Added three permanent [regressions](tests/Erlang.Tests/Program.cs): capture after tuple-head mismatch, after a shadowing clause's guard failure, and after a binary head with two shadowing names fails. All pass. [Differential corpus](tools/Erlang.Differential/Program.cs) grows from 90 to **93** with the corresponding cases.

### Audit and validation

Inspected the pinned expressions.md Fun Expressions contract and existing clause analysis/selection/capture code. Reused C# dictionaries, transactional matching and guard evaluation; no copied source or package. See [dependency decisions](docs/dependency-decisions.md). Official reference checkout and baseline remain unchanged.

| Check | Command / saved evidence | Actual result |
| --- | --- | --- |
| Full solution and permanent harness | pwsh -File tools/validate.ps1; [tests](docs/validation/part-010/tests.json) | **243 passed, 0 failed**, 49 direct MFAs; build 0 warnings/errors |
| Generated examples, incremental/negative/clean, PackageReference consumer and local CLI | Same script; [integration](docs/validation/part-010/integration-results.json) | Passed; exact Hello World; expected disabled-preprocessing and unavailable-oracle failures |
| Predecessor Windows CI | [8d35ea8 run 37941788800](https://github.com/develmax/Erlang.Net/actions/runs/37941788800) | Confirmed success; predecessor evidence only |
| Corrected remote 90-case oracle | [run 37941798273](https://github.com/develmax/Erlang.Net/actions/runs/37941798273), head 8d35ea8bf4b684be1c6226f447ae4ce01b85f845 | Build in progress when this checkpoint was prepared; excludes the three new cases, no new result claimed |
| Hygiene | JSON/evidence checks, git diff --check and reference status | Checked before commit; official checkout unchanged |

### Limits and next step

Named/external fun forms, complete scope combinations, bit float/UTF/string modifiers and all earlier runtime/OTP/distribution boundaries remain pending. No benchmark rerun or broad compatibility promotion. Current progress, compatibility and next steps updated; immutable local reports saved.

Commit title: `Preserve captured variables independently in anonymous fun clauses`. Publish the tested fix; inspect the already running 90-case oracle's actual report, resolve any mismatches, then run all current 93 cases and record that separate head/result. Next language work remains complete bit-pattern scopes and remaining segment types.

## Part 011 — 2026-10-09 — Bit-size grammar and 52 real OTP comparisons

**Status:** completed for the grammar correction and recorded evidence; overall Partially compatible. Complete current corpus comparison remains pending. Full assignment remains approximately 5% by the coarse estimate.

### Actual oracle result and diagnosed discrepancy

[Run 37941798273](https://github.com/develmax/Erlang.Net/actions/runs/37941798273), implementation head 8d35ea8bf4b684be1c6226f447ae4ce01b85f845, built the exact pinned OTP-29.1.1 reference and matched **52 of 90 planned comparisons**, with zero observed semantic mismatches. It reached maps, exact numeric map keys, map construction/updates/pattern scopes, map guard predicates/access/errors, error/throw/exit class and reason, valid Unicode ordering and integer/binary construction with sizes/units/byte order/truncation/prefixes and badarg.

The run then aborted at source <<1:-1>> because OTP's parser rejected the unparenthesized prefix operator in the size. Pinned lib/stdlib/src/erl_parse.yrl distinguishes bit_expr (prefix_op expr_max allowed for the value) from bit_size_expr (expr_max only). The corpus intended to test a runtime negative-size error and must use <<1:(-1)>>. Our parser incorrectly accepted the original spelling; this was a real source-grammar discrepancy in addition to an invalid oracle fixture.

The original [JSON report](docs/validation/part-011/oracle-8d35ea8.json) and [run/head/artifact metadata](docs/validation/part-011/remote-checkpoint.json) preserve exact partial results. Downloaded ZIP SHA256 matched GitHub digest 37fe2e5f6ca26db9002a29b08ed8ee0f31640018f2d8dc667b2bac7bb20ab525. The remaining **38** cases at that head were not executed; all bit patterns and bit-size BIF cases are after the abort. The three newer part 010 capture cases were not in that run. No complete 90/93-case or full-feature compatibility claim.

### Changes and regression protection

[Syntax.cs](src/Erlang.Compiler/Syntax.cs) now rejects unparenthesized +, -, not and bnot in segment sizes; parenthesized unary sizes use existing semantic validation, and unary segment values remain accepted. Updated the negative construction fixture/corpus to <<1:(-1)>> and the invalid pattern-size alternative fixture to a parenthesized size.

Added a permanent [grammar regression](tests/Erlang.Tests/Program.cs) rejecting all four unparenthesized forms, accepting a parenthesized positive size and preserving <<-1>> construction. Registry remains **49 MFAs**; differential corpus remains **93**, with corrected source. Reused the existing parser/diagnostic path without new dependencies or copied grammar source; [audit](docs/dependency-decisions.md). Reference checkout and production BEAM independence are preserved.

### Executed validation

| Check | Exact command / evidence | Actual result |
| --- | --- | --- |
| Full solution and permanent harness | pwsh -File tools/validate.ps1; [tests](docs/validation/part-011/tests.json) | **244 passed, 0 failed**, 49 direct MFAs; build 0 warnings/errors |
| .erl/hybrid/incremental/negative/clean/packages/consumer/local CLI | Same script; [integration](docs/validation/part-011/integration-results.json) | Passed; exact Hello World; expected disabled-preprocessing and unavailable-oracle failures |
| Remote comparison at 8d35ea8 | Run 37941798273 / real report | 52 matched, source-grammar abort; 38 unexecuted cases |
| Predecessor Windows CI | [074babb run 37942660273](https://github.com/develmax/Erlang.Net/actions/runs/37942660273) | Success; predecessor evidence only |
| Evidence/hygiene | JSON/test-evidence checks, artifact digest, local report paths, git diff --check, reference status | Checked before commit; reference unchanged |

### Limits and continuation

Updated progress, compatibility and next steps to distinguish the real 52-case evidence from unexecuted patterns, size BIFs and new captures. All features/MFAs retain partial status. Full stacks/signals/side effects, complete scopes, float/UTF/string modifiers, runtime/OTP/distribution services and all previous boundaries remain pending. No benchmark rerun or performance claim.

Commit title: `Enforce bit-size primary grammar and record OTP comparison evidence`. Publish this tested checkpoint and dispatch the corrected current **93-case** oracle run. Save its actual head/report and resolve failures before broadening the language subset. English historical entries and immutable earlier reports are retained.

## Part 012 — 2026-10-09 — Complete 93-case reference comparison

**Status:** completed for this development corpus, with **93/93 matched**. Broad feature/MFA compatibility remains partial; full assignment remains approximately 5% by the coarse engineering estimate.

[Run 37943457369](https://github.com/develmax/Erlang.Net/actions/runs/37943457369) successfully built pinned OTP-29.1.1 at ad05823719d77c8faee87348ea39513d4e2f99c5 and compared implementation head c139b45bebca29b7560d84cc747bcf8443f7e97c. The [original JSON](docs/validation/part-012/differential.json) records VersionVerified=true, Complete=true, Planned=Executed=Passed=93, Failed=0, no aborted source or infrastructure error. [Metadata](docs/validation/part-012/remote-checkpoint.json) records exact heads, job/artifact IDs and digest. Downloaded artifact SHA256 matched GitHub: 85fae17424dd9c064bd04d276e9b24b51dcdbc011d808b2802c66a3c4e84d76f.

This first complete corpus covers the existing arithmetic/equality/list/guard cases, valid Unicode ordering, map construction/updates/pattern scopes/exact keys, map guard BIF values/errors, exception classes/reasons, integer/binary bit construction, bit-size BIFs, signed/unsigned and endian bit patterns, prior-segment/captured size bindings, mismatch/error paths and clause-local anonymous fun captures. No semantic mismatch was found in these 93 cases. Earlier invalid-source attempts remain immutable history in parts 006–011.

The same head passed [Windows CI run 37943449681](https://github.com/develmax/Erlang.Net/actions/runs/37943449681), which executes tools/validate.ps1: **244 local tests**, full build, generated examples, incremental/disabled/clean checks and real local-package consumer/tool. This is exact-head remote evidence, not a new local test execution in this documentation-only part. No benchmark rerun, new production code/MFA/dependency or copied implementation.

Updated current progress, compatibility JSON and semantic boundaries to point to the complete corpus and retain full-scope limitations. The actual oracle was development-only; production remains independent of BEAM, baseline unchanged, official sibling checkout unchanged. JSON/count/artifact-digest/evidence and git diff --check checks performed before commit. No runtime regression rerun was necessary for evidence-only documentation changes.

Full stack traces, asynchronous signals, side effects, complete applicable inputs and OTP reference suites remain unverified. No full feature/module is promoted to Verified compatible solely from selected corpus cases. Next executable unit: float bit segments with 16/32/64 sizes, endianness, finite-value/error handling and generated .erl/hybrid tests, followed by UTF/string modifiers and broader scope contracts. Commit title: `Record complete pinned OTP differential corpus results`; publish the evidence checkpoint before the next implementation part.

## Part 013 — 2026-10-09 — Float bit construction and patterns

**Part status:** completed for the implemented local subset; remote float comparison pending. Full assignment remains approximately **5%**, a coarse engineering estimate rather than measured compatibility.
**Objective and scope:** replace the unsupported float-segment diagnostic with C# construction and matching for binary16/32/64, preserving byte order, numeric conversion and finite-term semantics through source compilation and hybrid/package integration.

### Changes

- [Syntax.cs](src/Erlang.Compiler/Syntax.cs) accepts float specifiers/default 64 bits and diagnoses a numeric unit without an explicit size. [BitConstruction.cs](src/Erlang.Compiler/BitConstruction.cs), [BitPattern.cs](src/Erlang.Compiler/BitPattern.cs) and new [BitFloat.cs](src/Erlang.Compiler/BitFloat.cs) implement 16/32/64-bit big/little/native construction, unaligned extraction, finite-only decoding, prior-segment sizes, units, numeric literals and signed zero. Zero-width float patterns bind positive zero; zero-width construction raises badarg.
- Reuse of a raw BigInteger-to-double cast initially truncated values (9007199254740995 became 9007199254740994). Added explicit nearest-even rounding with sticky-bit handling, carry and finite-double overflow boundaries for construction and integer pattern literals. Half conversion rounds directly from double, avoiding the single-precision intermediate trap.
- Narrowing finite double values can produce infinity bit encodings, which remain binary data. Matching infinity/NaN fails without creating an invalid FloatTerm. Other clauses can still match the input.
- Added **27 permanent tests**, including all **63,488 finite half encodings**, subnormal/direct-rounding vectors, signed zero, integer ties/sticky bits/max-double boundary, numeric literal versus bound integer matching, nonfinite/short/invalid segments and guard/map-key usage. Replaced the old combined float/UTF unsupported test with the remaining UTF diagnostic. One new test fixture used unsupported bsl; replaced it with an explicit BigInteger literal before final validation.
- [map_source.erl](examples/HelloHybrid/map_source.erl) and [Program.cs](examples/HelloHybrid/Program.cs) exercise generated unaligned float matching and hybrid signed-zero matching, including the real PackageReference consumer and installed local CLI. Added **30 float cases** to the development differential runner: **123 planned cases** total. No new MFA: registry remains **49**.

### Decisions and compatibility

Pinned OTP-29.1.1 sources inspected: expressions manual, erl_bits.erl, erl_bits.c get/put/FP16 helpers, big.c big_to_double, v3_core.erl integer pattern coercion, bs_construct_SUITE.erl fp16/coerce_to_float and bs_match_misc_SUITE.erl. Reused audited .NET Half, BitConverter, BinaryPrimitives, BigInteger and Math.ScaleB; no new dependency or copied implementation. [Dependency/license audit](docs/dependency-decisions.md) records direct double-to-half source evidence and why raw BigInteger conversion is unsuitable.

Float segments remain **Partially compatible**: local/source evidence does not establish the entire bit-syntax contract. UTF/string modifiers, full size/scope/error precedence, complete diagnostics/reference suites and int-sized representation/resource boundaries remain open. No BEAM production dependency, benchmark rerun or performance equivalence claim. Official sibling checkout unchanged. The historical 93-case oracle report at c139b45 excludes this new implementation and must not be used as float verification.

### Validation

| Check | Exact command / saved report | Actual result |
| --- | --- | --- |
| Full solution and permanent harness | pwsh -File tools/validate.ps1; [tests](docs/validation/part-013/tests.json) | **271 passed, 0 failed**, 49 direct MFAs; build 0 warnings/errors |
| Generated .erl/hybrid, incremental/negative/clean, local packages/consumer/CLI | Same script; [integration](docs/validation/part-013/integration-results.json) | Passed; exact Hello World, float assertions executed, expected disabled-preprocessing and unavailable-oracle failures |
| Historical exact-version corpus | [Part 012 report](docs/validation/part-012/differential.json), run 37943457369 | 93/93 at c139b45; excludes floats, not rerun in this part |
| Predecessor Windows CI | [d25bfe2 run 37950629019](https://github.com/develmax/Erlang.Net/actions/runs/37950629019) | Confirmed success; predecessor evidence only |
| Evidence/hygiene | JSON counts/evidence identifiers, git diff --check, reference status | Checked before commit |

The final full validation followed the rounding fix. Earlier 268/269 fixture failure and intermediate 269/269 pass are diagnostic history, not final checkpoint evidence. Local Erlang executable is absent; the new remote 123-case comparison will run after publication. Full reference suites, stacks/signals/side effects and performance comparison remain unrun.

### Handoff

Updated PROGRESS, COMPATIBILITY, compatibility JSON/matrix, semantic differences and NEXT_STEPS. Commit title: `Implement float bit segments with exact numeric rounding`. Publish the validated checkpoint to authorized origin/main, dispatch the 123-case pinned oracle run, preserve the actual tested-head report and resolve any mismatches before extending UTF/string syntax. Public NuGet publication remains deferred. Publication hashes and subsequent remote evidence are obtained from Git history and the next numbered evidence entry.

## Part 014 — 2026-10-09 — Strict UTF segments and confirmed float oracle

**Part status:** completed for the implemented local UTF subset and the 123-case float oracle corpus. UTF remote comparison pending. Full assignment remains approximately **5%** by a coarse engineering estimate.
**Objective and scope:** add UTF8/16/32 source construction/patterns and UTF literal strings, using scalar values rather than replacement decoding; preserve reference evidence for the published float predecessor.

### Changes

- New [BitUnicode.cs](src/Erlang.Compiler/BitUnicode.cs) uses Rune scalar validation/strict decoding and explicit UTF16/32 byte order. UTF8 ignores endian. Integer values in 0..D7FF/E000..10FFFF are valid, including noncharacters; invalid/noninteger construction raises badarg, invalid/truncated/overlong/surrogate decoding fails matching. No replacement scalar is substituted.
- [Syntax.cs](src/Erlang.Compiler/Syntax.cs) accepts UTF types and expands UTF literal strings into scalar segments. Numeric size/unit specifiers are diagnosed; literal undefined size follows pinned erl_bits defaults. Other string size modifiers remain unsupported. [BitConstruction.cs](src/Erlang.Compiler/BitConstruction.cs) computes encoded widths; [BitPattern.cs](src/Erlang.Compiler/BitPattern.cs) decodes a bounded prefix at any bit offset and advances only by consumed bytes, reusing transactional binding/rollback.
- **22 new permanent tests**, plus the old unsupported-UTF test replaced by size/unit diagnostics, cover encoding boundaries, surrogate pairs, endian/native, noncharacters, malformed/short/range-invalid input, offsets 1–7, failed binding rollback, prior scalar sizes, literal/bound values, guard/map keys, UTF strings and **1,280 deterministic scalar roundtrips**. [Tests](tests/Erlang.Tests/Program.cs).
- Generated [utf_check/0](examples/HelloHybrid/map_source.erl) and native hybrid string matching in [Program.cs](examples/HelloHybrid/Program.cs) run through the normal build and real PackageReference consumer/local CLI. Added **31 UTF oracle cases**, bringing the current corpus to **154**. MFA registry remains **49**.

### Reference and reuse

Inspected pinned OTP-29.1.1 Unicode/string expressions, erl_bits.erl check_size_unit_1, UTF helpers in erl_bits.c and bs_utf_SUITE.erl boundary/illegal/unaligned tests. Reused audited System.Text.Rune, OperationStatus and BinaryPrimitives, with existing BCL MIT notices; no package or copied implementation. [Audit](docs/dependency-decisions.md) preceded implementation. Current resource/scope/diagnostic/exception-stack limits remain partial; no performance claim or BEAM production dependency.

### Exact-head float evidence

Part 013 was committed/pushed as **19774f9c5e292345afff9d3ec95d0d60bd583060**. [Oracle run 37952290309](https://github.com/develmax/Erlang.Net/actions/runs/37952290309) built the exact pinned reference and completed **123/123 comparisons**, zero mismatches, version confirmed, no abort/infrastructure error. This includes 30 float cases for direct half rounding/subnormals, nearest-even integer conversion/literals, endian/unit/unaligned extraction, zero-width patterns, signed zero, nonfinite rejection, narrow-overflow binary data and guard/map-key usage. This confirms the selected cases, not all bit syntax.

The [original report](docs/validation/part-014/float-differential.json) and [metadata](docs/validation/part-014/float-remote-checkpoint.json) preserve exact heads, IDs and results. Downloaded artifact SHA256 matched GitHub digest **fc54c03bc452004c9d5090f749c1cabcb1b99f7727cf6fa7d10e04f0d28f68ed**. The same head passed [Windows CI run 37952244708](https://github.com/develmax/Erlang.Net/actions/runs/37952244708), including 271 tests and full integration. These remote runs exclude the new UTF implementation and its 31 cases. Earlier 93-case evidence remains historical and immutable.

### Validation

| Check | Exact command / saved report | Actual result |
| --- | --- | --- |
| Full solution and permanent harness | pwsh -File tools/validate.ps1; [tests](docs/validation/part-014/tests.json) | **293 passed, 0 failed**; build 0 warnings/errors; 49 direct MFAs |
| Generated .erl/hybrid, incremental/negative/clean, packages/consumer/local CLI | Same script; [integration](docs/validation/part-014/integration-results.json) | Passed; exact Hello World and UTF/float assertions; expected negative checks |
| Pinned oracle at predecessor 19774f9 | Run 37952290309 / original report | **123/123 matched**, complete; excludes UTF |
| Windows CI at predecessor 19774f9 | Run 37952244708 | Success; excludes UTF |
| Evidence/hygiene | JSON counts/evidence/path checks, artifact SHA256, git diff --check, reference status | Checked before commit; official reference unchanged |

Local oracle remains absent. New 154-case UTF comparison will run after publication. Full reference suites, stacks/signals/side effects and performance comparison remain unrun. Broad features/MFAs remain Partially compatible.

### Handoff

Updated current progress/compatibility/matrix/JSON/semantic boundaries/next steps to distinguish 123 confirmed predecessor cases from 31 pending UTF additions. Commit title: `Implement strict UTF bit segments and record float oracle results`. Publish this validated part, dispatch the 154-case pinned run, preserve exact-head evidence and fix any discrepancies. Next language unit is non-UTF literal-string size modifiers with source/error contracts. No public NuGet publication.

## Part 015 — 2026-10-09 — Shared nearest-even numeric coercion

**Part status:** completed for the repaired local numeric subset; new arithmetic oracle cases pending. Full assignment remains approximately **5%**, a coarse estimate.
**Objective and scope:** remove the raw BigInteger casts still used by runtime arithmetic after the part 013 float-segment fix. Preserve per-operation error contracts while using one audited conversion.

### Changes and defect

- [Integer.TryToDouble](src/Erlang.Terms/Term.cs) now owns finite nearest-even conversion, including ties/sticky bits, mantissa carry and the maximum-double overflow boundary. False indicates conversion failure; no nonfinite FloatTerm is created.
- [CoreModules.Arithmetic](src/Erlang.Runtime/Modules.cs) uses it for mixed integer/float operands and integer `/`. Previously `9007199254740995+0.0` truncated to 9007199254740994 instead of rounding to 9007199254740996. A huge denominator could become infinity and incorrectly return zero; reference arithmetic rejects a nonrepresentable operand before evaluation with badarith. Added per-operand checks, preserving guard failure rather than a successful zero result.
- [BitFloat.cs](src/Erlang.Compiler/BitFloat.cs)/[BitPattern.cs](src/Erlang.Compiler/BitPattern.cs) reuse the same conversion for construction/integer literals. Construction retains badarg and out-of-range pattern literals retain a compile diagnostic. Exact numeric term comparison still uses rational comparison and is unchanged.
- **Six new permanent tests** cover direct nearest-even/overflow boundaries, mixed operators/left-right coercion/cancellation, integer division, huge operand badarith and guard rejection. [Tests](tests/Erlang.Tests/Program.cs). Added six source/class/reason oracle cases, for **160 planned cases**. No new MFA: **49** remain registered.
- Rewrote current PROGRESS/COMPATIBILITY to remove superseded historical pending/count statements; detailed numbered history and immutable reports remain in CHANGELOG. Current source scope, local counts, tested heads and pending additions are distinct.

### Decisions and compatibility

Before implementation inspected pinned OTP-29.1.1 erl_arith.c mixed plus/minus/times/division and big.c big_to_double. Reused the already audited part 013 rounding implementation and BCL BigInteger/Math.ScaleB; no package, copied source or new license. [Audit](docs/dependency-decisions.md). This fixes the selected conversion gap, not full arithmetic errors/stacks or all numeric contracts. Broad features/MFAs remain Partially compatible. No benchmark rerun/performance claim; official sibling checkout unchanged and production remains BEAM-independent.

### Validation

| Check | Exact command / saved report | Actual result |
| --- | --- | --- |
| Full solution and permanent harness | pwsh -File tools/validate.ps1; [tests](docs/validation/part-015/tests.json) | **299 passed, 0 failed**, 49 direct MFAs; build 0 warnings/errors |
| .erl/hybrid/incremental/negative/clean/packages/consumer/local CLI | Same script; [integration](docs/validation/part-015/integration-results.json) | Passed, including existing exhaustive float/UTF and generated assertions |
| Published UTF predecessor Windows CI | [67ac674 run 37953727459](https://github.com/develmax/Erlang.Net/actions/runs/37953727459) | Confirmed success, 293 tests/full integration; excludes this repair |
| Historical float oracle | [123-case report](docs/validation/part-014/float-differential.json), run 37952290309 at 19774f9 | Complete match; six new arithmetic cases are excluded |
| UTF predecessor oracle | Run 37953748714 at 67ac674 | Exact-reference build in progress when this implementation checkpoint was recorded; no result claimed |
| Evidence/hygiene | JSON/test counts/evidence/path checks, git diff --check, reference status | Checked before commit |

Local Erlang executable remains absent. The 160-case new-head comparison must run after publication; full suites/stacks/signals/side effects/performance remain unrun.

### Handoff

Commit title: `Share exact integer float coercion across runtime and compiler`. Publish the validated repair, dispatch 160-case oracle, save actual reports for both the 154-case UTF predecessor and this head, and resolve mismatches. Current progress/compatibility/matrix/JSON/semantic differences/next steps updated. Non-UTF string size modifiers follow reference verification; remaining runtime/OTP/distribution/IDE work is unchanged. No public NuGet publication.

## Part 016 — 2026-10-09 — Reject explicit UTF sizes and preserve partial evidence

**Part status:** completed for the source-grammar/fixture repair; corrected 160-case remote run pending. Full assignment remains approximately **5%**.
**Objective and scope:** resolve the first actual UTF oracle abort, preserving the original partial report and correcting the earlier source-contract interpretation.

### Evidence and dated correction

[Run 37953748714](https://github.com/develmax/Erlang.Net/actions/runs/37953748714) at **67ac67453dd99b4db6c1970aab7f565858072c3b** built pinned OTP-29.1.1 and matched **132 comparisons**, zero mismatches, then aborted at `<<65:undefined/utf8,65:undefined/utf16>>`. These are the complete preceding 123 cases plus **9 UTF construction cases**: boundaries, UTF16/32 endian/native, UTF8 ignored endian and invalid scalar/value badarg. The remaining **22 UTF cases** (patterns, strings, malformed inputs, binding scopes) were not executed. No complete 154-case or UTF compatibility claim.

[Original report](docs/validation/part-016/utf-partial.json) and [metadata](docs/validation/part-016/utf-remote-checkpoint.json) preserve the abort, exact heads/IDs and actual counts. Artifact ZIP SHA256 verified against GitHub: **99ffac878a4caa8ad472bd664cc142e3e75b90caeb7e74ba7582eb75c4b52413**. Part 014 incorrectly treated erl_bits helper acceptance of undefined as a source-language default spelling. Pinned **erl_lint.erl bit_size/pat_bit_size**, **erl_eval.erl check_command** and **erl_bits.erl check_size_unit_1** show that explicit noninteger sizes become unknown in linting and are rejected; accepted undefined representations are internal defaults. This is a real parser discrepancy plus an invalid oracle fixture, not transport failure.

### Changes and protection

[Syntax.cs](src/Erlang.Compiler/Syntax.cs) no longer normalizes explicit undefined UTF size to omitted size; all explicit sizes/units are rejected. The former positive test is replaced by permanent construction/pattern diagnostics for UTF8/16/32. [Runner](tools/Erlang.Differential/Program.cs) replaces the invalid source with an omitted-size case; planned count stays **160**. Registry stays **49 MFAs**, permanent count **299**. No dependency/copied implementation; [audit correction](docs/dependency-decisions.md).

Part 015 was committed/pushed as **eb621ec0795bbfd4774d57ec3cee7f59e8f3483a**. Its run **37954818924** was cancelled during reference build once this shared invalid fixture was confirmed; it did not test the six new arithmetic cases. The corrected head replaces it. Earlier numbered reports are immutable and earlier claims are corrected here rather than rewritten.

### Validation

| Check | Exact command / saved report | Actual result |
| --- | --- | --- |
| Full solution and permanent harness | pwsh -File tools/validate.ps1; [tests](docs/validation/part-016/tests.json) | **299 passed, 0 failed**, 49 direct MFAs; build 0 warnings/errors |
| Generated .erl/hybrid/incremental/negative/clean/packages/consumer/local CLI | Same script; [integration](docs/validation/part-016/integration-results.json) | Passed; expected unavailable-oracle/disabled-preprocessing failures |
| UTF predecessor exact reference | Run 37953748714 / original partial report | 132 matched, explicit-size lint abort; 22 unexecuted |
| UTF predecessor Windows CI | Run 37953727459 at 67ac674 | Success, 293 tests/full integration; predecessor evidence |
| Evidence/hygiene | Report/digest/count/evidence/path checks, git diff --check, reference status | Checked before commit; reference unchanged |

Full stacks/signals/side effects/reference suites/performance remain unrun; no broad feature/MFA promotion. Local reference executable remains absent.

### Handoff

Updated current PROGRESS/COMPATIBILITY/JSON/semantic differences/NEXT_STEPS with partial tested scope and the correction. Commit title: `Reject explicit UTF sizes and correct oracle fixture`. Publish the repaired checkpoint, dispatch the corrected 160-case comparison, preserve its actual report and fix remaining discrepancies before extending source syntax. No public NuGet publication.

## Part 017 — 2026-10-09 — Complete 160-case pinned oracle checkpoint

**Part status:** completed for this development corpus. Full assignment remains approximately **5%** by a coarse engineering estimate; no whole-feature/MFA promotion.
**Objective and scope:** finish exact-head reference validation of float/UTF/numeric-coercion work and preserve the complete report after the explicit UTF-size repair.

[Run 37955525454](https://github.com/develmax/Erlang.Net/actions/runs/37955525454) at **666c4b5060110068aba4ad043fec7d0ac3388168** built exact OTP-29.1.1 (**ad05823719d77c8faee87348ea39513d4e2f99c5**) and matched **160/160 cases**. The [original JSON](docs/validation/part-017/differential.json) records VersionVerified=true, Complete=true, Planned=Executed=Passed=160, Failed=0 and no aborted source/infrastructure error. [Metadata](docs/validation/part-017/remote-checkpoint.json) preserves the exact head/reference/job/artifact IDs and verified artifact ZIP SHA256 **a16760ea047792287fbdb4fa8a2bd9d35b65637cb51b67aa8f21f2e15e65b2d0**.

The corpus contains the previous 93 expressions, 30 float, 31 UTF and six arithmetic-coercion additions. It now reaches and matches UTF patterns/strings, malformed/overlong/surrogate/short/range-invalid matching, unaligned/endian/scalar/noncharacter cases, prior bindings/rollback and numeric nearest-even/overflow/guard outcomes. This confirms the selected values and exception class/reason cases after the repair. The earlier 132/154 report/invalid-source diagnosis remains immutable in part 016. Cancelled superseded run 37954818924 is not evidence for arithmetic correctness; the new complete run is.

Same-head [Windows CI run 37955516789](https://github.com/develmax/Erlang.Net/actions/runs/37955516789), job 113904905652, passed **299/299 tests** and the full tools/validate.ps1 pipeline. Actual job-log lines confirmed 299/299 passed, zero warnings/errors and Validation passed. This includes generated .erl/hybrid assertions, incremental/disabled/clean builds and real local-package consumer/CLI. Local part 016 [tests](docs/validation/part-016/tests.json)/[integration](docs/validation/part-016/integration-results.json) remain the earlier local snapshots; no local runtime tests were rerun for this evidence/documentation-only part.

### Changes, scope and validation

Updated PROGRESS, COMPATIBILITY, machine-readable feature evidence, matrix, semantic boundaries and NEXT_STEPS around the **complete current 160-case result**, removing superseded pending claims from current state while retaining historical journal entries. Registry remains **49 MFAs**, all Partially compatible. No runtime/compiler change, new dependency/copied source/license decision, production BEAM dependency or benchmark/performance claim. Official reference checkout remains unchanged.

Remote commands are those in differential.yml: pinned configure/make/make install, dotnet build tools/Erlang.Differential -m:1, dotnet run --project tools/Erlang.Differential --no-build -- "$GITHUB_WORKSPACE/artifacts/otp-oracle/bin/erl" artifacts/differential.json. Windows CI runs pwsh -File tools/validate.ps1. Retrieved original reports/log evidence, checked artifact SHA256, JSON completeness/counts, registered feature test identifiers/report paths and git diff --check before commit.

Reference expressions are evaluated through **erl_eval:exprs**, not a full compiled-reference-module/optimizer test suite. Complete applicable inputs/errors, compiled-reference modules, stacks, asynchronous signals, side effects, full OTP reference suites and BEAM performance comparisons remain unverified. No full module/MFA is marked Verified compatible from the selected corpus.

### Handoff

Commit title: `Record complete float UTF and numeric oracle comparison`. Publish this evidence checkpoint to origin/main. Next executable language unit: construction-only non-UTF literal-string size/type modifiers, including empty-string validation, with patterns restricted according to pinned erl_lint good_string_size_type (default or unsized UTF). Add a separate compiled-reference-module comparison track; continue broader runtime/OTP/distribution/IDE work afterwards. Detailed English entries and immutable reports continue per part. No public NuGet publication.
