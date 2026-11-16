# Part 1749

Reduce the live X count in test_heap when the existing continuation analysis proves that the trailing registers have no reads. The regression covers a separator branch that releases x3 and an ordinary branch that retains it.

The C# compiler rebuilt all 1,366 Windows OTP 29.1.1 sources with 398 Equal and 968 Different. ELDAPv3 from the C# ASN.1 generator matches the release. The pinned loader accepts both images of every differing pair. No status changes.

23 live-X count comparisons at corresponding heap tests across 22 changed images, with 17 new matches and 0 lost matches. The absolute Code size gap is unchanged. Normalized instructions gain 3 function matches and lose 0.

The compiler and test builds succeeded. All six targeted tests passed. The new test fails against the compiler from part 1748. The OTP VM returned the same three results for the locally generated and OTP-compiled fixture.

On .NET 10.0.11, DETS passed and the port and dynamic apply checks reproduced the failures documented in part 1742. The full runtime suite and readiness regeneration were outside this run.

[Corpus](otp-corpus.json) · [Changed statuses](status-changes.txt) · [Loader and Code](loader-and-code.txt) · [Normalized functions](normalized-functions.txt) · [Live X matches](delta.txt) · [Remaining heap tests](remaining.txt) · [Focused tests](focused.txt) · [Previous compiler](negative.txt) · [Fixture instructions](probe.txt) · [Fixture execution](behavior.txt) · [Runtime gates](runtime-gates.txt)
