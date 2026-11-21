# Part 1974 Windows OTP compiler validation

The compiler release gate completed locally. Repository-wide managed acceptance remains open.

| Check | Actual result |
| --- | --- |
| Pinned release | OTP-29.1.1 at ad05823719d77c8faee87348ea39513d4e2f99c5 |
| Frozen payload | [Source, compiler and runtime](source-freeze.json), [compiler inputs](compiler-source-closure.json), [full launch](full-launch.json) |
| Focused regression | [535 passed](focused-verified.json), [exact coverage in two subruns](fast-splits/receipt.json) |
| Scoped release | [17 Equal and 85 Different](pilot.json) across 102 images |
| Uncached full release | [509 Equal and 857 Different](full-uncached/otp-corpus.json) in 1268.93 seconds. Zero Unsupported, SourceRejected and CompilerError. |
| Raw changes | [All 509 previous exact images retained](full-uncached/compare-summary.json). 0 new exact images. [Section deltas](full-uncached/section-delta.json) record 0 gains and 0 losses. |
| Normalized instructions | [12 gains and zero losses](rank-counts.json), 88 frames closer and 0 farther |
| Native | [259 release observations and 216 synthetic comparisons](native-early-records/receipt.json), [pinned loader outcomes](loader-rollup.json) |
| Remaining causes | [Corpus ranking](ranked-causes.md) |
| Windows build profiles | [Effective profiles agree](profile-equivalence.json) |
| Readiness | [Regenerated from the immutable managed test report](readiness-receipt.json) |

Ordinary returning calls retain a continuation frame. Proven pre-call scratch can use X when no saved value survives the call. Independent pre-call aliases reuse their source between safepoints. Terminal projections extend the live prefix when lower X roots are initialized. Disjoint terminal type tests preserve an unchanged tuple across an ordinary call.

The linked scope and runtime execution use the frozen compiler and release manifests. The full corpus recompiles every module. Native checks also exercise the real syntax annotation API, tagged record failures and the exact effects of list and case evaluation. The trailing-root witnesses expose selected private functions through temporary copies with only ExpT changed and assert identical Code. The loader rollup uses the unmodified compiler images.


The local [eight-module decoder timing comparison](performance-baseline-1971/receipt.json) uses a separate Part 1971 experiment.

The managed Jaro workloads retain the existing timeout blocker. No current full managed run, local delivery commit or server mutation is recorded.
