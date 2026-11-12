# Part 1134 — prepare caught dynamic calls from entry registers

A dynamic remote call used as a caught case selector now takes its simple operands from the entry X registers. Only arguments referenced by the case branches are saved, in reverse register order. The catch instruction initializes its own frame slot.

The select/4 probe verifies ordinary results, errors, exits and throws. Its instruction sequence through catch_end matches OTP 29.1.1 apart from label numbers. The released asn1ct test_value closure matches the protected call prefix too. Its case branches still use the general emitter, which saves the selector and field values in Y slots.

- [Probe source](beam_general_catch_probe.erl)
- [Oracle assertions](run.eval)
- [Probe disassembly](disassembly.txt)
- [Release comparison](asn1ct-test-value.txt)

The full run generated 1,363 images. Results were 348 Equal, 1,015 Different and three SourceRejected. ELDAPv3 remains Equal. OTP prepared all differing images for loading. The Code gap narrowed in 14 changed images. The normalized function comparison reported no new or lost instruction matches.

- [Changed images](changed-images.txt)
- [Function comparison](function-comparison.txt)
- [Focused tests](focused.json)
- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [Loader results](loader.txt)
