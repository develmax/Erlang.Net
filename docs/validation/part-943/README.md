# Part 943 Diameter record accessors

Generate the generic and per-record accessors and exports for diameter_exprecs. Preserve upstream function order and generated function line numbers. Generated debug forms for release profiles remain unsupported. [Pinned lines](generated-lines.txt) and [OTP values](values.txt).

Three focused checks passed. The deterministic probe still differs in code and metadata. The full runtime suite and canonical readiness regeneration were outside this run. [Probe](probe.json) and [focused checks](focused.json).

The full Windows OTP 29.1.1 corpus has 223 Equal, 1021 Different, 29 SourceRejected and 93 Unsupported. All six affected Diameter modules now generate BEAM. ELDAPv3 remains Equal. [Corpus](otp-corpus.json) and [changed statuses](changed-status.txt).
