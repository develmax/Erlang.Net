# Part 1285 — Place the case failure after the external tail call

Defer a boolean case failure when its selected result is passed to a terminal external call. Emit the failure block after the successful tail and attach the selector call location to case_end.

The focused probe checks both successful branches and the case_clause stacktrace. The complete Line section in gl.beam now matches OTP. Only Code and Attr differ.

The full corpus generated all 1,366 images with 359 Equal, 1,007 Different and zero source rejections. ELDAPv3 remains Equal. OTP accepted all differing pairs. The change affected gl and typer_core. The gl Code gap decreased by two bytes and the typer_core Code length stayed the same.

- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader](loader.txt)
- [Focused execution](probe.txt)
- [Focused command](check-case-failure.ps1)
- [gl instruction comparison](gl-init.txt)
- [gl section comparison](gl-chunks.txt)
