# Part 1140 — forward matched tuple fields to tail calls

The entry tuple match path can forward several fields to an ordinary tail call. It leaves the matched value in X0 through the failure checks and schedules extraction before preparing the final arguments. Incoming arguments are saved only when checked or used by the tail call.

The forward/3 and forward_pair/2 probes match OTP 29.1.1 after local label normalization. They cover an original argument retained across the lookup, register permutation and a badmatch with the original tuple.

- [Probe source](beam_entry_tuple_probe.erl)
- [Oracle assertions](run.eval)
- [Probe disassembly](disassembly.txt)

The full run generated 1,363 images. Results were 348 Equal, 1,015 Different and three SourceRejected. ELDAPv3 remains Equal. OTP prepared all differing images for loading. The Code size gap narrowed in all 56 changed images.

- [Changed images](changed-images.txt)
- [Function comparison](function-comparison.txt)
- [Focused tests](focused.json)
- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [Loader results](loader.txt)
