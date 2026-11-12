# Part 1190 — keep a wildcard select_val selector in X0

The case emitter dispatches from X0 without saving the selector when all explicit branches match scalar literals and the final branch is an unguarded wildcard. Named fallbacks and tuple extraction retain the existing path.

The runtime probe passed on OTP and both C# builds. It covers two literal branches, a wildcard fallback and a named fallback that reads the selector. The generated wildcard frame shrank from three slots to two. OTP uses one slot.

The full Windows OTP 29.1.1 corpus generated 1,363 of 1,366 images. Results were 356 Equal, 1,007 Different and 3 SourceRejected. The Code length gap narrowed in all 65 changed images. ELDAPv3 remained Equal. OTP prepared all differing images for loading. Three focused tests passed.

- [Full corpus](otp-corpus.json)
- [Delta](delta.json)
- [Focused tests](focused.json)
- [Loader](loader.txt)
- [Probe source](case_select_register_probe.erl)
- [Probe assertions](run.eval)
- [Disassembly](disassembly.txt)
