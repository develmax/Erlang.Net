# Part 1191 — keep tagged selectors in registers

The tuple case emitter retains an incoming two-element tuple in X0 and extracts its tag into X1 when the fallback discards the selector. A selected branch can extract the payload directly from the tuple register.

The probe passed on OTP and both C# builds. It covers payload binding and fallback on an unknown tag, wrong type and wrong arity. The generated frame shrank from four slots to three. OTP uses one slot and extracts the payload before dispatch.

The Windows OTP 29.1.1 corpus generated 1,363 of 1,366 images. Results were 356 Equal, 1,007 Different and 3 SourceRejected. The Code length gap narrowed in all six changed images. ELDAPv3 remained Equal. OTP prepared all differing images for loading. Three focused tests passed.

- [Full corpus](otp-corpus.json)
- [Delta](delta.json)
- [Focused tests](focused.json)
- [Loader](loader.txt)
- [Probe source](case_tuple_register_probe.erl)
- [Probe assertions](run.eval)
- [Disassembly](disassembly.txt)
