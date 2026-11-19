# Compiler timing audit and phase probe

The read-only audit compares saved uncached Windows runs for Parts 1845 and 1853. `audit.py` reads the original reports, shard timings, source archives, saved generated BEAMs and installed OTP references. `audit.json` records both report and source hashes. It performs no compilation. The four-worker elapsed times were 602.52 s and 846.85 s with different concurrent workloads and shard assignments. Only 343 of 1,366 modules kept the same shard. All four heavy parser modules kept theirs.

| Group | Modules | Median 1853 / 1845 module wall time |
| --- | ---: | ---: |
| Byte-identical output | 895 | 1.375 |
| Changed output | 471 | 1.350 |
| Prior duration under 0.2 s | 660 | 1.375 |
| Prior duration 0.2–1 s | 391 | 1.330 |
| Prior duration 1–10 s | 265 | 1.387 |
| Prior duration above 10 s | 50 | 1.382 |

| Parser | Before (s) | After (s) | Raw changed sections |
| --- | ---: | ---: | --- |
| `erl_parse` | 177.51 | 286.06 | None |
| `megaco_text_parser_v1` | 81.28 | 134.62 | `Dbgi` |
| `megaco_text_parser_v2` | 86.32 | 157.60 | `Dbgi` |
| `megaco_text_parser_v3` | 113.87 | 193.11 | `Attr`, `Code`, `Dbgi` |

1,211 modules slowed. The four parsers contribute 312.41 s of the 1,041.39 s summed module wall-time increase. The sum spans overlapping jobs and is distinct from the 244.34 s elapsed difference. `erl_parse` produces an identical 181,512-byte `Code` chunk with 8,541 labels and 914 functions. The v3 `Code` chunk is 24 bytes smaller with 7,632 labels and 1,155 functions. Uniform ratios across size bands and the slowdown of identical images make host contention and shared output-neutral work plausible. The existing timing reports cannot attribute cost to a pass.

## Instrumentation proposal

1. Add a measurement-only switch to the staged compiler/corpus pair. Keep one mutable counter collection on the compile thread for the current module. Record the same output and write one separate sidecar record **after** compilation. Use `Stopwatch.GetTimestamp`, `GC.GetAllocatedBytesForCurrentThread`, process CPU delta, and GC generation counts. Include SDK/runtime, compiler/tool, module source/header/profile hashes and peak process memory. Keep the disabled path free of per-pass allocations and logging.
2. Measure inclusive wall ticks and allocations separately for preprocessing, parser/semantics, abstract-code and documentation construction, high-level rewrites, initial BEAM lowering, and the `EmitCode` cleanup. Inside the cleanup bracket `FoldStackAndArgumentSwaps`, the initial and late stack trims, `RemoveProvenRegisterMoves`, `ShareReturnBlocks`, and final line cleanup. Track instruction reads and raw decoded bytes separately from inclusive phase totals to avoid double-counting.
3. Within `ShareReturnBlocks`, record rounds split between sharing and canonicalization, full-reader time and decoded bytes per round, and pass invocations and successes. Preserve the short-circuit order. The most promising comparison is the no-edit scan work added by 1846–1853 (`KeepAdjacentResultsInX0`, delayed result stores through constructors, `KeepMatchedCallProjectionInX`, the late map-only scan, and broadened leaf-frame analysis). The fixed-point loop reparses the whole module when any one rewrite changes the body. Count successful edits by pass as well as total time. Include time for the output-neutral whole-AST pass `FoldUnusedTupleBindings`, whose newer implementation walks and materializes each clause even if it has no candidate match.
4. Once the current linked validation is finished, use one quiet, jobs=1 witness of `erl_parse`, `megaco_text_parser_v3`, and a small unchanged control such as `logger_handler`. Keep temp and generated paths isolated. Use already frozen binaries for the first paired elapsed/CPU control. Follow with identically instrumented baselines only if a real difference remains or the current phase distribution warrants an improvement. Reverse A/B order for repeat measurements and keep profiler/host workload fixed. Add v1 only if the v3 metadata split needs confirmation. Compare instrumented and uninstrumented output bytes from each compiler and preserve per-module status and normalized-code measurements separately.

Existing decode reuse experiment in `artifacts/local-emit-code-decode/README.md` reported 8.2% fewer thread allocations on its small fixture and overlapping end-to-end timing ranges. Same-segment reuse does not remove fixed-point decodes after the byte segment changes. The present proposal prioritizes measuring rounds and finding repeated whole-body work before choosing an optimization.

No build, corpus rerun, benchmark, profiler, or source edit was performed during this audit. The Python script was executed only against saved artifacts.
