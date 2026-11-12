# Part 1371 — Dispatch literal clauses in entry registers

The separate clause planner checks literal patterns in the incoming X registers. Branches can return values or tail-call without a frame, while a branch with a nested call can allocate its own frame. Distinct keys on one argument use a sorted select_val table with clause labels in source order.

The register constructor planner counts remaining reads of each input. It releases the input register after its last read and can reuse it for a tuple or list. It retains an input needed by another argument until the call moves are complete. Control-transfer calls can construct their arguments under one heap check and use call_ext_only.

The focused oracle checks direct tail calls, an overlapping constructor input, nested-call side effects, exit and throw terms, and a select_val with no fallback. Four functions drop from two stack slots to zero and match OTP's frame sizes. Dispatch, conditional and test_last match the decoded OTP instructions after label normalization. Ten earlier probes, two compiler tests and the direct whole-module test passed.

The Windows OTP 29.1.1 corpus generated all 1,366 images with 369 Equal, 997 Different and zero SourceRejected. ELDAPv3 remains Equal. OTP accepted all differing images for loading. Of 264 changed images, 263 narrowed the Code size gap by 24,713 bytes in total. The remaining changed image has the same Code size gap. There are 141 gained normalized function matches and no lost matches. All 642 functions with a smaller peak frame reach the OTP frame size.

AlgorithmInformation-2009:decode_disp/2 and encode_disp/2 now match the normalized release instructions. Both lose a two-slot frame and replace three heap checks with one test_heap for nine words. The frame audit now counts the first exact-equality versus allocation difference in 591 modules with multi-clause functions, down from 622. The nonempty-list group is larger at 613 modules and 3,954 functions.

The full runtime suite has not run on this build. Canonical readiness remains pending.

- [Full corpus](otp-corpus.json)
- [Code size changes](code-delta.json)
- [Normalized function comparison](normalized-functions.txt)
- [Peak frame comparison](peak-slots.txt)
- [OTP loader check](loader.txt)
- [Frame audit](frame-shapes.txt)
- [List clause instructions](list-clause-entry.txt)
- [Fixture source](clause_entry_probe.erl)
- [Focused check](check-clause-entry.ps1)
- [Focused output](probe.txt)
- [Fixture disassembly](instructions.txt)
- [Release disassembly](release-functions.txt)
- [Compiler tests](focused.json)
- [Whole-module test](whole-module.txt)
