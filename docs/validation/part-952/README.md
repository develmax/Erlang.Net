# Part 952 branch bindings in match operands

Carry live bindings out of a case, if or receive used as the right operand of a match. The branch result is evaluated once. The emitter transfers branch bindings before matching that result, so a left operand can test a variable bound on the right.

The previous compiler rejects the case fixture. The updated compiler emits it and returns the same values as OTP for both branches. The fixture still differs at the byte level. [Previous probe](probe-before.json), [current probe](probe.json), [reference values](reference.txt) and [focused checks](focused.json).

The full Windows corpus has 223 Equal, 1097 Different, 39 Unsupported and 7 SourceRejected. Six more modules generate BEAM. ELDAPv3 remains Equal. [Corpus](otp-corpus.json) and [changed statuses](changed-status.txt).
