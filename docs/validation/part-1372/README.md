# Part 1372 — Check list clauses before allocating their frames

The entry planner handles list clauses with sequence and case bodies. A nonempty-list test runs in the incoming X register before the branch allocates its frame. A disjoint pair of empty and nonempty clauses can test the nonempty branch first. A private recursive function can omit the remaining nil test when every entry supplies a proper list and its recursive calls preserve the proper tail.

For an ordinary first call that consumes the head and preserves the tail, get_list can write the tail directly into a Y slot. A terminal ordinary call can use the first call's result in X0 without an intermediate stack save.

The focused oracle verifies proper and improper input, a repeated binding, retained arguments, and call order. follow/2 matches OTP's decoded instructions after label normalization. Nine earlier probes, two selected compiler tests and the direct whole-module test passed.

The Windows OTP 29.1.1 corpus generated all 1,366 images with 369 Equal, 997 Different and zero SourceRejected. ELDAPv3 remains Equal. The OTP loader accepted all 997 differing images. Of 368 changed images, 329 narrowed the Code size gap and 29 widened it. The net reduction was 12,914 bytes. The normalized comparison found 43 gained instruction matches and zero losses. Peak stack size moved closer to OTP in 692 functions and farther in 47. A total of 135 changed frames reach the OTP size.

The nonempty-list first-difference group fell from 613 modules and 3,954 functions to 580 modules and 3,328 functions. The release example in AttributeCertificateVersion1-2009 now tests X0 before allocate. Its nonempty branch uses ten slots, down from eleven. OTP uses four. The corpus has eleven images with one differing normalized function. Their remaining chunk differences are in the ranking.

The full runtime suite has not run on this build. Canonical readiness and publication remain pending.

- [Full corpus](otp-corpus.json)
- [Code size changes](code-delta.json)
- [Normalized function comparison](normalized-functions.txt)
- [Peak frame comparison](peak-slots.txt)
- [OTP loader check](loader.txt)
- [Frame audit](frame-shapes.txt)
- [Remaining function and chunk ranking](remaining-functions.txt)
- [Ranking script](rank-remaining-functions.escript)
- [Release instructions](release-functions.txt)
- [Compiler tests](focused.json)
- [Whole-module test](whole-module.txt)
- [Fixture source](list_clause_probe.erl)
- [Focused check](check-list-clause.ps1)
- [Focused output](probe.txt)
- [Fixture instructions](instructions.txt)
