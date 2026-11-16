# Part 1141 — rebuild a tuple from matched call fields

The entry emitter checks the returned tuple in X0, reserves heap space before extracting fields and builds the output tuple from registers, saved arguments and literal operands. The original selector remains available on all badmatch edges.

The rebuild/3 probe matches the OTP 29.1.1 instructions after local label normalization. Both images returned the expected reordered tuple and preserved the unmatched tuple in badmatch.

- [Probe source](beam_entry_tuple_probe.erl)
- [Oracle assertions](run.eval)
- [Probe disassembly](disassembly.txt)

The full run generated 1,363 images. Results were 348 Equal, 1,015 Different and three SourceRejected. ELDAPv3 remains Equal. OTP prepared all differing images for loading. The Code gap narrowed in all eight changed images. Two release functions gained instruction matches after local label normalization.

- [Changed images](changed-images.txt)
- [Function comparison](function-comparison.txt)
- [Focused tests](focused.json)
- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [Loader results](loader.txt)
