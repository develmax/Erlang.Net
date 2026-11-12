# Part 1336

Plan tail calls in an atom case with a call selector and at least one dynamic branch. Fold a preceding atom binding into the call operands, retain only inputs needed by the branches, and preserve the case error location. Allow function_exported/3 as an ordinary external selector.

The focused test matches the complete dispatch instruction sequence after label normalization and exercises dynamic dispatch, the local fallback and case_clause. The release error_handler:call_undefined_function_handler/3 now matches all instructions after label normalization. Its module Code size gap fell from 33 to 18 bytes. ensure_loaded/1 remains the one differing instruction sequence in that module.

The full Windows corpus generated all 1,366 images with 363 Equal, 1,003 Different and zero SourceRejected. Eleven images changed by hash. The Code size gap narrowed in nine by a combined 88 bytes. OTP accepted all differing images and ELDAPv3 remained Equal.

- [Focused check](check-tail-atom-call.ps1)
- [Focused output](probe.txt)
- [Release comparison](error-handler.txt)
- [Full corpus](otp-corpus.json)
- [Changed images](changed-images.txt)
- [Chunk changes](delta.json)
- [Instruction matches](normalized-functions.txt)
- [OTP loader](loader.txt)
- [Related functions in the prior corpus](atom-tail-ranking.txt)
