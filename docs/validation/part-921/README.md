# Part 921 fixed-width binary generators

The general qualifier loop now supports binary generators with fixed-width integer and binary segments. It advances an OTP-style match context by one full group per iteration and applies the generator patterns to that group's values. A relaxed mismatch skips the group and an incomplete group ends the loop. Strict mode saves the position and restores it before reporting badmatch on a mismatch or a partial group.

The pinned OTP probe covers one-bit traversal, multiple segments, relaxed filtering, strict exhaustion, strict mismatch, a partial strict tail and an invalid generator source. [Reference outcomes and instructions](reference.txt).

The general emitter keeps the iteration inside the surrounding function. The pinned OTP examples lift the iteration into a helper. Dynamic-width, zero-width and zipped binary generators remain separate.

All three selected compiler tests passed. The full Windows OTP 29.1.1 run reported 221 Equal, 815 Different, 105 SourceRejected and 225 Unsupported. Six modules moved from Unsupported to Different, including all three Megaco PER versions. No other status changed. [Focused report](focused.json) and [corpus report](otp-corpus.json).

Targeted inlining is now the largest single named emission site, with 52 first failures. There are 20 unsupported body templates, 18 unsupported function heads and 14 residual call sites.
