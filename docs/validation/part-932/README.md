# Part 932 xor-family operators

Lower bxor and bnot with GC BIF instructions and strict boolean xor with a plain BIF. Direct operand functions use argument registers. General expression and guard emission use the existing operand and failure paths. [Pinned disassembly](reference.txt) and [OTP values](values.txt).

The deterministic direct three-operator module matches OTP byte for byte. The general guard probe verifies guard rejection and strict xor badarg. The focused compiler checks passed 3/3. The full runtime suite and canonical readiness regeneration were outside this run. [Direct comparison](direct.json) and [focused checks](focused.json).

The full Windows OTP 29.1.1 corpus has 223 Equal, 946 Different, 56 SourceRejected and 141 Unsupported. Ten modules moved to Different and three reached later emission blockers. ELDAPv3 remains Equal. [Corpus](otp-corpus.json) and [changed statuses](changed-status.txt).
