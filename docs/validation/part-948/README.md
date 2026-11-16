# Part 948 diagnostic compile options

Accept warn_missing_spec and nowarn_unexported_function through the existing diagnostic option policy. Accept the compiler verbosity flag alongside other non-emission options. The deterministic fixture matches the BEAM produced by the pinned OTP compiler. The managed compiler does not reproduce the reference warning text.

The source for the reference option descriptions is OTP 29.1.1 compile.erl and erl_lint.erl at ad05823719d77c8faee87348ea39513d4e2f99c5. [Probe](probe.json).

The full corpus has 223 Equal, 1040 Different, 96 Unsupported and 7 SourceRejected. ELDAPv3 remains Equal. JSON and tprof generate BEAM. Eunit_test reaches debug metadata emission. [Corpus](otp-corpus.json) and [changed statuses](changed-status.txt).

All three focused compiler checks passed. [Focused results](focused.json).
