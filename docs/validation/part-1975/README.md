# Part 1975 Windows OTP compiler validation

The compiler release gate completed locally. Repository-wide managed acceptance remains open.

| Check | Actual result |
| --- | --- |
| Pinned release | OTP-29.1.1 at ad05823719d77c8faee87348ea39513d4e2f99c5 |
| Frozen payload | [Source, compiler and runtime](source-freeze.json), [compiler inputs](compiler-source-closure.json), [full launch](full-launch.json) |
| Focused regression | [538 passed](focused-verified.json), [exact coverage in two subruns](fast-splits/receipt.json) |
| Scoped release | [10 Equal and 77 Different](pilot.json) across 87 images |
| Uncached full release | [510 Equal and 856 Different](full-uncached/otp-corpus.json) in 1348.18 seconds. Zero Unsupported, SourceRejected and CompilerError. |
| Raw changes | [All 509 previous exact images retained](full-uncached/compare-summary.json). 1 new exact images. [Section deltas](full-uncached/section-delta.json) record 1 gains and 0 losses. |
| Normalized instructions | [9 gains and zero losses](rank-counts.json), 74 frames closer and 2 farther |
| Native | [269 release observations and 225 synthetic comparisons](native-early-records/receipt.json), [pinned loader outcomes](loader-rollup.json) |
| Remaining causes | [Corpus ranking](ranked-causes.md) |
| Windows build profiles | [Effective profiles agree](profile-equivalence.json) |
| Readiness | [Regenerated from the immutable managed test report](readiness-receipt.json) |

An ordinary call can keep an empty frame when entry checks precede the frame and no Y value survives that call. A pre-call alias can reuse a dense, proved X root prefix across one collector. A stable call result stays in X0 through its last dependent read. Checked terminal tuple branches share projections and preserve the record and payload through the heap reservation.

The linked scope and runtime execution use the frozen compiler and release manifests. The full corpus recompiles every module. Native checks also exercise the real syntax annotation API, tagged record failures and the exact effects of list and case evaluation. The trailing-root witnesses expose selected private functions through temporary copies with only ExpT changed and assert identical Code. The loader rollup uses the unmodified compiler images.


The local [eight-module decoder timing comparison](performance-baseline-1971/receipt.json) uses a separate Part 1971 experiment.

The managed Jaro workloads retain the existing timeout blocker. No current full managed run, local delivery commit or server mutation is recorded.
