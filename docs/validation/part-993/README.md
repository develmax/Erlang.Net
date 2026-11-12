# Part 993 documentation call wrapping

Match the OTP 29.1.1 pretty printer when an equivalent call with atomic arguments exceeds 72 columns. Wrap at the argument boundary and align continuation lines with the first argument. The existing equivalent-call fixture now exercises a trailing empty list at the wrap point.

The full Windows OTP 29.1.1 corpus has 273 Equal, 1090 Different and 3 SourceRejected. WxScrolledWindow is the only new match. ELDAPv3 and all previous matches remain Equal. [Corpus report](otp-corpus.json).

The compiler and corpus runner built successfully. [Three focused checks passed](focused.json).
