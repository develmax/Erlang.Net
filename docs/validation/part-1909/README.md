# Part 1909 full Windows OTP 29.1.1 comparison

| Equal | Different | Unsupported | SourceRejected | Seconds |
| ---: | ---: | ---: | ---: | ---: |
| 426 | 940 | 0 | 0 | 831.074 |

Four isolated workers compiled all 1,366 inputs without compilation-result caching. All 424 prior complete matches remain. The new matches are `ssh_io` and `xmerl_xs`. Pinned ERTS prepares 334 ordinary changed images. The zstd on-load image is verified by direct isolated load and a compression round trip. Each new raw match shares the hash of the independently compiled and executed native fixture. All 1,366 effective input profiles agree.

The normalized comparison measures 64 gains and 0 losses across 760 checked functions. Frame-size distance decreased for 20 and increased for 0 functions. The independent raw section comparison reports 12 gains and 0 losses.

The 205-module short comparison produced 35 Equal and 170 Different in 664.303 seconds. All 128 focused fast cases and the exclusive whole-module case passed. The first two full invocations ended without a valid shard report after session interruptions. The retained files include both original invocations and a temporary-directory collision during recovery. The completed run used the same verified compiler and source in a fresh isolated temporary directory and survived the task runner session.

Readiness is regenerated from the last complete passing runtime report (Part 1875), with Part 1839 as the explicit comparison. No runtime MFA is promoted by these compiler results. This comparison covers raw equality, native preparation, selected behavior and compiler tests.

[Raw result](full-uncached/otp-corpus.json) · [Native proof](native-early-records/receipt.json) · [Focused test proof](focused-verified.json) · [Interrupted lane](interrupted/interrupt-receipt.json).
