# Part 925 selective receive patterns and type guards

The compiler admits literal-key map patterns and type predicates that have matching operations in the managed receive planner. The or branch lowers to jumps only for boolean tests that cannot raise, so a guard does not incorrectly try another branch after an exception. Unsupported cases identify the rejected pattern or guard.

The pinned OTP probe skips a map with a missing key and one with an invalid value type, selects the next map and preserves the order of the skipped messages. It also checks timeout and empty mailbox behavior. [Reference result and instructions](reference.txt).

The full Windows OTP 29.1.1 run reported 221 Equal, 849 Different, 105 SourceRejected and 191 Unsupported across 1,366 files. Ten modules moved from Unsupported to Different and no status regressed. Nineteen remaining receive failures report a binary guard expression. [Corpus report](otp-corpus.json).

All three selected tests passed. A separate probe variant with a catch-all receive exposed an existing validator restriction on an unreachable loop_rec_end. The mailbox probe uses explicit map patterns when draining the skipped messages. [Focused report](focused.json).
