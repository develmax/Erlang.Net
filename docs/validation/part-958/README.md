# Part 958 auto-imported function references

Emit external fun literals for bare references to auto-imported Erlang BIFs. Resolve declared local functions first and reuse source binding rules for auto-import suppression. The direct capture fixture is byte-identical to OTP 29.1.1. [Probe](probe.json).

All three focused checks passed. The full Windows corpus has 223 Equal, 1113 Different, 23 Unsupported and 7 SourceRejected. Five additional modules generate BEAM. ELDAPv3 remains Equal. [Focused checks](focused.json), [corpus](otp-corpus.json) and [changed statuses](changed-status.txt).
