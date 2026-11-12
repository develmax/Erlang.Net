# Part 939 guard extraction BIFs

Emit tl/1 with a unary BIF and binary_part/3 with a ternary GC BIF. The direct exact comparison form uses argument registers and the OTP byte-aligned matchable result type. General guards reuse the existing operand slots and failure branches. [Pinned disassembly and values](reference.txt).

The deterministic direct fixture is Equal, and the managed guard test covers improper tails, empty input, negative binary lengths and out-of-range slices. Three focused checks passed. The full runtime suite and canonical readiness regeneration were outside this run. [Probe](probe.json) and [focused checks](focused.json).

The full Windows OTP 29.1.1 corpus has 223 Equal, 1008 Different, 56 SourceRejected and 79 Unsupported. Three more modules generate BEAM and socket reached its cancel/2 body. ELDAPv3 remains Equal. [Corpus](otp-corpus.json) and [changed statuses](changed-status.txt).
