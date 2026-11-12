# Part 1176 — share entry tuple literal tests

The general emitter checks identical literal fields once after it proves the tuple shape and arity. These checks run in a scratch X register before the stack frame is allocated. The checks retain original arguments for function_clause failures. Fields that vary across clauses are checked in their own branches.

The probe passed with both OTP and C# output, including two successful branches, a guard failure, nonmatching fields and original arguments in failure stacks. Both outputs check the common integer field before allocation. The generated frames remain larger than the reference.

The Windows OTP 29.1.1 corpus produced 1,363 images out of 1,366. Results were 356 Equal, 1,007 Different and 3 SourceRejected. ELDAPv3 remained Equal. The change affected 35 differing images and reduced the Code length gap in 16. OTP prepared all 1,007 differing images for loading and three focused tests passed.

- [Full corpus](otp-corpus.json)
- [Delta](delta.json)
- [Loader](loader.txt)
- [Focused tests](focused.json)
- [Probe assertions](run.eval)
- [Before](before.txt)
- [After](after.txt)
- [OTP](otp.txt)
- [Opcode ranking](ranking.txt)
