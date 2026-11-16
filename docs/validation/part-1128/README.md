# Part 1128 — align Y-slot ordering for capture-free closures

When saved closure parameters have the same last use in a discarded call sequence, the emitter orders them by descending binding name. The OTP pre-codegen probe shows that a plain fun can number its parameters differently from a named function before the Y-register reversal. The ordinary-function order and capture-bearing closures retain their existing paths.

The named closure probe matches the OTP instructions apart from label definitions and propagates the first call error in both images. The release diameter_dict_util closure recovered the instruction match lost in part 1127. Only that image changed.

- [Probe source](beam_nested_entry_calls_probe.erl)
- [Probe assertions](run.eval)
- [Probe disassembly](disassembly.txt)
- [Release comparison](diameter-make-dict.txt)
- [Function comparison](function-comparison.txt)

The full run generated 1,363 images. Results were 348 Equal, 1,015 Different and three SourceRejected. ELDAPv3 remains Equal. OTP prepared all differing images for loading.

- [Focused tests](focused.json)
- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [Loader results](loader.txt)
