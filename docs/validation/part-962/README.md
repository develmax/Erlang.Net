# Part 962 nested branch exports

Propagate a continuation through nested branch bodies, blocks and sequences. Preserve the result of the branch expression while copying live bindings to its enclosing join. The prior compiler rejects the nested branch fixture. The updated compiler emits it and the three branch results match the pinned OTP VM. The fixture still differs at the byte level. [Previous probe](probe-before.json), [current probe](probe.json) and [reference values](reference.txt).

All three focused checks passed. The full corpus has 223 Equal, 1121 Different, 15 Unsupported and 7 SourceRejected. Observer_procinfo, edlin_expand and escript now emit BEAM. ELDAPv3 remains Equal. [Corpus](otp-corpus.json) and [focused checks](focused.json).
