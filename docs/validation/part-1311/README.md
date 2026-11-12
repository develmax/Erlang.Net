# Part 1311

The entry call planner accepts a cons argument whose head and tail are both bound variables. Clause emission checks heap space after the list shape test, before extracting the head. If the call also needs the original value of the cons destination, the planner can put the new list in the head register after its last use. Existing parallel move scheduling then prepares the remaining arguments.

The focused case compares complete instructions against OTP 29.1.1 and exercises proper and improper accumulators, the empty list, and a non-list fallback. It also checks that the original accumulator reaches the call separately. The proper list and tuple cases from part 1310 still pass.

- [Focused verification](probe.txt)
- [Focused command](check-variable-tail.ps1)
- [Previous list argument verification](proper-list-regression.txt)

- [prim_inet:rev/2 comparison](prim-inet-rev.txt)
- [prim_zip:reverse/2 comparison](prim-zip-reverse.txt)
- [Clause and argument forms still affected](variable-tail-clause-forms.txt)
- [Additional patterns in affected clauses](additional-head-patterns.txt)

All 1,366 sources were accepted and generated a BEAM. The full comparison has 361 Equal and 1,005 Different. Eleven images changed and each narrowed its Code size gap. The largest reductions were 953 bytes in lists and 510 bytes in sofs. OTP accepted all differing images. The intermediate register move regression in the two reverse functions was corrected before the final run.

- [Full corpus](otp-corpus.json)
- [Changed images](delta.json)
- [OTP loader](loader.txt)
- [Remaining instructions in auth:check_cookie/2](auth-cookie.txt)
