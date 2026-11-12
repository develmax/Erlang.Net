# Part 1272 — Keep flat receive messages in X0

For receive clauses with flat tuple patterns and no guards, match directly against the message in X0. Start allocating pattern scratch registers at X1. Clauses with guards or nested patterns keep the existing copy.

The projection probe confirms X0 extraction and a zero-slot frame. The guarded tail receive probe also passes. In gl:rec/1 the message copy is gone and the frame is six slots, down from seven in part 1270. OTP uses one slot. Across both increments the gl Code gap fell from 62 to 58 bytes. The number of normalized function matches remains 1,066 of 1,068.

The full corpus generated all 1,366 images with 359 Equal, 1,007 Different and zero rejected sources. ELDAPv3 remains Equal. The OTP loader accepted all differing pairs. The absolute Code size gap decreased in all 179 changed images, by 1,452 bytes in aggregate.

- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader](loader.txt)
- [Projection check](probe.txt)
- [Projection command](check-receive-projection.ps1)
- [Guarded receive check](guarded-probe.txt)
- [gl instruction comparison](gl-receive.txt)
- [gl section comparison](gl-chunks.txt)
