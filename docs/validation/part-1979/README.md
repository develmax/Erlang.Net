# Part 1979 — Common entry checks and call roots

| Validation | Result |
| --- | --- |
| Source and compiler identity | [Frozen input](source-freeze.json) |
| Selected compiler cases | [571 passed](focused-verified.json) |
| Scoped release | [230 files in 577.04 seconds](pilot-receipt.json) |
| Uncached Windows OTP 29.1.1 release | [1,366 files](full-uncached/otp-corpus.json) in 735.38 seconds. 510 Equal, 856 Different, 0 Unsupported, 0 SourceRejected. |
| Raw module status | 0 gained, 0 lost |
| Normalized function bodies | [11 gained and 0 lost](rank-counts.json) |
| Frame widths | [All 212 earlier recoveries retained](full-widths/receipt.json). Five different functions widened in this candidate. One further proxy alert is an actual frame removal. |
| Native execution | [32 selected observations](native-scoped/receipt.json). 211 changed images prepared and 1 shared loader restriction. |
| Source integration | Pending recovery of the five newly widened functions and validation on the resulting compiler. |

The [ranked cause table](ranked-causes.md) records overlapping module and function groups. The frozen corpus was recompiled from the source and build inputs. Earlier native-driver syntax failure remains in the archive. The corrected native recipe covered its seven missing observations and bound the prior 25 to their exact image hashes. The source-binding and whole repository evidence for the integrated compiler remain in [Part 1978](../part-1978/README.md).
