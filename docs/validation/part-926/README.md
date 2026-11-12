# Part 926 computed receive guards and catch-all clauses

The compiler admits the pure operations already supported by the receive matcher as operands in guard comparisons. These include record access, addition, self, node, byte_size, tuple_size, length and element. Scratch registers are reused with their corresponding compiler slots. A reachable unconditional variable clause ends receive pattern emission and removes the unreachable rejection instruction.

The pinned OTP probe checks an exception inside a computed guard, a false comparison, a matching message, timeout and catch-all reads of the two messages skipped earlier. [Reference result and instructions](reference.txt).

GC guard BIFs keep the live mailbox scratch registers. Computed expressions inside semicolon-separated guard alternatives remain gated until the compiler tracks the register state across those branches.

All three selected compiler tests passed. The final full Windows OTP 29.1.1 run reported 221 Equal, 863 Different, 105 SourceRejected and 177 Unsupported. Fourteen modules moved from Unsupported to Different, with no other status changes. Eleven modules still stop at receive admission. [Focused report](focused.json) and [corpus report](otp-corpus.json).
