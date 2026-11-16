# Part 1127 — keep only live arguments across discarded calls

The nested-call planner now also handles straight-line sequences of ordinary calls with discarded intermediate results. It saves arguments used after the first call, reloads them where needed and trims the frame before calls that no longer need a stack prefix.

The probe returned the same result and propagated the same error on both OTP images. The generated frame and trim sizes match OTP. OTP assigns the two initial slots in the opposite order and relocates the live slot before trimming.

- [Probe source](beam_nested_entry_calls_probe.erl)
- [Probe assertions](run.eval)
- [Probe disassembly](disassembly.txt)

The full run produced 348 Equal, 1,015 Different and three SourceRejected. ELDAPv3 remains Equal. OTP prepared all differing images for loading. Among 38 changed images, 14 Code size gaps narrowed and seven widened. Comparing function disassembly with label definitions excluded found 28 newly matching functions and one lost match. The lost match is a reversed Y-slot assignment in a two-call diameter_dict_util closure.

- [Changed images](changed-images.txt)
- [Function comparison](function-comparison.txt)
- [Comparison method](compare-functions.eval)
- [Lost match disassembly in reference, previous, current order](diameter-make-dict.txt)
- [Focused tests](focused.json)
- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [Loader results](loader.txt)
