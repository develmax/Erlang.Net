# Part 1138 — dispatch literal branches on a call result in X0

The register case emitter uses select_val for distinct atom branches or distinct integer branches of the supported range. The table is in ascending key order and the terminal branch bodies are emitted in descending order. Returns and tail calls share the existing register case result emitter.

The external-call pick/2 probe matches the OTP 29.1.1 instructions after local label normalization and exercises all four branches on OTP. The managed harness uses a local selector after the external proplists call raised NotSupportedException in the harness.

- [Probe source](beam_general_catch_probe.erl)
- [Oracle assertions](run.eval)
- [Probe disassembly](disassembly.txt)

The full run generated 1,363 images. Results were 348 Equal, 1,015 Different and three SourceRejected. ELDAPv3 remains Equal. OTP prepared all differing images for loading. Six images changed and their Code gaps narrowed. The erl_syntax revert_root/1 function gained an instruction match.

- [Changed images](changed-images.txt)
- [Function comparison](function-comparison.txt)
- [Focused tests](focused.json)
- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [Loader results](loader.txt)

The next tuple-match example preserves a single key and uses a typed X0 selector in OTP. The C# emitter currently allocates five slots. [Diameter keyfetch comparison](diameter-keyfetch.txt).
