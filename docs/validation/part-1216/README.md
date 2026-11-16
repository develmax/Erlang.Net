# Part 1216 — preserve slot reuse in boolean dispatch

The boolean select_val path now uses the same branch continuation and operand slot reuse as the generic case path. The selector error still retains the unmatched value.

Compared with part 1215, first allocation size moved closer in 336 functions and farther in none. Code size moved closer in 180 modules and farther in two. Compared with part 1214, the two boolean dispatch increments leave 2,729 first allocations closer and two farther, compared with 111 farther after part 1215 alone. Code size is closer in 549 modules and farther in 52 against part 1214.

All 1,366 images were generated. The Windows OTP 29.1.1 comparison produced 357 Equal and 1,009 Different. ELDAPv3 stayed Equal. OTP accepted all differing files. Three focused tests and the continuation execution probe passed.

- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [Allocation comparison](frame.txt)
- [Cumulative Code delta](cumulative-delta.json)
- [Cumulative allocation comparison](cumulative-frame.txt)
- [Focused tests](focused.json)
- [Loader](loader.txt)
- [Execution probe](probe.txt)
- [Opcode ranking](ranking.txt)
