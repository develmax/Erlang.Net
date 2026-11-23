# Part 2015 PR 143 map input ownership through constructor joins

[Hosted CI at f772b1c5e](https://github.com/develmax/Erlang.Net/actions/runs/45145216365) recorded 7,818 of 7,844 platform cases passing. The [26 raw failure headlines](hosted.txt) match the preceding local cohort. Twelve independent jobs passed.

The existing closed map-join allocator requires two projected values to feed a two-entry map constructor. Map key order can place those two reads in either order. Check that both distinct source registers occur exactly once and retain all existing ownership, branch, frame and error-edge predicates. The rewrite still keeps the formal map in X0 and renames the projected values according to their register identity. The existing regression catches the previously skipped pattern. The arithmetic of the allocator is unchanged.

Pinned OTP 29.1.1 at ad05823719 emits the constructor using the SSA entry registers and map operands in beam_ssa_codegen. Reuse the C# operand reader, register renamer and existing OTP compiler fixture. No new dependency, runtime contract classification or license change. The [generated fixture](map-candidate.txt) matches the whole deterministic original file, SHA-256 68D8D4C12F97C392BBA47053754F4338994F20D7FB61F736647F46637B9D522F. Seven [paired original ERTS calls](native-map.stdout) agree across the three constructor branches, iterator fallback and rejection paths.

| Check | Report | Result |
| --- | --- | --- |
| Single-worker .NET SDK 10.0.400 test project build | [Build](build-final.json) | Passed in 67.2 s |
| Same 44 selected cases | [Results](focused-final.json), [raw output](focused-final.stdout), [time](focused-final-time.json) | 19 passed, 25 failed in 43.1 s. One newly passing case and no lost passes. |
| Three uncached release representatives with map comprehensions | [Baseline](baseline-corpus.json), [candidate](candidate-corpus.json), [delta](release-delta.json), [loads](release-prepare.stdout) | 1 Equal, 2 Different, 0 Unsupported and 0 SourceRejected. Same output bytes before and after. Both Different images prepare in ERTS. |
| Layout and source admission | [Layout](style-final.txt), [13 plans](source-admission-final.json), [hash refresh](refresh-final.json) | Passed |

The [screening run](compare.txt) is a baseline survey of extracted synthetic modules. It has separate scope from the focused registered cases and the release corpus. The [baseline native probe](native-baseline.stdout) also captures an unresolved value-position maps:get/3 behavior gap. A computed lookup of an existing key returns the default value in the baseline C# image when executed by pinned ERTS. The same probe retains evaluation effects and correctly reports the other observed errors. The raw assertion failure is [preserved](native-baseline.stderr). Candidate changes to prepared-map provenance and synthetic argument-root assertions did not meet their focused checks and were discarded.

Continue with the lifetime of the computed key across the frame trim and the remaining frame ownership cases. The full CI, uncached 1,366-file gate and readiness regeneration remain open.
