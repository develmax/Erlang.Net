# Part 1129 — relocate live Y slots before trimming the frame

Discarded-call chains assign their initial stack slots independently of last-use order. Before a nonterminal call, the emitter relocates a live value from the trimmed prefix to the highest available dead slot. Subsequent loads use the remapped slot. The nested-expression path retains its existing placement.

The discarded/3 probe now matches the OTP 29.1.1 instructions, including the Y-to-Y move and trim. Both probe images returned the same result and propagated the intermediate call error.

- [Probe source](beam_nested_entry_calls_probe.erl)
- [Probe assertions](run.eval)
- [Probe disassembly](disassembly.txt)

The full run generated 1,363 images. Results were 348 Equal, 1,015 Different and three SourceRejected. ELDAPv3 remains Equal. OTP prepared all differing images for loading. Five images changed. Four functions gained instruction matches with label definitions omitted and none lost a match. The absolute Code size gap widened in all five images.

- [Changed images](changed-images.txt)
- [Function comparison](function-comparison.txt)
- [Focused tests](focused.json)
- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [Loader results](loader.txt)
