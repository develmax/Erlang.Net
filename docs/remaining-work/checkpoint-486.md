# Remaining work after Part485

This current checkpoint supersedes old milestone wording in the Part450–454 planning notes. Baseline: OTP-29.1.1, commit `ad05823719d77c8faee87348ea39513d4e2f99c5`. Part485 merged into main as `eb2d3d74352436f4d02d0cd41ad795a66093f70d`; its publication CI40192228033 passed8896/8896 and package gates. These are historical acceptance results, not a new Part486 test run.

## Completed scope

- 34 original OTP application libraries exist as ordinary src projects. All 1321 primary images prepare under the explicit reviewed native composition; bare preparation remains 1317/1321. This does not establish every export or application lifecycle contract.
- Part485 closes both observed binary regex-capture backing differences:176/176 complete comparisons. Its configured native catalog, portable9/9 groups, resource cleanup and empty-PATH cold consumer pass. The admitted original PCRE2/native provider remains an explicit dependency.
- Original net_kernel authenticated connection, data exchange, real peer restart and reconnect have scoped evidence from455–459. Private native child-port creation is already implemented; broad distribution compatibility remains open.
- Original-library consumers and explicit managed/original backend selection exist. Do not rewrite pure original OTP libraries in C# to increase implementation counts.

## Prioritized unfinished acceptance

1. **Current API differences:** reproduce and close31 ledger differences on current backends. They concern physical heap/stack/GC information, spawn/start tuning options, anonymous function restoration in particular proc_lib paths, NIF loading boundaries and per-runtime connection limits. The ledger is current; these historical difference descriptions are not newly reproduced by this layout part.
2. **Complete API contracts:** finish559 Needs verification entries using original OTP values, errors, complete stacks, malformed inputs, concurrent/resource/lifecycle branches and applicable source/BEAM/C# and OS profiles. Include relevant original-library APIs outside the default708-entry registry. There is no defensible fixed test count that proves all these contracts.
3. **Original applications and boot:** expand full kernel/application/code_server/net_kernel boot, shutdown, restart, configuration, supervision, logging and upgrade workflows. Retain the already accepted finite boot and peer profiles; they are not full application certification.
4. **Native production distribution:** package native dependencies independently of the development admitted OTP installation. Complete NIF/driver ABI, resource/dirty scheduling/native threads/TLS, unload/purge/old images, DDLL load/reload/demonitor/force schedules, ports/FD/TTY/signals and supported OS/architecture matrices. Existing ordinary NIF loading and child ports remain implemented.
5. **ETS and runtime resources:** broader atomic concurrency/cursor/mutation/ownership interleavings, physical memory accounting and promised fine-lock/decentralized backends; native heap/GC/hibernate/max-heap tuning and resource-limit semantics. Prior codec cancellation, cleanup and ownership-transfer cases remain closed within scope.
6. **Distribution:** broader topology, signal ordering, pressure, discovery/setup/timeouts, generation replacement, remote controllers, reconnect races and supported capability/TLS/platform combinations beyond the accepted finite original-VM profiles.
7. **Compiler and language:** complete source-derived language, diagnostics/lint/warnings/spans, optimizer/debug mapping, BEAM operand/opcode state spaces, generated modules, preprocessing/parse transforms, reproducible source builds and cross-image code lifecycle. Earlier receive-in-try, trim active tags, remote-await, trampoline and identity witnesses are retained; do not relabel those finite milestones absent.
8. **Scheduler architecture:** one engine with selectable CompatibleScheduler, OptimizedScheduler and DeterministicScheduler; shared correctness and separate fair benchmarks. Current scheduling support does not establish all three strategies.
9. **Library acceptance:** execute upstream application/library suites across the34 prepared libraries and relevant original-module dependency closures. Image preparation100% is completed; API/application compatibility100% is not.
10. **.NET tooling and release:** complete reproducible fresh checkout/incremental/cold consumer/IDE-debug/release matrices and explicit supported-backend documentation. The src/tools layout is a separate mechanical checkpoint; further assembly splits must be justified by dependencies, not empty project names. Public NuGet delivery and owner license selection remain separate decisions; the local license draft stays uncommitted.
11. **Performance:** representative throughput, latency, allocation, GC, memory and scaling comparisons against pinned OTP under equal inputs and correctness gates. No whole-project faster-than-BEAM claim is established.
12. **Validation breadth:** whole upstream suites, per-OS/backend support matrix, current source/DLL binding checks, dependency-based regression blocks and exact-head CI. Scoped success must remain distinct from full-contract promotion.

## Default registry by module

These are registered C# adapter/native contracts. Loaded original modules can expose additional APIs, so zero or partial registration coverage is not proof that the original implementation is missing.

| Module | Registered | Verified full contracts | Needs verification | Known differences |
| --- | ---: | ---: | ---: | ---: |
| erlang |351|21|317|13|
| lists |91|0|91|0|
| maps |34|0|34|0|
| records |8|0|8|0|
| io |1|0|1|0|
| gen_server |10|0|10|0|
| supervisor |14|0|14|0|
| logger |26|0|26|0|
| application |4|0|4|0|
| error_logger |2|0|2|0|
| proc_lib |39|0|22|17|
| sys |2|0|2|0|
| erpc |3|0|3|0|
| rpc |2|0|2|0|
| proplists |22|22|0|0|
| queue |38|38|0|0|
| binary |31|31|0|0|
| ordsets |21|6|15|0|
| erts_internal |7|0|6|1|
| math |1|0|1|0|
| string |1|0|1|0|
| **Total** |**708**|**118**|**559**|**31**|

The complete590 unclosed per-command rows remain in [volumes1–4](contracts-001.md) and [the machine inventory](complete-inventory.json); the authoritative current classifications are in [mfa-compatibility.json](../mfa-compatibility.json). Broad historical component notes require revalidation before being treated as newly confirmed defects. An overall project percentage would conflate preparation, implementation and full semantic acceptance and is therefore not assigned.
