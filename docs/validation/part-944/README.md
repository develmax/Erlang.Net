# Part 944 on-load entry marker

Validate one defined zero-arity on_load function in the BEAM parser. Retain private initializers and emit the entry marker. Preserve the original source declaration in debug forms. The separate C# source backend retains its existing admission behavior. [Pinned disassembly and abstract forms](reference.txt).

The deterministic probe matches OTP byte for byte. Three focused compiler checks and one loader test passed. The full runtime suite and canonical readiness regeneration were outside this run. [Probe](probe.json), [focused checks](focused.json) and [loader check](loader.json).

The full Windows OTP 29.1.1 corpus has 223 Equal, 1025 Different, 25 SourceRejected and 93 Unsupported. Four additional modules generate BEAM and crypto reached a sigil expression. ELDAPv3 remains Equal. [Corpus](otp-corpus.json) and [changed statuses](changed-status.txt).
