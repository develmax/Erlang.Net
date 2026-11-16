# Part 1744

Store BIF results in their saved Y slot across an ordinary call when the call does not read the X register. The failure edge keeps its original target and does not write the destination. An earlier experiment allowed the rewrite anywhere X was dead and reduced destination alignment with OTP.

All 1,366 Windows OTP 29.1.1 sources were admitted and compiled. 398 images matched byte for byte, including ELDAPv3 from the C# ASN.1 generator. 968 differed and passed the OTP loader check. The change affects 63 images and decreases the total absolute Code size gap by 344 bytes. Normalized instructions gain 0 function matches and lose 0. No status changes.

115 more direct Y BIF destinations and 115 fewer X-to-Y moves across 63 changed images. Within functions with the same ordered BIF signatures, 82 destination kinds match OTP after the change and 6 cease to match.

The build and all six targeted tests passed. The [fixture](../../../tests/Erlang.Tests/Resources/Beam/beam_bif_result_store.erl) checks both the direct Y destination in the successful case and the original X destination when the call reads that register. The pinned VM verified the three valid inputs and the badarith handler. On .NET 10.0.11 the DETS check passed, and the port and dynamic apply checks reproduced the same failures documented in part 1742. The full runtime suite and readiness regeneration were outside this run.

[Corpus](otp-corpus.json) · [Status changes](status-changes.txt) · [Loader and Code](loader-and-code.txt) · [Functions](normalized-functions.txt) · [Tests](focused.txt) · [BIF changes](delta.txt) · [Probe](probe.txt) · [Initial experiment](initial-experiment.txt) · [BIF frame placement candidates](bif-frame-placement-suspects.txt) · [Runtime gates](runtime-gates.txt)
