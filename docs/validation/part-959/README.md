# Part 959 receive tuple guards

Accept tuple operands in selective receive guards. Preserve live scratch registers during tuple allocation. Accept negated total tests as the left operand of a guard disjunction and allow a guarded operation to fail on the right.

The fixture checks tuple-key lookup, exact tuple comparison, short circuit and mailbox order after rejected messages. Its results match OTP. The fixture still differs in emitted bytes. All three focused checks passed. [Previous probe](probe-before.json), [current probe](probe.json), [reference values](reference.txt) and [focused checks](focused.json).

The Windows corpus has 223 Equal, 1115 Different, 21 Unsupported and 7 SourceRejected. Disk_log and prim_tty now emit BEAM. Supervisor passes the receive guard and reaches the unsupported min/2 guard. ELDAPv3 remains Equal. [Corpus](otp-corpus.json).
