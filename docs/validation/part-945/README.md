# Part 945 string sigils

Read default, b, B, s and S sigils with OTP delimiters, character-list or binary results, and raw or escaped text. Reuse triple-quoted indentation and character escape handling. [OTP values](values.txt).

The deterministic probe matches byte for byte, including the escaped triple-quoted case. Three focused checks passed. The full runtime suite and canonical readiness regeneration were outside this run. [Probe](probe.json) and [focused checks](focused.json).

The full Windows OTP 29.1.1 corpus has 223 Equal, 1030 Different, 18 SourceRejected and 95 Unsupported. Five additional modules generate BEAM and two reach later emission blockers. ELDAPv3 remains Equal. [Corpus](otp-corpus.json) and [changed statuses](changed-status.txt).
