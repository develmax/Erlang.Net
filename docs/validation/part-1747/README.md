# Part 1747

Continue delaying a shared frame even when earlier entry guards precede it. Those guards may fail through func_info, and the existing frame dominance check handles subsequent predicates and return paths.

All 1,366 Windows OTP 29.1.1 sources were admitted and compiled. 398 images matched byte for byte, including ELDAPv3 from the C# ASN.1 generator. 968 differed and passed the OTP loader check. The change affects 16 images and changes the total absolute Code size gap, which decreases by 15 bytes. Normalized instructions gain 0 function matches and lose 0. No status changes.

2 entry guard placement comparisons across 16 changed images, with 2 new order matches and 0 lost matches.

The build and all six targeted tests passed. The [fixture](../../../tests/Erlang.Tests/Resources/Beam/beam_guarded_frame_prefix.erl) checks that both predicates precede the frame. The OTP VM verifies the framed branch, the fast return and function_clause arguments. The full runtime suite and readiness regeneration were outside this run. The rebuilt compiler SHA-256 matches the compiler used for the full corpus report.

[Corpus](otp-corpus.json) · [Status changes](status-changes.txt) · [Loader and Code](loader-and-code.txt) · [Functions](normalized-functions.txt) · [Tests](focused.txt) · [BIF changes](delta.txt) · [Probe](probe.txt) · [Remaining BIF shapes](remaining-bif-shapes.txt) · [Placement experiments](placement-experiments.txt)
