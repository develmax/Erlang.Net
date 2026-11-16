# Part 917 maybe expressions and map_size guards

The general emitter keeps the value of a failed conditional match and dispatches to else clauses using the incoming bindings. Without an else clause it returns the unmatched value. A successful final conditional match returns the right-hand value. Unmatched else clauses raise else_clause with the failed value.

The map_size/1 guard uses the existing GC BIF path with the enclosing guard failure label. The pinned OTP disassembly and runtime output cover successful maybe evaluation, early and late mismatch, a guarded else, an unmatched else and non-map guard input. [Pinned OTP run](reference.txt).

OTP can arrange or lift failure clauses differently from the general emitter, so the new lowering is checked for semantics independently of complete release image comparison.

All three selected compiler checks passed, including nine runtime outcomes checked against the reference. The full runtime suite and canonical readiness regeneration were outside this run. [Focused report](focused.json).

The full Windows OTP 29.1.1 run reported 221 Equal, 792 Different, 105 SourceRejected and 248 Unsupported across 1,366 files. Twenty-three modules moved from Unsupported to Different, with no other status changes. Of these, twelve previously stopped at map_size, eight at maybe and three at the general emission planner. [Corpus report](otp-corpus.json).
