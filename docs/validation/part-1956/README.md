# Part 1956 Windows OTP compiler validation

**Status** Blocked by two normalized instruction losses first emitted by the Part 1955 private-argument propagation pass. The raw report is provisional. Part 1953 remains the accepted compiler checkpoint.

| Check | Actual result |
| --- | --- |
| Windows installer and pinned source | OTP-29.1.1, ad05823719d77c8faee87348ea39513d4e2f99c5 |
| Compiler, runtime and full source closure | [Frozen identities](source-freeze.json), [907 inputs](compiler-source-closure.json), [build and partition parameters](full-launch.json) |
| Selected compiler regression | [458 passed](focused-verified.json) |
| Scoped corpus | [62 Equal, 41 Different](pilot.json) across 103 images, zero rejection or compilation errors |
| Uncached full corpus | [506 Equal, 860 Different](full-uncached/otp-corpus.json), zero Unsupported, SourceRejected and CompilerError, [elapsed 809.67 seconds](full-uncached/timing.json) |
| Byte-exact retention | [Three gains and zero losses](full-uncached/compare-summary.json) since Part 1953. Gains are diameter_gen_relay, PKIX-X400Address-2009 and otp_internal. |
| Raw sections | [34 gains, zero losses](full-uncached/section-delta.json) |
| Normalized instructions | [111 gains, two losses, 31 frames closer](full-uncached/normalized-summary.terms) among 673 changed comparisons. All 29 provisional Part 1954 gains remain. |
| Loader outcomes | [394 prepared and two identical on_load restrictions](normalized-losses/loader-rollup.terms) across 396 changed images in all three comparison roles |
| Native execution | [1,775 release observations and 14 synthetic observations](linked-runtime-witnesses/receipt.json) agree |
| Remaining clusters | [Ranked causes](ranked-causes.md) and [function totals](rank-family-counts.tsv) |
| Readiness | [Regenerated](readiness-receipt.json) from the accepted compiler milestone and immutable Part 1875 passing managed report. |

The linked compiler restores empty checked record updates and the established Boolean-return priorities. Previously failing ex_canvas_paint and sudoku_game compile and agree with pinned OTP on the captured effects. erl_bifs is byte-exact. The package preserves the generated Diameter line provenance, resolved transform reachability, constructed case literals, protected try failure location and shared BER tag and length work. Pinned source and staged proof details are recorded in the component receipts and [retake proofs](retake-proofs/release-corrections.json).

The normalized gate preserves the original [diagnostic exception](full-uncached/interim-normalized.stderr.log). [asn1ct_constructed_per:make_var/1](normalized-losses/asn1ct_constructed_per.terms) rematerializes an atom before a call that already receives the argument in the expected register. [erl_scan:lists_foldl_reverse/2](normalized-losses/erl_scan.terms) treats a literal external function as an effect and rematerializes nil. Both changes appear in the frozen Part 1955 images. Relative to Part 1954 the raw run adds two images and preserves every previous match.

The full managed suite was not rerun on this candidate. Earlier pinned original string Jaro cases exceeded the two existing ten-second harness deadlines during a measured 17.9–19 second workload. No current-head managed pass, source promotion or GitHub operation is recorded. The final Windows profile-equivalence stage was downstream from the recorded normalization failure.
