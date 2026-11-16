# Part 1130 — forward the result of a tagged case in registers

The entry emitter keeps a call result in X0 while testing a tagged tuple. On a match it extracts the field into its argument register and restores the inputs needed by the tail call. On a mismatch it returns the original call result.

The retained/2 probe matches the OTP 29.1.1 instruction sequence. Both images produced the expected results for a matching tuple, a scalar and a tuple of the wrong arity.

- [Probe source](beam_entry_tagged_projection_probe.erl)
- [Probe assertions](run.eval)
- [Probe disassembly](disassembly.txt)

The full run generated 1,363 images. Results were 348 Equal, 1,015 Different and three SourceRejected. ELDAPv3 remains Equal. OTP prepared all differing images for loading. The Code size gap narrowed in all 16 changed images. Fourteen released functions gained instruction matches with local labels normalized and none lost a match.

- [Changed images](changed-images.txt)
- [Function comparison](function-comparison.txt)
- [Comparison method](compare-functions.eval)
- [Focused tests](focused.json)
- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [Loader results](loader.txt)
