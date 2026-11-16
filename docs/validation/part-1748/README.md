# Part 1748

Trim unused trailing X registers from the live count of stack allocations. Use explicit reads, implicit call arguments and heap roots, and the continuation control flow to prove each register is dead.

All 1,366 Windows OTP 29.1.1 sources were admitted and compiled. 398 images matched byte for byte, including ELDAPv3 from the C# ASN.1 generator. 968 differed and passed the OTP loader check. The change affects 181 images with the same total absolute Code size gap. Normalized instructions gain 10 function matches and lose 0. No status changes.

53 live-X count comparisons at corresponding frame allocations across 181 changed images, with 53 new matches and 0 lost matches.

The build and all six targeted tests passed. The [fixture](../../../tests/Erlang.Tests/Resources/Beam/beam_bif_predicate_frame.erl) checks allocation with no live X registers inside the guarded branch. The pinned VM verifies both results and the case_clause error. On .NET 10.0.11, DETS passed and the port and dynamic apply checks reproduced the failures documented in part 1742. The full runtime suite and readiness regeneration were outside this run.

[Corpus](otp-corpus.json) · [Status changes](status-changes.txt) · [Loader and Code](loader-and-code.txt) · [Functions](normalized-functions.txt) · [Tests](focused.txt) · [Live-X changes](delta.txt) · [Probe](probe.txt) · [Remaining allocation counts](remaining-allocation-live.txt) · [Heap test candidates](heap-test-live-candidates.txt) · [Runtime gates](runtime-gates.txt)
