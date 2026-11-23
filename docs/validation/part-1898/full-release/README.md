# Full uncached Windows OTP 29.1.1 release comparison

The pinned source is `OTP-29.1.1` at `ad05823719d77c8faee87348ea39513d4e2f99c5`. These results bind to the frozen compiler and all 1,366 release inputs, including the recorded source, transitive include, SDK/runtime and generator identities. Four isolated workers compiled without cached results.

| Equal | Different | Unsupported | SourceRejected | Elapsed seconds |
| ---: | ---: | ---: | ---: | ---: |
| 412 | 954 | 0 | 0 | 1,055.41 |

The preceding accepted commit gains two full matches, `prim_eval.beam` and `systools.beam`, with zero losses. The comparison against Part 1897 restores `diameter_callback.beam` and retains `prim_eval.beam`. All 90 of the selected 9,885 test cases pass. A separate 126-file selection records 15 Equal and 111 Different in 689.49 seconds.

The strong ERTS validator clears the typed record defect on all 20 affected modules. The Windows Megaco fixture reproduces both outcomes with pinned stack lines 87 and 79. The public-key on-load module is loaded independently on Windows, with matching OS certificate count and digest, missing-path behavior and cache clearing. The improved Megaco stack attribution changes its raw Line section while the complete file remains Different. The raw section inventory and normalized function diagnostics remain independent.

[Raw corpus](full-uncached/otp-corpus.json) · [Prior accepted delta](status-vs-last-local-commit.json) · [Pinned input verification](input-verification.json) · [Build profiles](profile-equivalence.json) · [Focused tests](focused.json) · [Native Megaco](native-megaco-stack-provenance/receipt.json) · [Native certificate module](native-public-key-on-load/receipt.json).
