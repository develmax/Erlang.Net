# Part 1977 — Early matched list entry and helper identities

The frozen candidate compiled all 1,366 Windows OTP 29.1.1 release images from source in 691.54 seconds across five uncached workers. 510 files were Equal and 856 Different. Unsupported and SourceRejected were zero. Every previous exact match remained.

| Check | Actual outcome |
| --- | --- |
| Selected compiler checks | [560 passed](focused-verified.json) |
| Release selection | [30 Equal and 80 Different](pilot.json), without losses |
| Whole corpus | [Raw images](full-uncached/otp-corpus.json), [effective profiles](profile-equivalence.json), [tool and source identity](source-freeze.json) |
| Functions | [9 normalized gains, 0 losses](rank-counts.json) |
| Frames | [26 nearer, 212 farther](frame-width/all-212-farther-widths.json) |
| Sections | [Three new exact sections](full-uncached/section-delta.json) |
| Native | [25 execution and preparation observations](native-scoped/receipt.json), [144 changed images prepared](loader-rollup.json) |
| Residuals | [Ranked by coverage and dependencies](ranked-causes.md) |

The native scenarios include parser entry behavior, escaped closures, checked record outputs and simulated Telnet transport. The underlying OTP installation and reference checkout retain their original bytes. The compiler raw and normalized retention marker passed its original scope. The independent width findings keep this candidate provisional. Local full repository validation remains the previous package's result and was not repeated on this interim candidate.
