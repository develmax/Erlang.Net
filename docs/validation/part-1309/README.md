# Part 1309 — Dispatch nonempty and empty list tail clauses in registers

For a three-clause function with a nonempty-list head, an empty-list clause and an unconditional fallback, emit the branch tests on the original argument. Once the cons test succeeds, reuse its X register for the tail. When the empty and fallback branches return true and false, emit exact equality against nil and return the boolean directly.

The focused probe compares complete instruction sequences with OTP 29.1.1 and executes both versions on proper lists, improper lists, the empty list and non-lists. The release function is_set/1 in ordsets matches including label numbering. The release is_proper_list/1 in cerl matches after label normalization. No dependency or license changes.

- [Probe command](check-list-tail-dispatch.ps1)
- [Probe instructions](probe.txt)
- [ordsets release function](ordsets.txt)
- [cerl release function](cerl.txt)
- [Matching source forms in the previous corpus](release-shapes.txt)

Both complete corpus runs generated all 1,366 release files with 361 Equal and zero rejected. The final run includes the nil equality fold. Only cerl and ordsets changed against part 1308, with Code size gaps reduced by 28 and 29 bytes respectively. OTP accepted all 1,005 differing images. Full runtime tests, readiness regeneration and CI were outside this check.

- [Final corpus](otp-corpus.json)
- [Final delta](delta.json)
- [OTP loader](loader.txt)
- [Remaining local tail argument shapes](local-tail-arguments.txt)
