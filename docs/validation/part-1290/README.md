# Part 1290 — Clear dead frame slots before a case branch call

A boolean case that feeds a terminal external call can clear its frame after preparing the inner branch call's register arguments. The nested call and the final consumer only use X registers from that point.

The focused probe checks the cleanup in both arms, their results and the case failure location. In gl:init_nif/0, normalizing label numbers and swapping Y0 and Y1 now makes the instruction sequence identical to OTP.

The full corpus generated all 1,366 images with 359 Equal, 1,007 Different and zero source rejections. ELDAPv3 remains Equal. OTP accepted all differing pairs. Only gl changed, with the Code size gap decreasing by ten bytes.

- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader](loader.txt)
- [Focused execution](probe.txt)
- [Focused command](check-case-frame.ps1)
- [gl instruction comparison](gl-init.txt)
- [gl slot permutation comparison](gl-slot-permutation.txt)
