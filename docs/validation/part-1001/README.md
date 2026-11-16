# Part 1001 wx protected class selection

Emit two protected parent-class checks for a typed wx object and pass the first successful class to typeCast/2 before queuing the command. Preserve the exception handlers, stack slots and line information. The deterministic fixture exercises this layout.

The complete Windows OTP 29.1.1 corpus has 293 Equal, 1070 Different and 3 SourceRejected. WxTopLevelWindow is the new exact image. ELDAPv3 and all prior matches remain Equal. [Corpus report](otp-corpus.json). [Three focused checks passed](focused.json).

Among the 16 class-union candidates, the change affected three modules. The instructions for wxGridBagSizer findItem/2 and wxImageList add/2 match the reference after label normalization. Both complete images retain other differences. [Targeted results](wx-results.txt).
