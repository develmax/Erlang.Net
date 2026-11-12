# Part 1135 — keep caught literal selectors in X0

The caught dynamic call path compares a single literal pattern directly against X0 and emits terminal returns for a matching value and a fallback. Supported result operands are constants, saved arguments and the unchanged selector in the fallback.

The select/4 probe matches all OTP 29.1.1 instructions after local label normalization. Both images executed the normal, throw, exit and error cases. Released images did not change in this iteration. The asn1ct test_value closure still uses the general branch emitter.

- [Probe source](beam_general_catch_probe.erl)
- [Oracle assertions](run.eval)
- [Probe disassembly](disassembly.txt)
- [Image delta](delta.json)

The full run generated 1,363 images. Results were 348 Equal, 1,015 Different and three SourceRejected. ELDAPv3 remains Equal. OTP prepared all differing images for loading.

The oversized-frame group was classified by the reference instruction after argument saves. An external call was the next instruction in 584 modules. Among these, 379 modules have a function where the call is followed by a test after skipping register moves and stack cleanup. The counts overlap across categories.

- [Frame categories](frame-next-op.txt)
- [Categories after external calls](frame-after-external-call.txt)
- [Tests after external calls](frame-test-after-external-call.txt)
- [Focused tests](focused.json)
- [Full corpus](otp-corpus.json)
- [Loader results](loader.txt)
