# Part 1159 — frameless constructors with a fallback clause

The disassembly census found 58 modules with a nonempty-list entry test, 50 with an untagged tuple test and 37 with a tagged tuple test that share a failure destination and contain a frameless constructor and tail call in OTP. The groups overlap and include clauses outside the two-clause source shape implemented here.

The compiler reuses the register constructor planner for a matching first clause and an unconditional second clause that returns a value or makes a simple tail call. Shape and literal checks leave the original input registers intact until the first clause is selected. Both probes match OTP instructions after local label normalization and return the expected terms.

The full run generated 1,363 images and returned 348 Equal, 1,015 Different and three SourceRejected. ELDAPv3 remains Equal. Two Code size gaps narrowed. Sixty-five functions changed after label normalization, and no instruction matches were gained or lost. OTP prepared all differing images for loading and all three focused tests passed.

- [Fallback audit](fallback-audit.txt)
- [Audit program](fallback-audit.eval)
- [Oracle disassembly](disassembly.txt)
- [Probe assertions](run.eval)
- [Full corpus](otp-corpus.json)
- [Image delta](delta.json)
- [Function comparison](function-comparison.txt)
- [Focused tests](focused.json)
- [Loader](loader.txt)
