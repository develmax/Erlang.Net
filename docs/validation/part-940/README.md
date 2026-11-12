# Part 940 release macro inputs

Supply explicit preprocessing definitions in the release profile. Object macros participate in conditionals and normal expansion. Keep metadata-only definitions separate and use pinned options for the eight affected sources. [Pinned compile info](compile-info.txt) and [OTP values](values.txt).

Three focused checks passed, including string, atom and negative integer definitions and ifdef selection. The full runtime suite and canonical readiness regeneration were outside this run. [Focused checks](focused.json).

The full Windows OTP 29.1.1 corpus has 223 Equal, 1009 Different, 50 SourceRejected and 84 Unsupported. All eight formerly missing definitions are resolved. One module generates BEAM, five reach debug-form generation, and two stop on ms_transform. ELDAPv3 remains Equal. [Corpus](otp-corpus.json) and [changed statuses](changed-status.txt).
