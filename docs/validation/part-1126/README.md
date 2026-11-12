# Part 1126 — construct singleton lists from nested call results

The nested-call planner can construct a singleton list directly from the result in X0 before it prepares the remaining arguments. Only inputs used after an inner call occupy stack slots.

The generated and reference boxed/3 instructions match. Both images executed successfully on OTP 29.1.1, including propagation of an inner call error. In the released asn1ct pretty-printer closure the frame shrank from four slots to one. The instructions now match apart from numeric labels.

- [Probe source](beam_nested_entry_calls_probe.erl)
- [Oracle assertions](run.eval)
- [Probe disassembly](disassembly.txt)
- [Release comparison in reference, previous, current order](asn1ct-pretty.txt)

The full corpus generated 1,363 images. Results were 348 Equal, 1,015 Different and three SourceRejected. ELDAPv3 remains Equal. The Code gap narrowed in ten modules and OTP prepared all differing images for loading.

- [Changed images](changed-images.txt)
- [Focused tests](focused.json)
- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [Loader results](loader.txt)
