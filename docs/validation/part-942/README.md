# Part 942 ETS match specification source transform

Transform the observed ets:fun2ms literal function clauses during parsing. Encode head variables, whole-object aliases, captured constants, guard alternatives and tuple results as match specification terms. The supported subset excludes dbg and more complex pattern and expression forms. [OTP values](values.txt) and [pinned disassembly](reference.txt).

The deterministic probe returns the pinned match specification values but differs in Code, ExpT, LitT and Attr. Three focused checks passed. The full runtime suite and canonical readiness regeneration were outside this run. [Probe](probe.json) and [focused checks](focused.json).

The full Windows OTP 29.1.1 corpus has 223 Equal, 1015 Different, 35 SourceRejected and 93 Unsupported. All nine affected sources pass the ms_transform gate. Two modules generate BEAM and seven reach later generation blockers. ELDAPv3 remains Equal. [Corpus](otp-corpus.json) and [changed statuses](changed-status.txt).
