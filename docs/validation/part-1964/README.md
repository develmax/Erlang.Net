# Part 1964 Windows OTP compiler validation

The compiler release gate completed locally. Repository-wide managed acceptance remains open.

| Check | Actual result |
| --- | --- |
| Pinned release | OTP-29.1.1 at ad05823719d77c8faee87348ea39513d4e2f99c5 |
| Frozen payload | [Source, compiler and runtime](source-freeze.json), [compiler inputs](compiler-source-closure.json), [full launch](full-launch.json) |
| Focused regression | [492 passed](focused-verified.json), [exact coverage in two subruns](fast-splits/receipt.json) |
| Scoped release | [23 Equal and 63 Different](pilot.json) across 86 images |
| Uncached full release | [508 Equal and 858 Different](full-uncached/otp-corpus.json) in 1040.88 seconds. Zero Unsupported, SourceRejected and CompilerError. |
| Raw changes | [All 508 previous exact images retained](full-uncached/compare-summary.json). 0 new exact images. [Section deltas](full-uncached/section-delta.json) record 1 gains and 0 losses. |
| Normalized instructions | [13 gains and zero losses](rank-counts.json), 18 frames closer and 0 farther |
| Native | [142 release observations and 13 synthetic comparisons](native-early-records/receipt.json), [pinned loader outcomes](loader-rollup.json) |
| Remaining causes | [Corpus ranking](ranked-causes.md) |
| Windows build profiles | [Effective profiles agree](profile-equivalence.json) |
| Readiness | [Regenerated from the immutable managed test report](readiness-receipt.json) |

List arguments retain their live reference in an X register while emitting pure tuple and cons constructors. A mixed atom and tuple decision tests the tuple domain first when the checked result can be forwarded. Call-result continuations reserve only persistent inputs in Y and move the checked record failure to the cold exit region. The call planner forwards the result of a dictionary read to the next call after constructing pure outgoing arguments.

The linked scope and runtime execution use the frozen compiler and release manifests. The full corpus recompiles every module. The native checks preserve callback values and failures, Common Test style assets, and the ordering of protected effects.


The managed Jaro workloads retain the existing timeout blocker. No current full managed run, local delivery commit or server mutation is recorded.
