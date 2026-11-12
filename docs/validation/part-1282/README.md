# Part 1282 — Pass the final case result directly to a call

Keep a fresh result in X0 when the last case binding is followed by a static external call that consumes it as its first argument and all other arguments are constants. Preserve the ordinary binding path if the case itself can bind the same variable.

The focused probe checks both branches and the constant second argument. In gl:init_nif/0 the selected filename reaches erlang:load_nif/2 without a stack store or reload. The frame falls from four slots to three, compared with two in OTP.

The full corpus generated all 1,366 images with 359 Equal, 1,007 Different and zero source rejections. ELDAPv3 remains Equal. OTP accepted all differing pairs. The seven changed images all moved closer in Code size, by 24 bytes in aggregate.

- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader](loader.txt)
- [Focused execution](probe.txt)
- [Focused command](check-case-result.ps1)
- [gl instruction comparison](gl-init.txt)
