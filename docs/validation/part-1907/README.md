# Part 1907 full Windows OTP 29.1.1 comparison

| Equal | Different | Unsupported | SourceRejected | Seconds |
| ---: | ---: | ---: | ---: | ---: |
| 420 | 946 | 0 | 0 | 721.377 |

Four isolated workers compiled all 1,366 release inputs without compiler-result caching. The release source profiles are unchanged. All 418 earlier raw matches are retained. X509-ML-KEM-2025 and snmpm_server_sup newly match.

Windows ERTS prepares all 60 changed images. Independent validated images match the two new release hashes. The supervisor evidence includes seven actual release scenarios. KEM is covered in the same source-derived native comparison with 54 recorded outcomes across KEM and DSA. Normalized instruction equality gains 277 functions with no losses. Eight frame widths move toward OTP. The Mnesia Schema three-atom chain restores its two-slot frame. Ten raw chunks newly match without a prior equal chunk becoming different.

The 103-file uncached pilot has 18 Equal and 85 Different in 304.561 seconds. All 117 fast cases pass, and the exclusive whole-module case passes in 29.641 seconds. The exact predecessor compiler fails the new separate three-atom assertion on the same test binary. The prior Part 1898 complete managed result and last green readiness snapshot remain the records for their original source identities.

The accumulated full raw gate took 721.377 seconds versus 894.391 seconds for Part 1906. Different compilers and other host work ran during the two measurements. The two observations do not isolate an optimizer or infrastructure speedup.

The source-helper census is diagnostic and built from Part 1905's frozen term rank. Its 303 parsed module inputs retain separate generated-source or macro exclusions. In parents with no observed EPP errors and only the single unfiltered source category, 837 list-helper keys are absent. The repeated shape selection screens the source independently of whether the active compiler truly omits a helper or emits it under another name or arity.

[Full raw report](full-uncached/otp-corpus.json) · [Native proof](native-early-records/receipt.json) · [Scoped tests](focused-verified.json) · [Predecessor error](atom-parent-regression/report.json) · [Source shape census](source-helper-census/shape-summary.json).
