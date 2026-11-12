# Part 1144 — prepare nested arguments for register cases

The literal and unary guard register case path reuses entry selector planning. It forwards the result of one nested call in X registers and saves only values used after that call. The case retains its frame shape and clears slots that expire before the outer call.

The status_nested/3 probe matches OTP 29.1.1 after local label normalization and exercises both outcomes. OTP clears the key slot with init_yregs before testing the external call result. A preliminary version used trim there. The final version matches the slot clearing and frame size.

- [Probe source](beam_general_catch_probe.erl)
- [Oracle assertions](run.eval)
- [Probe disassembly](disassembly.txt)

The full run generated 1,363 images. Results were 348 Equal, 1,015 Different and three SourceRejected. ELDAPv3 remains Equal. OTP prepared all differing images for loading. Three Code gaps narrowed and shell write_and_compile_module/2 gained an instruction match.

- [Changed images](changed-images.txt)
- [Function comparison](function-comparison.txt)
- [Focused tests](focused.json)
- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [Loader results](loader.txt)
