# Part 1745

Place an eligible entry BIF before the frame. Replace saved argument reads with their original X registers, keep the incoming arguments live at the BIF, and extend the allocation live count when the result needs a separate X register.

All 1,366 Windows OTP 29.1.1 sources were admitted and compiled. 398 images matched byte for byte, including ELDAPv3 from the C# ASN.1 generator. 968 differed and passed the OTP loader check. The change affects 374 images and increases the total absolute Code size gap by 3777 bytes. Normalized instructions gain 3 function matches and lose 2. No status changes.

957 BIF placement comparisons across 374 changed images, with 834 new order matches and 123 lost matches.

The build and all six targeted tests passed. The [fixture](../../../tests/Erlang.Tests/Resources/Beam/beam_entry_bif_placement.erl) checks both allocation live counts and the resolved X operands. The pinned VM verified the three valid inputs and the badarith stack entries and source lines. On .NET 10.0.11, DETS passed and the port and dynamic apply checks reproduced the failures documented in part 1742. The full runtime suite and readiness regeneration were outside this run.

[Corpus](otp-corpus.json) · [Status changes](status-changes.txt) · [Loader and Code](loader-and-code.txt) · [Functions](normalized-functions.txt) · [Tests](focused.txt) · [BIF changes](delta.txt) · [Probe](probe.txt) · [Initial experiment](initial-experiment.txt) · [Predicate placement candidates](bif-guard-placement-suspects.txt) · [Remaining BIF placement](remaining-bif-placement.txt) · [Runtime gates](runtime-gates.txt)
