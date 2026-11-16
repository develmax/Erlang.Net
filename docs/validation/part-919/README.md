# Part 919 named recursive funs

A general named fun lifts its clauses with the captured variables appended to the arguments. Calls to the active self binding use the lifted local entry and pass the same captures. A self value is reconstructed from the same fun table entry. The focused managed runtime test agrees with pinned OTP for a captured value, recursive arithmetic and a returned self value. [Reference](reference.txt) and [focused report](focused.json).

The GeneralEmitter source was restored after an edit truncated it. The recovery uses the saved Part 824 source and the subsequent logged edits. Formatting differs from the pre-edit file. The build and all three focused tests passed after recovery.

The full Windows OTP 29.1.1 run reported 221 Equal, 807 Different, 105 SourceRejected and 233 Unsupported across 1,366 files. Six modules moved from Unsupported to Different. Five previously stopped at a named fun and crashdump stopped at the emission planner. No status regressed. Only three unchanged statuses have new diagnostics, all from previously unsupported fun modules. [Corpus report](otp-corpus.json).

The next expression group is binary comprehensions. [Pinned disassembly](next-binary-comprehension.txt).

The wider compiler block includes an original dynamic apply values/stacks mismatch. The same test fails on the saved Part 918 binaries. [Baseline reproduction](dynamic-apply-baseline.json).

The original source escape, documentation and type-form tests also fail on the saved Part 918 binary. [Source baseline reproduction](source-baseline.json).

The additional 3,827-case compiler block was still running at the checkpoint (execution session 47592). Four failures observed so far reproduced on the archived Part 918 binaries. The final report target is artifacts/beam-backend/part919-compiler.json.

The compiler block finished with 3,719 passed and 108 failed. All 108 failed case names reproduced on the archived Part 918 binaries when selected separately. [Current block](compiler-block.json) and [baseline reproduction](compiler-block-baseline.json).
