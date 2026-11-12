# Part 1045 mixed constant and register moves

The general direct tail-call emitter now schedules constants and register copies together. A constant can terminate a dependency chain. This preserves the incoming value of its destination until the moves that read it have run. Other call paths retain their existing scheduling.

The mixed argument probe executed successfully in OTP. Its instruction order still differs. The generated code loads the independent marker constant earlier than the OTP codegen listing.

Among paired lifted tail bodies, seven newly match and two no longer match. The two losses are in cover and qlc. The comparison excludes labels, line markers and function headers, and compares decoded operands. All 1021 differing generated images still prepare successfully in OTP.

The full corpus admitted and generated 1363 of 1366 files. All per-file statuses remain unchanged at 342 Equal, 1021 Different and 3 SourceRejected. ELDAPv3 remains Equal.

- [Full corpus](otp-corpus.json)
- [Focused checks](focused.json)
- [Lifted body transitions](tail-transitions.txt)
- [Loader results](loader.txt)
- [Mixed argument probe](mixed_move_probe.erl)
- [Generated probe before](before.txt)
- [Generated probe after](after.txt)
- [OTP codegen listing](mixed_move_probe.codegen)
