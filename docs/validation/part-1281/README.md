# Part 1281 — Preserve the line of direct nested calls

For a direct static remote argument, propagate its call site to the caller's line emission. This skips a duplicate line only when the nested call and outer call share a source coordinate.

The focused probe checks both same-line calls and calls on separate lines. The latter keep the outer location in the error stacktrace. Both filename:absname/1 branches in gl:init_nif/0 lose a redundant line. The frame and control flow still differ.

The full corpus generated all 1,366 images with 359 Equal, 1,007 Different and zero source rejections. ELDAPv3 remains Equal. OTP accepted all differing pairs. All 434 changed images moved closer in Code size, by 5,484 bytes in aggregate.

- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader](loader.txt)
- [Focused execution](probe.txt)
- [Focused command](check-call-line.ps1)
- [gl instruction comparison](gl-init.txt)
