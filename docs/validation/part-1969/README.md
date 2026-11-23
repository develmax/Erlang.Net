# Part 1969 Windows OTP compiler validation

The compiler release gate completed locally. Repository-wide managed acceptance remains open.

| Check | Actual result |
| --- | --- |
| Pinned release | OTP-29.1.1 at ad05823719d77c8faee87348ea39513d4e2f99c5 |
| Frozen payload | [Source, compiler and runtime](source-freeze.json), [compiler inputs](compiler-source-closure.json), [full launch](full-launch.json) |
| Focused regression | [517 passed](focused-verified.json), [exact coverage in two subruns](fast-splits/receipt.json) |
| Scoped release | [7 Equal and 51 Different](pilot.json) across 58 images |
| Uncached full release | [509 Equal and 857 Different](full-uncached/otp-corpus.json) in 1211.45 seconds. Zero Unsupported, SourceRejected and CompilerError. |
| Raw changes | [All 509 previous exact images retained](full-uncached/compare-summary.json). 0 new exact images. [Section deltas](full-uncached/section-delta.json) record 0 gains and 0 losses. |
| Normalized instructions | [9 gains and zero losses](rank-counts.json), 277 frames closer and 12 farther |
| Native | [217 release observations and 51 synthetic comparisons](native-early-records/receipt.json), [pinned loader outcomes](loader-rollup.json) |
| Remaining causes | [Corpus ranking](ranked-causes.md) |
| Windows build profiles | [Effective profiles agree](profile-equivalence.json) |
| Readiness | [Regenerated from the immutable managed test report](readiness-receipt.json) |

Frame ownership and exact live aliases remove eligible interior duplicate slots without moving a value across branches. Case and function entry patterns retain tuple carriers for source-exact reconstruction. The Core-matching traversal keeps list-generator identities under ordinary append, structured equality and a later discriminator following a definition-free arm.

The linked scope and runtime execution use the frozen compiler and release manifests. The full corpus recompiles every module. Native checks include generated XML encoding variants, error paths and ordering of list-generator effects. The trailing-root witnesses expose selected private functions through temporary copies with only ExpT changed and assert identical Code. The loader rollup uses the unmodified compiler images.


The managed Jaro workloads retain the existing timeout blocker. No current full managed run, local delivery commit or server mutation is recorded.
