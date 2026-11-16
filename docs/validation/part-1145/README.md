# Part 1145 — handle terminal tagged branches in registers

The register case path checks tag and arity in X0 and extracts the matched field when the branch uses it. This path supports a saved input or the unchanged selector in the fallback. Specialized tagged paths run first and retain their type annotations and direct argument destinations.

The tagged_result/2 probe matches the OTP 29.1.1 instructions after local label normalization. Both images exercised matching tag and arity, wrong arity and wrong tag. The preliminary run lost three matches because the new path preceded specialized emitters. After correcting precedence, the final run gained 25 instruction matches and lost none.

- [Probe source](beam_general_catch_probe.erl)
- [Oracle assertions](run.eval)
- [Probe disassembly](disassembly.txt)
- [Preliminary comparison](preliminary-function-comparison.txt)
- [Final function comparison](function-comparison.txt)

The full run generated 1,363 images. Results were 348 Equal, 1,015 Different and three SourceRejected. ELDAPv3 remains Equal. OTP prepared all differing images for loading. The Code gap narrowed in all 16 changed images.

- [Changed images](changed-images.txt)
- [Focused tests](focused.json)
- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [Loader results](loader.txt)
