# Part 1280 — Share the source line between nested calls

Use the line of a unary static remote call in a tagged final list element as the current location for the outer call. Tuple and list construction preserve that location. If the outer call is on the same source line, its line instruction is redundant.

The focused probe checks that the caller still reports line 4 in the stacktrace. gl:rec/1 now matches after label normalization. The only differing function is init_nif/0. The standalone Code section is eight bytes longer than OTP. Attr, Docs and Line also differ.

The full corpus generated all 1,366 images with 359 Equal, 1,007 Different and zero source rejections. ELDAPv3 remains Equal. OTP accepted all differing pairs. Only gl changed and its Code gap decreased by three bytes.

- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader](loader.txt)
- [Focused execution](probe.txt)
- [Focused command](check-call-line.ps1)
- [gl instruction comparison](gl-receive.txt)
- [gl section comparison](gl-chunks.txt)
