# Part 1160 — use tuple-head bindings in frameless list branches

The audit joined release abstract forms to BEAM disassembly for all 1,015 differing images. The largest group by clause signature had 97 modules with an unguarded list-variable clause ending in a call in a three-clause function. The category includes functions with more than one kind of entry pattern.

The constructor planner now consumes fields extracted from a tuple at the head of a list. It retains the list on failure and preserves the whole tuple when its alias is live. Both oracle images return the expected values. The literal test scratch register and the extracted field destination still differ in the probe.

The full run generated 1,363 images and returned 348 Equal, 1,015 Different and three SourceRejected. ELDAPv3 remains Equal. Twenty-four image hashes changed and 21 Code size gaps narrowed. The normalized comparison gained an instruction match in orddict:reverse_pairs/2 and lost none. OTP prepared all differing images for loading. All three focused tests passed.

A separate baseline chunk comparison found six modules with four differing chunks, the smallest count in the corpus. Two supervisor modules share the same chunk signature and have similar source. They provide a target for investigating whole-image convergence.

- [Source-clause audit](source-audit.txt)
- [Chunk census](chunk-signatures.txt)
- [Supervisor disassembly](httpd-misc-sup.txt)
- [Oracle probe](disassembly.txt)
- [Probe assertions](run.eval)
- [Full corpus](otp-corpus.json)
- [Image delta](delta.json)
- [Function comparison](function-comparison.txt)
- [Focused tests](focused.json)
- [Loader](loader.txt)
