# Part 933 integer range and native record predicates

Dynamic is_integer/3 uses bif3. A guard with ordered constant bounds emits an integer test and two comparisons with the inferred operand types. is_record/1 uses the native-record test for guard branches and bif1 when its value is needed. [Pinned disassembly](reference.txt) and [OTP values](values.txt).

All three focused tests passed. The deterministic probe still differs in Code, ExpT and Attr. The full runtime suite and canonical readiness regeneration were outside this run. [Probe](probe.json) and [focused checks](focused.json).

The full Windows OTP 29.1.1 corpus has 223 Equal, 954 Different, 56 SourceRejected and 133 Unsupported. Eight modules now generate BEAM and two reached later emission blockers. ELDAPv3 remains Equal. [Corpus](otp-corpus.json) and [changed statuses](changed-status.txt).
