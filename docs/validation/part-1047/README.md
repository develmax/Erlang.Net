# Part 1047 register-only literal clause dispatch

The general emitter now checks supported literal heads directly in the incoming X registers when every clause ends in a static tail call with simple arguments. The checks run before any argument moves. Failure can enter the next clause or go directly to func_info with the original arguments. Repeated bindings and guards retain their existing lowering paths.

The execution probe verifies integer and atom selection, catch-all selection, and a function_clause stack frame with the original arguments after a float fails exact integer matching. Both probe functions now have no allocation, as in OTP. For mixed literal types, OTP still emits different dispatch code, including an atom type test and a typed operand.

The number of paired functions with an ordinary allocation only in generated code fell from 22243 to 21602. All 1021 differing generated images prepared successfully in OTP. The full corpus admitted and generated 1363 of 1366 files. All per-file statuses remain unchanged at 342 Equal, 1021 Different and 3 SourceRejected. ELDAPv3 remains Equal.

- [Full corpus](otp-corpus.json)
- [Focused checks](focused.json)
- [Frame counts before](frames-before.txt)
- [Frame counts after](frames.txt)
- [Loader results](loader.txt)
- [Generated execution probe](after.txt)
- [OTP execution probe](otp.txt)
