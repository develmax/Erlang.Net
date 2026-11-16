# Part903 Inline local bindings

The targeted inliner rewrites local matches in supported templates. It assigns fresh names to variables introduced by a match, reuses previous bindings for equality checks and rewrites list and tuple patterns. Multiple inlined copies can use the same original names without sharing bindings.

The regression inlines a stack pop twice into a caller with a variable of the same name as the helper's local. Both BEAM images returned {1,2,outer} on OTP. The instruction layouts differ.

All three focused compiler checks passed. [Focused report](focused.json).

The full corpus recorded 137 Equal, 818 Different, 105 SourceRejected and 306 Unsupported. Three additional modules generated differing BEAM images. [Corpus report](otp-corpus.json).
