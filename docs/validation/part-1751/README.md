# Part 1751

Place test_heap before eligible erlang:self/0 and erlang:node/0 calls. Both branches in the regression fixture use zero live X registers at this point.

The C# compiler rebuilt all 1,366 Windows OTP 29.1.1 sources with 398 Equal and 968 Different. ELDAPv3 from the C# ASN.1 generator matches the release. The pinned loader accepts both images of every differing pair. No status changes.

223 nullary BIF adjacency comparisons at corresponding heap tests across 229 changed images, with 190 new matches and 0 lost matches. 223 live-X count comparisons at corresponding heap tests across 229 changed images, with 115 new matches and 96 lost matches. Some counts diverge when OTP keeps a value in X and the C# compiler has already saved it in Y. For example, asn1ct_name:req/1 keeps the monitor result in x0 until after the heap check in OTP. The absolute Code size gap decreases by 402 bytes. Normalized instructions gain 2 function matches and lose 0.

The compiler and test builds succeeded. All six targeted tests passed. The new test fails against the compiler from part 1750. The OTP VM agreed on both return values, sent messages and dictionary effects.

On .NET 10.0.11, DETS passed and the port and dynamic apply checks reproduced the failures documented in part 1742. The full runtime suite and readiness regeneration were outside this run.

[Corpus](otp-corpus.json) · [Changed statuses](status-changes.txt) · [Loader and Code](loader-and-code.txt) · [Normalized functions](normalized-functions.txt) · [Instruction order](order.txt) · [Live X matches](delta.txt) · [Remaining heap instruction groups](heap-neighbors.txt) · [First differing instructions](first-instruction-differences.txt) · [Frame differences](first-frame-differences.txt) · [Nullary BIF candidates](nullary-bif-heap-candidates.txt) · [Remaining heap tests](remaining.txt) · [Focused tests](focused.txt) · [Previous compiler](negative.txt) · [Fixture instructions](probe.txt) · [Monitor result spill](spill-example.txt) · [Fixture execution](behavior.txt) · [Runtime gates](runtime-gates.txt)
