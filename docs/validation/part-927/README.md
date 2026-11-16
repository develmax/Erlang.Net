# Part 927 guarded inline dispatch

Supported guarded and multi-clause inline targets now expand into an ordered match on the already evaluated arguments. Each branch uses fresh names. An unmatched call branches to an OTP-named function_clause stub carrying the original arguments and the target source location. The compiler also substitutes through case and if templates with supported patterns.

The pinned OTP probe records two successful calls and the function_clause stack entry for the unmatched input. The OTP optimizer tail-calls the failure helper in the probe. The general emitter can keep a caller frame. [Reference result and instructions](reference.txt).

All three focused compiler tests passed. The full runtime suite and canonical readiness regeneration were outside this run. [Focused report](focused.json).

The full Windows OTP 29.1.1 run reported 221 Equal, 873 Different, 105 SourceRejected and 167 Unsupported across 1,366 files. Ten moved from Unsupported to Different, with no other status changes. Nineteen modules still stop at targeted inlining. [Corpus report](otp-corpus.json).
