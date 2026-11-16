# Part 1289 — Emit tagged case literals in the argument register

For a two-arm tagged tuple case passed as the second argument, emit the literal branch directly into X1. A fallback call still returns in X0 and moves its result to X1 on that branch only.

The probe checks both constant arms. In gl:init_nif/0 the win32 branch now writes X1 and the system_info branch moves its call result before the join label, as in OTP.

The full corpus generated all 1,366 images with 359 Equal, 1,007 Different and zero source rejections. ELDAPv3 remains Equal. OTP accepted all differing pairs. Only gl changed, with the same Code length.

- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader](loader.txt)
- [Focused execution](probe.txt)
- [Focused command](check-case-register.ps1)
- [gl instruction comparison](gl-init.txt)
