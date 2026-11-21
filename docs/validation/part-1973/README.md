# Part 1973 Windows OTP compiler validation

The compiler release gate completed locally. Repository-wide managed acceptance remains open.

| Check | Actual result |
| --- | --- |
| Pinned release | OTP-29.1.1 at ad05823719d77c8faee87348ea39513d4e2f99c5 |
| Frozen payload | [Source, compiler and runtime](source-freeze.json), [compiler inputs](compiler-source-closure.json), [full launch](full-launch.json) |
| Focused regression | [533 passed](focused-verified.json), [exact coverage in two subruns](fast-splits/receipt.json) |
| Scoped release | [7 Equal and 73 Different](pilot.json) across 80 images |
| Uncached full release | [509 Equal and 857 Different](full-uncached/otp-corpus.json) in 1039.32 seconds. Zero Unsupported, SourceRejected and CompilerError. |
| Raw changes | [All 509 previous exact images retained](full-uncached/compare-summary.json). 0 new exact images. [Section deltas](full-uncached/section-delta.json) record 0 gains and 0 losses. |
| Normalized instructions | [1 gains and zero losses](rank-counts.json), 0 frames closer and 0 farther |
| Native | [259 release observations and 207 synthetic comparisons](native-early-records/receipt.json), [pinned loader outcomes](loader-rollup.json) |
| Remaining causes | [Corpus ranking](ranked-causes.md) |
| Windows build profiles | [Effective profiles agree](profile-equivalence.json) |
| Readiness | [Regenerated from the immutable managed test report](readiness-receipt.json) |

Values move onto the successor frame along case branches. The checked entry root order follows definitions, and a nonclobbering projection precedes a saved return copy. Terminal saved patterns preserve direct Y operands and the original matched tuple. Late analyses may reuse a decoded immutable view within compilation.

The linked scope and runtime execution use the frozen compiler and release manifests. The full corpus recompiles every module. Native checks also exercise the real syntax annotation API, tagged record failures and the exact effects of list and case evaluation. The trailing-root witnesses expose selected private functions through temporary copies with only ExpT changed and assert identical Code. The loader rollup uses the unmodified compiler images.


The local [eight-module decoder timing comparison](performance-baseline-1971/receipt.json) uses a separate Part 1971 experiment.

The managed Jaro workloads retain the existing timeout blocker. No current full managed run, local delivery commit or server mutation is recorded.
