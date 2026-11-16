# Part 1199 — retain only arguments used after a case selector call

The general emitter prepares an ordinary entry call in a case selector directly from the incoming X registers. It preserves parameters in the stack frame when a branch can read them after the call. It uses the existing capture analysis for branch patterns, guards and bodies, and the existing case emitter for the prepared result.

The first allocation size moved closer to OTP in all 1,261 functions where it changed. The Code chunk size moved closer in 531 modules. The full run generated 1,363 images. It recorded 357 Equal, 1,006 Different and 3 SourceRejected. ELDAPv3 remained Equal. OTP accepted all differing images for loading and all three focused tests passed.

- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [Frame delta](frame.txt)
- [Frame comparison script](frame.eval)
- [Changed images](changed-images.txt)
- [Focused tests](focused.json)
- [Loader](loader.txt)
- [Opcode ranking](ranking.txt)
