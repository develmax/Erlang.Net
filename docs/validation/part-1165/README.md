# Part 1165 — reuse tuple slots across a conditional tail call

The emitter reuses two parameter slots for the first call result and the child identifier tuple. It initializes expired slots before the second call, tests its result in X0 and returns that result directly on mismatch. The matched branch reloads the two surviving values and makes a tail call.

The probe matches OTP instructions after local label normalization. Both success and error results agree with OTP. The source census found the same shape in httpd_misc_sup:stop_permanent_worker/4.

The full run generated 1,363 images and returned 349 Equal, 1,014 Different and three SourceRejected. The only status change was httpd_misc_sup.beam, which now matches the Windows release byte for byte. The other differing image hashes remained identical. ELDAPv3 remains Equal. OTP prepared all differing images for loading and all three focused tests passed.

- [Full corpus](otp-corpus.json)
- [Image delta](delta.json)
- [Source audit](case-audit.txt)
- [Probe disassembly](disassembly.txt)
- [Probe assertions](run.eval)
- [Focused tests](focused.json)
- [Loader](loader.txt)
- [Remaining chunk signatures](chunk-signatures.txt)
