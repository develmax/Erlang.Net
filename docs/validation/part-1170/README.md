# Part 1170 — match the ttb_autostart release image

Nested term_to_binary/1 uses the existing entry call chain and zero-slot frame. Literal entry tests precede constant returns without a stack frame. The joined case tests the result in X0, runs the effect only on its matching branch and shares the remaining calls.

The nested call and literal probes match normalized OTP instructions. The joined-case probe returns the expected term. OTP folds that probe's selector because its local callee always returns the same atom. The Windows release ttb_autostart image exercises the nonconstant selector and matches byte for byte.

The full run generated 1,363 images and returned 352 Equal, 1,011 Different and three SourceRejected. ELDAPv3 remains Equal. Seven still-different images changed and their Code size gaps narrowed. Twenty functions gained instruction matches and none lost one. OTP prepared all differing images for loading. Three focused tests passed.

- [Full corpus](otp-corpus.json)
- [Image delta](delta.json)
- [Function comparison](function-comparison.txt)
- [Focused tests](focused.json)
- [Oracle probe](disassembly.txt)
- [Probe assertions](run.eval)
- [Remaining chunk signatures](chunk-signatures.txt)
- [Loader](loader.txt)
