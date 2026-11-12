# Part 1133 — check the tuple tag without extracting unused fields

The tagged-selector path accepts distinct field variables whose values are unused by the tail call. It preserves tag and arity checking and extracts at most one field that the tail call actually uses.

The tag_only/2 probe matches the OTP 29.1.1 instructions and execution. The asn1ct test_type closure now matches after local label normalization. Four other release functions gained instruction matches. The Code gap narrowed in all four changed images.

- [Probe source](beam_entry_tagged_projection_probe.erl)
- [Probe assertions](run.eval)
- [Probe disassembly](disassembly.txt)
- [Release comparison](asn1ct-test-type.txt)
- [Function comparison](function-comparison.txt)

The full run generated 1,363 images. Results were 348 Equal, 1,015 Different and three SourceRejected. ELDAPv3 remains Equal. OTP prepared all differing images for loading.

- [Changed images](changed-images.txt)
- [Focused tests](focused.json)
- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [Loader results](loader.txt)
