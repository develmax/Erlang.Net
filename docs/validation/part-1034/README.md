# Part 1034 tuple construction and literal dispatch

Build tuple arguments for direct remote tail calls. Emit register-only tuple returns when the first clause checks an atom and the second clause accepts all arguments. Reserve both outgoing registers even when the function has only one input.

The normalized instructions for alarm_handler:set_alarm/1, clear_alarm/1, handle_call/2 and terminate/2 match OTP. Start_link/0 and handle_event/2 still differ. The report changed for 28 modules.

The full Windows OTP 29.1.1 corpus has 341 Equal, 1022 Different and three SourceRejected. The exact image set is preserved, including ELDAPv3. The final run has zero CompilerError entries.

- [Full corpus](otp-corpus.json)
- [Three focused checks](focused.json)
