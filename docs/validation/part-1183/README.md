# Part 1183 — omit unused arguments from shared clause frames

For functions with an unconditional final clause, the general emitter omits an argument slot if every clause has an unused variable in that position. Pattern keys, repeated bindings, guards and body references preserve the slot. Functions that need original arguments on the failure epilogue keep the existing allocation.

The OTP probe covers both clauses and includes a guard and a repeated binding. Both outputs pass the assertions. The generated frame is one slot smaller in each probe function. The generated images still differ from OTP.

The Windows OTP 29.1.1 corpus generated 1,363 of 1,366 images. Results were 356 Equal, 1,007 Different and 3 SourceRejected. ELDAPv3 remained Equal. The change affected 135 differing images. The Code length gap narrowed in 132 and widened in three. OTP prepared all differing images for loading and three focused tests passed.

- [Full corpus](otp-corpus.json)
- [Delta](delta.json)
- [Focused tests](focused.json)
- [Loader](loader.txt)
- [Probe source](dead_clause_input_probe.erl)
- [Probe assertions](run.eval)
- [Before](before.txt)
- [After](after.txt)
- [OTP](otp.txt)
