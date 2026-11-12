# Part 1189 — compare a case selector directly in X0

When a case tests a literal and falls back to an unguarded wildcard, the general emitter tests a selector already in X0 without allocating a Y slot for it. Both branches continue through the existing result and export handling.

The runtime probe passed on OTP and both C# builds. The generated frame shrank from three slots to two. OTP uses one slot and allocates it after the test.

The full Windows OTP 29.1.1 corpus generated 1,363 of 1,366 images. Results were 356 Equal, 1,007 Different and 3 SourceRejected. All 347 changed images moved closer in Code length. ELDAPv3 remained Equal. OTP prepared all differing images for loading. Three focused tests passed.

- [Full corpus](otp-corpus.json)
- [Delta](delta.json)
- [Focused tests](focused.json)
- [Loader](loader.txt)
- [Probe source](case_register_selector_probe.erl)
- [Probe assertions](run.eval)
- [Disassembly](disassembly.txt)
