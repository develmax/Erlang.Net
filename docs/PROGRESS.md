# Progress checkpoint — 2026-10-09

Detailed history: [CHANGELOG, parts 001–012](../CHANGELOG.md). This file records the current state; every subsequent work part must also receive its own English changelog entry and a local commit when completed and validated.

**The full work.md assignment is not complete.** This checkpoint delivers a working, independently executable C#/.NET foundation and an early compiler/runtime/build vertical slice. Implementation work is in this repository; the sibling official OTP checkout has no local changes.

Reference: OTP-29.1.1 (`ad05823719d77c8faee87348ea39513d4e2f99c5`). SDK: 10.0.400; C# 14; net10.0. No BEAM or installed Erlang is needed for the implemented production build/execution path.

## Implemented work units

| Unit | Deliverable | Current status |
|---|---|---|
| Discovery/bootstrap | 11-project solution, pinned baseline/SDK, generated source inventory and durable documentation | Partially compatible |
| Term foundation | Atoms, arbitrary integers, floats/signed zero, PID/port/ref/fun values, tuples, cons/improper lists, maps, binary/bitstring values; explicit equality/order/hash | Partially compatible |
| Runtime | Local process contexts/dictionaries/registry, ordered selective receive, timeouts/cancellation, links/monitors/exit signals, atomic spawn_monitor, iterative exit cascades | Partially compatible |
| Compiler | Lexer/Pratt parser, AST, scope/guard analysis, function clauses/private/exported resolution, closure capture, case/receive, map construction/updates/patterns, integer/binary bitstring construction and patterns, generated C# AST construction and evaluator, module-local tail-call trampoline | Partially compatible |
| Core modules | 49 registered MFAs across erlang/lists/maps/io, each with a direct permanent contract case | Partially compatible |
| OTP foundation | C# gen_server callbacks for call/cast/info/stop; supervisor strategies/policies/intensity/ordered shutdown | Partially compatible |
| Serialization | Common ETF tags, bounded malformed input handling, compressed input, identity/bitstring/map/list roundtrips | Partially compatible |
| Hybrid/build | Native receive/case/fun blocks in ordinary async C#, Roslyn lexical context, #line/line preservation, nullable propagation, automatic preprocessing, incremental and clean builds | Partially compatible |
| Packaging | Local Erlang.Net.CSharp/Erlang.Net.Tool nupkgs; real PackageReference consumer and installed local CLI | Implemented |
| Verification/performance | Dependency-free regression harness, differential runner, CI definitions and exploratory benchmark harness | Implemented, complete 93-case exact-version corpus matched |

Source inventory contains **1,289 source modules, 38,622 explicit source exports, 524 BIF declarations**. These counts are not effective expanded API coverage; conditional/macros/generated/NIF/platform contracts remain incomplete. `supported-mfas.json` is the implemented-MFA overlay, with explicit test identifiers.

## Executed validation

- **244/244 local regression tests passed**, including one direct case for each of the 49 registered MFAs and two source-transport regressions.
- Validation checks an unavailable oracle returns code 2 and writes an incomplete infrastructure-error report with zero executed cases.
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

The language grammar, scheduling/signal guarantees, standard modules, OTP callback/service contracts, distribution, advanced runtime, IDE/debugger integration and full reference suites remain incomplete. Local OTP 29.1.1 oracle execution is blocked by a missing executable. The first remote build failed on a wx-dependent application; the repaired headless build succeeded. Its comparison stopped after 16 passes on an invalid U+FFFF source literal; part 009 corrects the earlier transport diagnosis and corpus. Run 37943457369 at c139b45 now matches all 93 cases, including map guards, bit construction/patterns, size BIFs and fun capture. These tested cases do not establish full module compatibility.

Map expression/pattern/update syntax is implemented for the current subset. Map guards is_map/map_size/map_get/is_map_key are now registered and tested. Next: add float/UTF segments and string modifiers, extend scope/error cases, and compare each new corpus against the exact reference. NEXT_STEPS contains precise continuation tasks and commands. Do not report this repository as a complete Erlang/OTP reimplementation.

## Overall completion estimate

Approximately **5% of the full assignment**, a coarse engineering estimate as of 2026-10-09, not measured semantic compatibility or a test-pass percentage. The vertical slice is executable and map syntax has been added, but most OTP exports, distribution, advanced services, full language/IDE support and reference-suite hardening remain unfinished. No exact weighted contract denominator is available. The 49 partially compatible MFAs and 38,622 source exports describe scope; they must not be treated as a complete or directly comparable API coverage denominator.

Part 007 adds is_bitstring/1, bit_size/1 and byte_size/1, including rounded-up byte counts for non-octet bitstrings and guard/pattern-key use. Part 008 repairs UTF-8 source transport and incomplete differential reporting. Reports are preserved under docs/validation/part-007 and part-008. Windows CI for 5a1e5cc and 4a8c379 was confirmed successful; the new-head CI and full differential run must be inspected after publication.






Latest oracle: 93/93 value/error comparisons passed in run 37943457369 at c139b45; original report and verified artifact metadata are preserved under docs/validation/part-012. The same head passed Windows CI run 37943449681, including 244 local tests and full package integration. No new local execution was necessary for this evidence-only checkpoint. Full stack/signal/side-effect/reference-suite scope remains unverified. Historical failed runs and diagnoses are in CHANGELOG, parts 006-011. Overall estimate remains approximately 5%.
