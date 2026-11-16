# Part 1577 — Keep fixed binary match captures in registers

A single unguarded clause with a fixed binary prefix and a trailing binary can rebuild a result from X registers. The compiler saves the starting bit position and restores the original argument before reporting a pattern failure. It reuses the fixed match and binary construction emitters and includes the byte alignment of captured operands in the Type chunk. References are OTP 29.1.1 beam_ssa_bsm.erl and beam_types.erl.

The corpus audit found 144 oversized-frame functions across 46 modules with both binary tail extraction and construction. The new rule handles the fixed-pattern subset. In diameter_codec:hop_by_hop_id/2 it removes four stack slots and matches OTP instructions after label normalization. [Audit](binary-frames.txt) · [Release instructions](hop_by_hop_id.txt).

A permanent whole-module test compares the 672-byte deterministic image from OTP. OTP VM execution agreed on five outcomes covering a nonempty tail, an empty tail, a non-byte-aligned input, an invalid field value and a non-binary argument. [Fixture instructions](rebuild-instructions.txt) · [Outcomes](rebuild-outcomes.txt).

Four focused tests passed. The full corpus accepted and generated 1,366 images, with 390 Equal and 976 Different. ELDAPv3 matched from the C# ASN.1 generator. Only diameter_codec changed. Its Code section moved 12 bytes closer to OTP, and OTP accepted all 976 differing images for loading. The full runtime suite and canonical readiness were outside this run. [Tests](focused.txt) · [Corpus](otp-corpus.json) · [Functions](normalized-functions.txt) · [Loader](loader-and-code.txt).
