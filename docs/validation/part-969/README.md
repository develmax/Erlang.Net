# Part 969 wide integer dispatch and module info calls

Emit integer dispatch operands outside the Int32 range using the existing compact encoding. Accept OTP report options during source binding. Record generated module_info entry labels in general planning and final emission so that local references resolve to those functions.

Both deterministic fixtures match OTP byte for byte. All three focused checks passed. [Probes](probe.json) and [focused checks](focused.json).

The full corpus has 223 Equal, 1135 Different, 1 Unsupported and 7 SourceRejected. Win32reg and fprof now emit BEAM. Xmerl_ucs passes report_warnings and reaches warn_unused_vars. ELDAPv3 remains Equal. [Corpus](otp-corpus.json).
