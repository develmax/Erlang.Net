# Part 2012 PR 143 shared literal map case projections

The [previous exact retake](../part-2011-pr143-record-list-tail/focused.json) had 33 failures. In pinned OTP 29.1.1, beam_core_to_ssa:partition_intersection/3 splits the common map keys from the residual keys in ordered clauses. Reuse the C# typed lookup writer and case-local slot allocation to apply the same intersection for admitted literal keys. Emit the residual keys only in the owning clause. Preserve the enclosing failure target, later branch retries, binding order, dynamic-key fallback and disabled optimization path.

The map fixture checks one read per common key, retry with a missing earlier key, branch priority when both branches match and the original case-clause payload when the common key is missing. The generic map type fixture counts all destination pairs and checks distinct destinations within a consolidated instruction. Both optimized and disabled cases still check operand type tags. The mixed list-filter fixture now expects its generated helper, which exists in both compilers. The two sibling comprehensions still differ in their additional helper layouts.

The [paired map disassembly](matrix.stdout) shows the shared alpha lookup and individual beta and gamma lookups in both optimized compilers. All 26 default-profile [paired ERTS map observations](native.stdout) agree on returned values or error class and reason. The disabled-profile uniform result in that file is retained from previously built managed images and rechecked with original ERTS. [Seven grouped-list calls](grouped-native.stdout) agree in ERTS. A separate original-map register retention test continues to fail.

| Check | Report | Result |
| --- | --- | --- |
| Single-worker build with .NET SDK 10.0.400 | [Build](build-final.json) | Passed in 32.3 s |
| Same 44-case selection with nine controls | [Results](focused-final.json), [raw output](focused-final.stdout), [time](focused-time.json) | 14 passed, 30 failed in 53.7 s. Three recovered and no new observed failures relative to Part 2011. |
| Five selected uncached Windows release images | [Baseline](baseline-corpus.json), [candidate](candidate-corpus.json), [generated hashes](subset-generated-hashes.json) | 0 Equal, 5 Different, 0 Unsupported, 0 SourceRejected on both compilers. Two generated files changed. |
| Per-function rank and native release preparation | [Baseline rank](baseline-rank.stdout), [candidate rank](candidate-rank.stdout), [functions](candidate-functions.terms), [ERTS](release-prepare.stdout) | All five accepted by the loader. No change to first-difference terms in the selected images. |
| Source layout and passive admission | [Layout](style-final.txt), [13 plans](source-admission.json), [hash refresh](refresh.json) | Passed |

Reuse pinned Apache-2.0 OTP for development evidence and the existing C# writer, term comparison and frame slot ownership. No new dependency, public runtime contract or registry change. The selected release images were chosen by a source scan for adjacent literal map patterns with partially overlapping keys. Release image preparation did not execute their exported functions. The 30 remaining failures, full CI, full uncached release corpus and readiness remain open.
