# Part 1874 complete uncached Windows corpus

The exact frozen source and tool from [the targeted report](../README.md)
produce **410 Equal, 956 Different, 0 Unsupported and 0 SourceRejected**
across 1,366 installed Windows OTP 29.1.1 images. The fixed four shards
complete in 661.74 seconds with no cached compilation. The only gained
whole file is inet6_sctp.beam. No complete file match is lost.

Against Part 1870, 161 emitted files change and all 161 prepare in pinned
ERTS. Fifty-eight Dbgi sections gain equality and inet6_sctp gains Code
and Attr. No raw section match is lost. The normalized comparison has
19 existing body gains, no losses, 57 nearer frame distances and none
farther. The separate strict-helper census accounts for added keys.

Against Part 1865, 423 emitted files change. Pinned ERTS prepares 422
and normally loads the on-load crypto image. Direct and incremental
SHA-256 agree across reference and both compilers. Twenty-seven
existing bodies gain normalized equality and one loses it in this
comparison. The exact diameter_make closure retains a redundant
scratch copy and remains the one normalized loss versus that compiler.
The old 160 farther frame distances remain a measured separate group.

The 1,366 effective original source build descriptions are equivalent
and retained source/header hashes pass. The distinct map inventory
records all 319 resolved logical include profiles. The reference
checkout remains clean. The 956 residual files still differ in Code,
and 124 differ in Dbgi. Whole managed acceptance and readiness are
pending on the linked local package.

[Raw complete classifications](otp-corpus.json) · [Timing](timing.json)
· [Delta against Part 1870](incremental-1870/compare-summary.json)
· [Raw section deltas](incremental-1870/pilot-section-delta.json)
· [Input hashes](input-verification.json) · [ERTS](loader-summary.terms)
· [Normalized comparisons](incremental-1870/pilot-summary.terms).

[Instruction and allocation rank](rank.stdout.log) · [Per-module and function groups](function-rank.terms).
