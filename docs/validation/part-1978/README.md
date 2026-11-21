# Part 1978 — Owned frame widths, call roots and binary list helpers

| Validation | Result |
| --- | --- |
| Windows OTP release | OTP-29.1.1 at ad05823719d77c8faee87348ea39513d4e2f99c5 |
| Selected managed checks | [567 passed](focused-verified.json) |
| Scoped release | [230 files in 549.68 seconds](pilot-receipt.json) |
| Full uncached release | [Result](full-uncached/otp-corpus.json), 510 Equal, 856 Different, 0 Unsupported, 0 SourceRejected, in 643.06 seconds |
| Byte-exact modules | 0 gained, 0 lost |
| Parsed instructions | [Normalized function results](rank-counts.json), 6 gained and 0 lost |
| Known frame-width regressions | [Actual width vectors for 212 functions](full-widths/receipt.json), 212 retained versus Part 1976 |
| Native | [25 behavior and preparation observations](native-scoped/receipt.json), 401 changed release images prepared and 2 shared loader restrictions |
| Repository-wide local validation | Final resumed stage exit 0, 1,321 catalog preparations and 9 native catalog outcomes, 10357 passed and 0 failed. [Initial run](general-native-host/receipt.json), [catalog recovery](general-apphost-confirmed/receipt.json), [readiness and packages](general-readiness-and-packages/receipt.json) |
| Readiness | [Scoped comparison](readiness/receipt.json) |
| Remaining groups | [Ranked coverage and likely responsible passes](ranked-causes.md) |

The selected package uses the same compiler binary for native witnesses and release comparison. The Windows OTP baseline, generated files and build inputs have independently recorded identities. This work runs locally. A raw module, a normalized body and a native observation each use their stated acceptance criteria.
