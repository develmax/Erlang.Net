# Part 1143 — project tagged lookup results in X0

Tuple matches and tagged lookup cases share entry call preparation, including one nested argument call. For a lookup with a known missing atom, the case tests the atom-or-tuple result, checks tuple tag and arity and projects the selected field. An invalid tuple is still the operand of case_end.

The lookup_tag/1 probe matches the OTP 29.1.1 instruction sequence after local label normalization. Both images exercised a tuple match, a missing key and the case_clause error for an invalid tuple. The release logger encoding/0 frame shrank from three slots to zero and the normalized instructions match.

- [Probe source](beam_entry_tuple_probe.erl)
- [Oracle assertions](run.eval)
- [Probe disassembly](disassembly.txt)
- [Release comparison](logger-encoding.txt)

The full run generated 1,363 images. Results were 348 Equal, 1,015 Different and three SourceRejected. ELDAPv3 remains Equal. OTP prepared all differing images for loading. Four Code gaps narrowed and five release functions gained instruction matches.

- [Changed images](changed-images.txt)
- [Function comparison](function-comparison.txt)
- [Focused tests](focused.json)
- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [Loader results](loader.txt)
