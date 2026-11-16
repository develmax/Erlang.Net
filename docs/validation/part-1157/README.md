# Part 1157 — lower a single tuple clause without a frame

The disassembly census found 25 modules with an untagged tuple entry and 25 with a tagged tuple entry, a constructor and a frameless tail in the reference and an allocation in the generated function. The census describes a BEAM signature, and the groups can overlap.

A single tuple clause now reuses the existing field and tail constructor planner. The untagged probe matched normalized OTP instructions. The tagged probe preserved literal failure and exposed a different scratch register. Part 1158 resolves that difference.

The intermediate full run returned 348 Equal, 1,015 Different and three SourceRejected. Twelve Code size gaps narrowed and three functions gained instruction matches. OTP prepared all differing images for loading.

- [Entry audit](single-entry-audit.txt)
- [Probe disassembly](disassembly.txt)
- [Corpus](otp-corpus.json)
- [Delta](delta.json)
- [Function comparison](function-comparison.txt)
- [Loader](loader.txt)
