# Part 1983 — Owned projections, frame lifetimes and generated helper ordering

| Evidence | Result |
| --- | --- |
| Frozen Windows OTP 29.1.1 release | 512 Equal, 854 Different, 0 Unsupported, 0 SourceRejected from 1,366 files in 667.46 seconds with fresh compilation. [Corpus report](full-uncached/otp-corpus.json) |
| New byte-exact matches | None |
| Lost byte-exact matches | None |
| Selected compiler cases | 593 passed. [Case identities](focused-verified.json) |
| Scoped release | 34 Equal and 301 Different in 666.84 seconds. [Selection](pilot-selection.json) |
| Normalized diagnostic bodies | 55 gains and 0 losses. [Counts](rank-counts.json) |
| Original OTP execution | 66 selections in 8.77 seconds. 282 changed BEAM images prepared and 1 paired shared on-load limitations. [Behavior](native-scoped/receipt.json) |
| Physical frame widths | The two scoped ct_rpc frame vectors equal the pinned vectors. Earlier allocation regressions remain separately measured. [Scoped vectors](pilot-ct-rpc-widths/widths.tsv) |
| Frozen compiler gate | Accepted. [Evidence](compiler-acceptance.json) |
| Linked repository gate | Initial repository attempt had two lifetime test failures. Both corrected checks and the explicit actor self-disposal control passed locally. [Focused retake](runtime-lifecycle-fixtures/results.json) |

The compiled source supports checked heap projections and retry sharing, cons and tuple forwarding, caller root preservation, handler-free register reuse, case filter sharing and generated helper ordering. Sources, tests and recorded raw errors are bound to the same compiler archive. Pinned files serve only for release comparisons and oracle execution.
