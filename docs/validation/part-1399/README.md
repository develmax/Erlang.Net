# Part 1399 — Emit dynamic tail calls after a bound call result

The entry emitter keeps the preceding call result in X0. For dynamic calls with a fixed argument count it puts arguments, module and function in the apply registers and emits apply_last. For erlang:apply/3 with a variable argument list it emits a tail call to the BIF. An explicit call to another module retains its original target.

The deterministic regression image matches OTP byte for byte. The OTP oracle checks six functions, including variable module and function operands, explicit and auto-imported apply/3, invalid modules and a same-named foreign call. The selected compiler tests pass.

The complete Windows OTP 29.1.1 corpus generates 1,366 images. It reports 372 Equal and 994 Different with zero SourceRejected and zero Unsupported. ELDAPv3 and the three previously rejected Observer modules remain Equal. The initial run used a header tree that lacked seven referenced Markdown files. The complete run uses the pinned source overlay with those documents.

Three release images change. Four functions gain normalized instruction matches with none lost. The Code gap narrows by 46 bytes across the three images. OTP accepts all 994 saved differing images at loader preparation.

- [Corpus](otp-corpus.json)
- [Normalized instruction changes](normalized-functions.txt)
- [Release instructions before](operands-before.txt)
- [Release instructions after](operands-after.txt)
- [Loader and Code sizes](loader-and-code.txt)
- [Oracle](check-dynamic-apply-tail.ps1) and [result](probe.txt)
- [Focused tests](focused.json) and [whole-module test](whole-module.json)
