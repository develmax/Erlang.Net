# Part 1743

Write tuple fields straight to the X registers that need them. Check that advancing the write cannot overwrite any operand read by intervening moves and projections. Leave the Y slot initialized until the compiler can remove it.

All 1,366 Windows OTP 29.1.1 sources were admitted and compiled. 398 images matched byte for byte, including ELDAPv3 from the C# ASN.1 generator. 968 differed and passed the OTP loader check. The change affects 522 images and decreases the total absolute Code size gap by 5104 bytes. Normalized instructions gain 11 function matches and lose 0. No status changes.

2731 more direct X projections, 2731 fewer Y projections, 2731 fewer Y-to-X moves, and 903 fewer slots across 522 changed images.

The build and all six targeted tests passed. The [fixture](../../../tests/Erlang.Tests/Resources/Beam/beam_direct_call_projections.erl) now allocates one slot and writes both tuple fields into the argument registers as OTP does. The pinned VM verifies both functions on three inputs, including heap values and an input tuple still needed in X0. On .NET 10.0.11, DETS passed and the port and dynamic apply tests reproduced their failures from part 1742. The full runtime suite and readiness regeneration were outside this run.

[Corpus](otp-corpus.json) · [Status changes](status-changes.txt) · [Loader and Code](loader-and-code.txt) · [Functions](normalized-functions.txt) · [Tests](focused.txt) · [Projection changes](delta.txt) · [Probe](probe.txt) · [Remaining chunks](remaining-chunks.txt) · [BIF result candidates](bif-result-copy-suspects.txt) · [Runtime gates](runtime-gates.txt)
