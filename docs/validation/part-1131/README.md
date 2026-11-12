# Part 1131 — preserve the selector register across preceding calls

The tagged-case path handles preceding ordinary calls and passes unary nested call results directly in X0. It saves only inputs used by later calls or the matching branch. Slots whose final read prepares a call are cleared before that call.

The staged/2 probe matches the OTP 29.1.1 instruction sequence and verifies the preceding side effect, the matching branch and the unchanged fallback value. In the release asn1ct test_module closure, the frame shrank from five slots to two. OTP still clears one slot between the two argument moves and attaches a tuple type to the selector test.

- [Probe source](beam_entry_tagged_projection_probe.erl)
- [Probe assertions](run.eval)
- [Probe disassembly](disassembly.txt)
- [Release comparison](asn1ct-test-module.txt)

The full run generated 1,363 images. Results were 348 Equal, 1,015 Different and three SourceRejected. ELDAPv3 remains Equal. OTP prepared all differing images for loading. The Code gap narrowed in the single changed asn1ct image.

- [Changed images](changed-images.txt)
- [Function comparison](function-comparison.txt)
- [Focused tests](focused.json)
- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [Loader results](loader.txt)
