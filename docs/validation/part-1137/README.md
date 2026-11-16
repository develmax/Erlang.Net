# Part 1137 — type tuple tests on lookup results

The register case emitter now annotates tuple tests on lists:keyfind/3, maps:find/2 and maps:take/2 with the atom-or-tuple result type. The status/3 and lookup/3 probes match the OTP 29.1.1 instructions after local label normalization. Both images returned the expected matching and fallback results.

- [Probe source](beam_general_catch_probe.erl)
- [Oracle assertions](run.eval)
- [Probe disassembly](disassembly.txt)
- [Typed external results in release files](typed-external-results.txt)
- [Diagnostic method](typed-external-results.eval)

The full run generated 1,363 images. Results were 348 Equal, 1,015 Different and three SourceRejected. ELDAPv3 remains Equal. OTP prepared all differing images for loading. No release image changed. Released lookup functions also use other shapes, including a tuple match on a call result and a case with a nested argument call.

- [Focused tests](focused.json)
- [Full corpus](otp-corpus.json)
- [Image delta](delta.json)
- [Loader results](loader.txt)
