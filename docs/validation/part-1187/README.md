# Part 1187 — test a case call result in X0

A single-clause function can test a call result in X0 when its case has one literal branch returning a constant and a wildcard fallback. Save only inputs referenced by the fallback before the call. The fallback uses existing tail-call lowering.

The release example asn1ct:add_generated_refed_func/1 now tests X0 and uses two stack slots instead of three. OTP uses one slot and puts the second call result directly into the list constructor. Both OTP and the generated probe passed the runtime assertions for the literal and fallback branches.

The full Windows OTP 29.1.1 corpus generated 1,363 of 1,366 images. Results were 356 Equal, 1,007 Different and 3 SourceRejected. The Code length gap narrowed in all nine changed images. ELDAPv3 remained Equal. OTP prepared all differing images for loading. Three focused tests passed.

- [Full corpus](otp-corpus.json)
- [Delta](delta.json)
- [Focused tests](focused.json)
- [Loader](loader.txt)
- [Release disassembly](release-disassembly.txt)
- [Probe source](entry_case_fallback_probe.erl)
- [Probe assertions](run.eval)
