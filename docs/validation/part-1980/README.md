# Part 1980 — Correct stack order and extend owned compiler lowering

| Validation | Result |
| --- | --- |
| Source and compiler identity | [Frozen input](source-freeze.json) |
| Selected compiler cases | [576 passed](focused-verified.json) |
| Scoped release | [353 files in 595.78 seconds](pilot-receipt.json). 26 Equal, 327 Different. |
| Uncached Windows OTP 29.1.1 release | [1,366 files](full-uncached/otp-corpus.json) in 748.68 seconds. 511 Equal, 855 Different, 0 Unsupported, 0 SourceRejected. |
| Raw byte equality | [unix_telnet is a new match](full-uncached/status-changes.json). All 510 prior matches retained. |
| Normalized function bodies | [25 gained and 0 lost](rank-counts.json). Forty frame proxies moved nearer. |
| Frame widths | [All 212 earlier recoveries retained](full-widths/receipt.json). The five later width changes from Part 1979 have their own pending validation. |
| Behavior | [34 native observations](native-scoped/receipt.json). 289 changed images prepared and 3 shared on-load restrictions. |
| Whole repository integration | [Part 1978](../part-1978/README.md). Part 1980 was measured in the sealed compiler closure. |

The earlier four focused failures and their [repairs](four-regression-repairs.json) are preserved. The raw module difference is computed over complete files. The [remaining cause table](ranked-causes.md) uses disassembly diagnostically and reports each distinct coverage measure separately.
