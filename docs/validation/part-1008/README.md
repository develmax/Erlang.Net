# Part 1008 call-result construction and compiler release profiles

Emit a single remote call before building nested literal tuples and lists around its result. Allocate the required heap once and keep the call result in X0. The deterministic fixture exercises the empty-list entry pattern and nested construction.

Apply the verified compiler application profiles, including the core_parse option difference and the existing beam_asm definition. All 59 compiler images have matching CInf. Dbgi differences fell from 57 to 17 and Docs differences fell from 56 to 8. [Release profile groups](profile-groups.txt).

The full Windows OTP 29.1.1 corpus has 309 Equal, 1054 Different and 3 SourceRejected. Logger_sup is the new exact image. All previous matches remain Equal, including ELDAPv3. [Corpus report](otp-corpus.json). [Three focused checks passed](focused.json).
