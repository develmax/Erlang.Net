# Part 968 binary receive patterns and constant append

Emit fixed binary prefixes with captured tails in receive using adjacent binary match instructions. Reuse the temporary context register for the tail and preserve live receive scratch registers. The runtime test confirms that an unmatched packet remains in the mailbox and the matching packet returns its payload.

Fold constant list append during expression normalization, including an improper right tail. The deterministic append fixture now matches OTP byte for byte. All three focused checks passed. [Previous probe](probe-before.json), [current probe](probe.json), [OTP receive result](reference.txt) and [focused checks](focused.json).

The full corpus has 223 Equal, 1133 Different, 3 Unsupported and 7 SourceRejected. Inet_gethost_native now emits BEAM. Fprof still fails at the specialized body fallback. ELDAPv3 remains Equal. [Corpus](otp-corpus.json).
