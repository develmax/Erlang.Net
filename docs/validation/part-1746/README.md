# Part 1746

Delay the stack frame through eligible predicates after an entry BIF. Apply the existing dominance check to the frame location so frameless return paths keep their original X operands.

All 1,366 Windows OTP 29.1.1 sources were admitted and compiled. 398 images matched byte for byte, including ELDAPv3 from the C# ASN.1 generator. 968 differed and passed the OTP loader check. The change affects 20 images and changes the total absolute Code size gap, which decreases by 81 bytes. Normalized instructions gain 3 function matches and lose 0. No status changes.

17 predicate placement comparisons across 20 changed images, with 16 new order matches and 1 lost matches.

The build and all six targeted tests passed. The [fixture](../../../tests/Erlang.Tests/Resources/Beam/beam_bif_predicate_frame.erl) checks the BIF, predicate and allocation order. The pinned VM verified both return paths, dictionary state and the case_clause error. On .NET 10.0.11, DETS passed and the port and dynamic apply checks reproduced the failures documented in part 1742. The full runtime suite and readiness regeneration were outside this run.

[Corpus](otp-corpus.json) · [Status changes](status-changes.txt) · [Loader and Code](loader-and-code.txt) · [Functions](normalized-functions.txt) · [Tests](focused.txt) · [BIF changes](delta.txt) · [Probe](probe.txt) · [Remaining BIF shapes](remaining-bif-shapes.txt) · [Predicate placement candidates](bif-guard-placement-suspects.txt) · [Runtime gates](runtime-gates.txt)
