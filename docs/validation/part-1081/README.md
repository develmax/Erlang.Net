# Part 1081 retain prefix-only inputs in X registers

For a safe entry binding sequence, the general emitter uses lexical reference analysis on the continuation. A parameter read only by the prefix is still available in its incoming X register and needs no Y slot. The prefix writes only Y registers, so an input remains available for every lookup. Inputs used by the continuation retain their existing Y bindings.

Corpus results are 348 Equal, 1015 Different, 3 SourceRejected. ELDAPv3 is Equal. Compared with part 1080, the Code length gap narrowed in 2 files and widened in 0 files. Status comparison returned 0 changed rows.

- [Full corpus](otp-corpus.json)
- [OTP loader results](loader.txt)
- [Focused checks](focused.json)
- [OTP execution probe](after.txt)