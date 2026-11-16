# Part 1288 — Pass a case result as the next call's second argument

For consecutive case and call bindings, pass the case result from X0 to the second argument register when the result has no later reads. Keep the first argument live through the case and preserve the next call's result binding.

The focused probe exercises both branches. gl:init_nif/0 drops the intermediate stack store and reload before filename:join/2. The frame remains two slots. Slot assignment and argument preparation still differ from OTP.

The full corpus generated all 1,366 images with 359 Equal, 1,007 Different and zero source rejections. ELDAPv3 remains Equal. OTP accepted all differing pairs. Only gl changed. Its Code size gap increased by three bytes while the redundant stack store and reload were removed.

- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader](loader.txt)
- [Focused execution](probe.txt)
- [Focused command](check-case-register.ps1)
- [gl instruction comparison](gl-init.txt)
