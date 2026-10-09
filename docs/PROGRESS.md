# Progress checkpoint — 2026-10-09

Detailed history: [CHANGELOG, parts 001–007](../CHANGELOG.md). This file records the current state; every subsequent work part must also receive its own English changelog entry and a local commit when completed and validated.

**The full work.md assignment is not complete.** This checkpoint delivers a working, independently executable C#/.NET foundation and an early compiler/runtime/build vertical slice. Implementation work is in this repository; the sibling official OTP checkout has no local changes.

Reference: OTP-29.1.1 (`ad05823719d77c8faee87348ea39513d4e2f99c5`). SDK: 10.0.400; C# 14; net10.0. No BEAM or installed Erlang is needed for the implemented production build/execution path.

## Implemented work units

| Unit | Deliverable | Current status |
|---|---|---|
| Discovery/bootstrap | 11-project solution, pinned baseline/SDK, generated source inventory and durable documentation | Partially compatible |
| Term foundation | Atoms, arbitrary integers, floats/signed zero, PID/port/ref/fun values, tuples, cons/improper lists, maps, binary/bitstring values; explicit equality/order/hash | Partially compatible |
| Runtime | Local process contexts/dictionaries/registry, ordered selective receive, timeouts/cancellation, links/monitors/exit signals, atomic spawn_monitor, iterative exit cascades | Partially compatible |
| Compiler | Lexer/Pratt parser, AST, scope/guard analysis, function clauses/private/exported resolution, closure capture, case/receive, map construction/updates/patterns, integer/binary bitstring construction, generated C# AST construction and evaluator, module-local tail-call trampoline | Partially compatible |
| Core modules | 49 registered MFAs across erlang/lists/maps/io, each with a direct permanent contract case | Partially compatible |
| OTP foundation | C# gen_server callbacks for call/cast/info/stop; supervisor strategies/policies/intensity/ordered shutdown | Partially compatible |
| Serialization | Common ETF tags, bounded malformed input handling, compressed input, identity/bitstring/map/list roundtrips | Partially compatible |
| Hybrid/build | Native receive/case/fun blocks in ordinary async C#, Roslyn lexical context, #line/line preservation, nullable propagation, automatic preprocessing, incremental and clean builds | Partially compatible |
| Packaging | Local Erlang.Net.CSharp/Erlang.Net.Tool nupkgs; real PackageReference consumer and installed local CLI | Implemented |
| Verification/performance | Dependency-free regression harness, differential runner, CI definitions and exploratory benchmark harness | Implemented, remote oracle build in progress |

Source inventory contains **1,289 source modules, 38,622 explicit source exports, 524 BIF declarations**. These counts are not effective expanded API coverage; conditional/macros/generated/NIF/platform contracts remain incomplete. `supported-mfas.json` is the implemented-MFA overlay, with explicit test identifiers.

## Executed validation

- **206/206 local regression tests passed**, including one direct case for each of the 49 registered MFAs.
- Full solution build passed with zero warnings/errors. Ordinary `dotnet build` was verified outside sandbox worker-process restrictions; `-m:1` works inside the sandbox.
- HelloHybrid built through MSBuild and printed exactly **Hello World**. It checked map_source .erl construction/update/function patterns and a hybrid map case, plus `.erl` double/1, signed floating zero emission and signed-zero clause matching, plus ordinary C# records/generics/nullable/raw/interpolated strings/switch/async semantics.
- Disabling preprocessing caused the expected source syntax errors even after an earlier successful build. The preprocessor runs before the compile dependency cache, preventing a stale successful build from masking disabled preprocessing.
- Unchanged inputs preserved generated-file timestamps; clean/rebuild restored the example successfully.
- A workspace-local NuGet PackageReference consumer built and produced exactly Hello World. Content-hash-specific restore caches prevent reusing stale same-version package binaries. The local CLI installed under artifacts/local-tool and compiled the .erl example.
- 50,000 module-local tail calls completed without growing CLR stack. A 10,000-process link-failure chain propagated iteratively. Atomic spawn-monitor was tested against immediately failing children 200 times.
- Supervisor tests exercised all three strategies, transient/temporary policies and restart intensity. gen_server tests exercised state updates, stop, crashes, timeout and init errors.
- Release exploratory benchmarks completed; see benchmarks.md and validation/benchmarks.json. No BEAM comparison, latency-percentile or universal scalability claim.

Executable checkpoint reports are in `docs/validation`. Full transient logs, packages, consumers and generated sources are under ignored `artifacts`, bin and obj. Checkpoint 3860695 was committed and pushed to origin/main following user authorization. Part 004 is recorded in the changelog and committed after validation. No public NuGet package publication occurred. Remote Windows CI for checkpoints 3860695, f428c9f, b15da2f and dc91e8c was confirmed successful through GitHub Actions; new-head CI must be checked after publication. Historical reports are preserved under docs/validation/part-001; part-004 reports preserve the map checkpoint, part-005 reports preserve the four new map guard BIFs and part-006 reports preserve bit construction plus the failed oracle build diagnosis.

## Open compatibility gaps and regressions

No known failing local regression at this checkpoint. Known unsupported or partial semantics are listed in semantic-differences.md; absence of a failing test does not establish their compatibility.

The language grammar, scheduling/signal guarantees, standard modules, OTP callback/service contracts, distribution, advanced runtime, IDE/debugger integration and full reference suites remain incomplete. Local OTP 29.1.1 oracle execution is blocked by a missing executable. The first manual differential run attempted the pinned source build but failed because debugger required disabled wx. The headless workflow now also excludes debugger/observer/et; comparison results remain unavailable until a successful rerun.

Map expression/pattern/update syntax is implemented for the current subset. Map guards is_map/map_size/map_get/is_map_key are now registered and tested. Next: rerun the repaired exact-version oracle workflow, then add bitstring patterns and remaining segment types. NEXT_STEPS contains precise continuation tasks and commands. Do not report this repository as a complete Erlang/OTP reimplementation.

## Overall completion estimate

Approximately **5% of the full assignment**, a coarse engineering estimate as of 2026-10-09, not measured semantic compatibility or a test-pass percentage. The vertical slice is executable and map syntax has been added, but most OTP exports, distribution, advanced services, full language/IDE support and reference-suite hardening remain unfinished. No exact weighted contract denominator is available. The 49 partially compatible MFAs and 38,622 source exports describe scope; they must not be treated as a complete or directly comparable API coverage denominator.

Part 007 adds is_bitstring/1, bit_size/1 and byte_size/1, including rounded-up byte counts for non-octet bitstrings and guard/pattern-key use. Reports are preserved under docs/validation/part-007. The repaired oracle run can be resumed at https://github.com/develmax/Erlang.Net/actions/runs/37938184184; record its exact head and actual result before compatibility promotion.
