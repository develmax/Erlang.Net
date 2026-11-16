# Part 934 zipped list generators

Emit a joint list loop for zip qualifiers. Keep the current remainders until pattern selection succeeds or the relaxed skip pattern matches. Reject invalid tails, length mismatches and failed strict rows with bad_generators carrying the remaining sources. Reuse the existing OTP-derived skip-pattern rewrite. Zips containing map or binary generators remain unsupported. [Pinned disassembly](reference.txt) and [OTP values](values.txt).

The deterministic probe still differs. OTP emits separate recursive comprehension helpers, while this implementation emits a loop inside the containing function. The three focused checks passed. The full runtime suite and canonical readiness regeneration were outside this run. [Probe](probe.json) and [focused checks](focused.json).

The full Windows OTP 29.1.1 corpus has 223 Equal, 967 Different, 56 SourceRejected and 120 Unsupported. Thirteen more modules generate BEAM and v3_core reached a later blocker. ELDAPv3 remains Equal. [Corpus](otp-corpus.json) and [changed statuses](changed-status.txt).
