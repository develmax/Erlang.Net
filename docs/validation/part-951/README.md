# Part 951 conditional and receive bindings

Carry live bindings from each if clause, each receive clause and the receive timeout body into shared frame slots. The same sequence analysis now runs when emitting a function through the decoder sequence path. The existing case lowering uses the shared slot transfer.

The focused fixture exercises both if branches, a matching message, a timeout and the earlier case bindings. All three focused checks passed. OTP and the managed runtime return identical values for the fixture. The BEAM files still differ. [Reference values](reference.txt), [previous probe](probe-before.json), [current probe](probe.json) and [focused checks](focused.json).

The complete release run has 223 Equal, 1091 Different, 45 Unsupported and 7 SourceRejected. Seven more modules emit BEAM. ELDAPv3 remains Equal. [Corpus](otp-corpus.json) and [changed statuses](changed-status.txt).

Code differs in all 1091 generated images with mismatches. Dbgi differs in 1055, CInf in 1054 and Docs in 996.
