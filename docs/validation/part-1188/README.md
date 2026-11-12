# Part 1188 — forward a fallback call result in X0

The entry case emitter reuses forwarded tail-call lowering when the fallback binds a call result and immediately passes it onward. In asn1ct:add_generated_refed_func/1 the result stays in X0 as the tail of a list built directly in X1. The function now uses one stack slot, as in the OTP release.

The runtime probe passed on both OTP and the generated image. It checks the literal branch and list construction with proper and improper tails. The probe images differ byte for byte.

The full Windows OTP 29.1.1 corpus generated 1,363 of 1,366 images. Results were 356 Equal, 1,007 Different and 3 SourceRejected. The asn1ct image changed and its Code length gap narrowed. ELDAPv3 remained Equal. OTP prepared all differing images for loading. Three focused tests passed.

- [Full corpus](otp-corpus.json)
- [Delta](delta.json)
- [Focused tests](focused.json)
- [Loader](loader.txt)
- [Release disassembly](release-disassembly.txt)
- [Probe source](entry_case_cons_tail_probe.erl)
- [Probe assertions](run.eval)
