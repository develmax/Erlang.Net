# Part 1139 — match returned tuples directly in X0

The entry emitter matches a call result in X0 and saves only input variables that are compared with tuple fields. It checks the tuple shape, preserves the original value for badmatch and extracts the returned field into X0 after all checks pass. The lookup type annotation is shared with the register case path.

The keyfetch/2 probe matches OTP 29.1.1 after local label normalization. Both images tested a successful match, a missing key, a tuple of the wrong arity and a key that compares equal only under numeric coercion. The release diameter_info keyfetch/2 frame shrank from five slots to one and its normalized instructions match.

- [Probe source](beam_entry_tuple_probe.erl)
- [Oracle assertions](run.eval)
- [Probe disassembly](disassembly.txt)
- [Release comparison](diameter-keyfetch.txt)

The full run generated 1,363 images. Results were 348 Equal, 1,015 Different and three SourceRejected. ELDAPv3 remains Equal. OTP prepared all differing images for loading. The Code gap narrowed in all 44 changed images.

- [Changed images](changed-images.txt)
- [Function comparison](function-comparison.txt)
- [Focused tests](focused.json)
- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [Loader results](loader.txt)
