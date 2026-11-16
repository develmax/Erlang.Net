# Part 1750

Place test_heap before eligible tuple field reads. The live X count drops from four to one in the regression fixture.

The C# compiler rebuilt all 1,366 Windows OTP 29.1.1 sources with 398 Equal and 968 Different. ELDAPv3 from the C# ASN.1 generator matches the release. The pinned loader accepts both images of every differing pair. No status changes.

243 tuple-read adjacency comparisons at corresponding heap tests across 273 changed images, with 157 new matches and 0 lost matches. 243 live-X count comparisons at corresponding heap tests across 273 changed images, with 178 new matches and 13 lost matches. The counts can still differ after the instructions align, as in ct_cover:get_app_info/3 where the tuple uses x3 in the C# output and x4 in OTP. The absolute Code size gap is unchanged. Normalized instructions gain 5 function matches and lose 0.

The compiler and test builds succeeded. All six targeted tests passed. The new test fails against the compiler from part 1749. The OTP VM agreed for the missing key, wrong tag and 1,000 tuple reconstructions with garbage collection.

On .NET 10.0.11, DETS passed and the port and dynamic apply checks reproduced the failures documented in part 1742. The full runtime suite and readiness regeneration were outside this run.

[Corpus](otp-corpus.json) · [Changed statuses](status-changes.txt) · [Loader and Code](loader-and-code.txt) · [Normalized functions](normalized-functions.txt) · [Instruction order](order.txt) · [Live X matches](delta.txt) · [Remaining instruction groups](heap-neighbors.txt) · [Nullary BIF candidates](nullary-bif-heap-candidates.txt) · [Remaining heap tests](remaining.txt) · [Focused tests](focused.txt) · [Previous compiler](negative.txt) · [Fixture instructions](probe.txt) · [Fixture execution](behavior.txt) · [Runtime gates](runtime-gates.txt)
