# Part 1273 — Extract the final receive field into X0

In a terminal, unguarded flat tuple pattern, extract the last field directly into X0 when the clause returns that fresh variable. Earlier tests finish before the message register is overwritten. A repeated or previously bound variable still needs the ordinary equality check.

The focused probe confirms direct extraction and checks that a repeated-variable mismatch leaves the message in the mailbox.

The full corpus generated all 1,366 images with 359 Equal, 1,007 Different and zero source rejections. ELDAPv3 remains Equal. OTP accepted all differing pairs. The Code size gap decreased in all 11 changed images, by 42 bytes in aggregate.

- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader](loader.txt)
- [Focused execution](probe.txt)
- [Focused command](check-receive-projection.ps1)
- [gl instruction comparison](gl-receive.txt)
