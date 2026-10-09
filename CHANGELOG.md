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
| Permanent regression tests | dotnet run --project tests/Erlang.Tests --no-build -- artifacts/tests.json; [report](docs/validation/tests.json) | 121 passed, 0 failed; includes 42 direct MFA tests |
| MSBuild/package/tool integration | pwsh -File tools/validate.ps1; [report](docs/validation/integration-results.json) | Build, Tests, Incremental, CleanRebuild, PackageConsumer, LocalTool — Passed; Hello World; DisabledPreprocessing — Expected failure |
| Differential against real OTP | [runner](tools/Erlang.Differential) | Not run: local oracle unavailable |
| Remote CI | [.github/workflows](.github/workflows) | Prepared; execution unconfirmed |
| Whitespace and reference checkout | git diff --check; git -C ../otp status --porcelain | No diff errors; reference checkout clean |

Exploratory benchmark: Release, .NET 10.0.11, Windows 10 x64, 16 logical processors; one warmup and one measured run. [Results](docs/validation/benchmarks.json), [method](docs/benchmarks.md):

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
