# Part 1142 — forward a nested call result into tuple matching

The entry tuple match path accepts a lookup with one ordinary nested call argument. It forwards the nested result in X registers and saves only inputs used after the first call. A dead frame prefix is trimmed before the lookup and later stack operands are remapped. Other expired slots are cleared.

The nested/2 and nested_dead/2 probes match OTP 29.1.1 after local label normalization. They cover a live key, a key that is dead after argument preparation and a badmatch retaining false as its original value.

- [Probe source](beam_entry_tuple_probe.erl)
- [Oracle assertions](run.eval)
- [Probe disassembly](disassembly.txt)

The full run generated 1,363 images. Results were 348 Equal, 1,015 Different and three SourceRejected. ELDAPv3 remains Equal. OTP prepared all differing images for loading. The Code size gap narrowed in all three changed images. No released function gained or lost a normalized instruction match.

- [Changed images](changed-images.txt)
- [Function comparison](function-comparison.txt)
- [Focused tests](focused.json)
- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [Loader results](loader.txt)
