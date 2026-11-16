# Part 961 rounding guard BIFs

Lower round, ceil and floor through the existing GC-aware unary guard instruction. The pinned OTP compiler uses gc_bif for each operation. The focused fixture checks negative fractional input and guard failure on an atom. The generated fixture still differs from OTP at the byte level. [Reference instructions](reference.S), [previous probe](probe-before.json) and [current probe](probe.json).

All three focused checks passed. The full corpus has 223 Equal, 1118 Different, 18 Unsupported and 7 SourceRejected. Qlc_pt now generates BEAM. ELDAPv3 remains Equal. [Corpus](otp-corpus.json) and [focused checks](focused.json).
