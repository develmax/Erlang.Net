# Part 1970 Windows OTP compiler validation

The compiler release gate completed locally. Repository-wide managed acceptance remains open.

| Check | Actual result |
| --- | --- |
| Pinned release | OTP-29.1.1 at ad05823719d77c8faee87348ea39513d4e2f99c5 |
| Frozen payload | [Source, compiler and runtime](source-freeze.json), [compiler inputs](compiler-source-closure.json), [full launch](full-launch.json) |
| Focused regression | [519 passed](focused-verified.json), [exact coverage in two subruns](fast-splits/receipt.json) |
| Scoped release | [12 Equal and 101 Different](pilot.json) across 113 images |
| Uncached full release | [509 Equal and 857 Different](full-uncached/otp-corpus.json) in 1078.42 seconds. Zero Unsupported, SourceRejected and CompilerError. |
| Raw changes | [All 509 previous exact images retained](full-uncached/compare-summary.json). 0 new exact images. [Section deltas](full-uncached/section-delta.json) record 0 gains and 0 losses. |
| Normalized instructions | [3 gains and zero losses](rank-counts.json), 5 frames closer and 3 farther |
| Native | [219 release observations and 84 synthetic comparisons](native-early-records/receipt.json), [pinned loader outcomes](loader-rollup.json) |
| Remaining causes | [Corpus ranking](ranked-causes.md) |
| Windows build profiles | [Effective profiles agree](profile-equivalence.json) |
| Readiness | [Regenerated from the immutable managed test report](readiness-receipt.json) |

Record guards on extracted list heads run before their successful branch owns an allocated frame. The preserved selector stays in Y through closed payload tests in X. Fresh bound list heads share the existing pure construction plan. Compound cases group adjacent arity-two tagged tuples and atoms by type.

The linked scope and runtime execution use the frozen compiler and release manifests. The full corpus recompiles every module. Native checks also exercise the real syntax annotation API, tagged record failures and the exact effects of list and case evaluation. The trailing-root witnesses expose selected private functions through temporary copies with only ExpT changed and assert identical Code. The loader rollup uses the unmodified compiler images.


The managed Jaro workloads retain the existing timeout blocker. No current full managed run, local delivery commit or server mutation is recorded.
