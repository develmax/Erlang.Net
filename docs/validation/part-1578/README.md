# Part 1578 — Fuse fixed binary literals into match commands

For fixed big-endian integer fields, the compiler checks literals in bs_match before allocating a captured tail. It coalesces adjacent fields and emits equality chunks of at most 31 bits, following OTP 29.1.1 beam_ssa_codegen.erl. Signed fields use their encoded bit pattern after a range check. The existing extraction and pattern test remain available for other fields.

The differential fixture confirms the same 31-bit and 9-bit operands as OTP. Both images produced the same six outcomes, covering a populated or empty tail, two literal mismatches, a non-byte-aligned tail, and a non-binary input. The fixture still has different frame allocation and capture registers. [Source](beam_fixed_literal_prefix.erl) · [Instructions](run.txt) · [Outcomes](outcomes.txt).

Three focused tests passed, including the signed prefix regression on the managed runtime. The corpus accepted and generated 1,366 files, with 390 Equal and 976 Different. ELDAPv3 matched from the C# ASN.1 generator. The change affected 87 release images. OTP accepted all 976 differing images for loading. The full runtime suite and canonical readiness were outside this run. [Tests](focused.txt) · [Corpus](otp-corpus.json) · [Changed images](changed.txt) · [Loader](loader-and-code.txt).
