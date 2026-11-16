# Part 1752

Plan shared Y slots across receive loops by propagating liveness through the scan and wait edges. The regression matches OTP's 3-slot and 2-slot frames.

The C# compiler rebuilt all 1,366 Windows OTP 29.1.1 sources with 398 Equal and 968 Different. ELDAPv3 from the C# ASN.1 generator matches the release. The pinned loader accepts both images of every differing pair. No status changes.

1585 stack slot-count comparisons at corresponding frame allocations across 498 changed images, with 152 new matches and 0 lost matches. Sum of absolute slot-count gaps decreases by 7980 across comparable allocations. The absolute Code size gap decreases by 26723 bytes. Normalized instructions gain 0 function matches and lose 0.

The compiler and test builds succeeded. All 11 targeted tests passed. The new test fails against the compiler from part 1751. The OTP VM agreed on receive and timeout results, the untouched message and process dictionary effects.

On .NET 10.0.11, DETS passed and the port and dynamic apply checks reproduced the failures documented in part 1742. The full runtime suite and readiness regeneration were outside this run.

[Corpus](otp-corpus.json) · [Changed statuses](status-changes.txt) · [Loader and Code](loader-and-code.txt) · [Normalized functions](normalized-functions.txt) · [Slot-count changes](delta.txt) · [Remaining receive frames](remaining.txt) · [First differing frames](first-frame-differences.txt) · [Remaining frame shapes](frame-shapes.txt) · [Entry spills](entry-spills.txt) · [Focused tests](focused.txt) · [Receive tests](mailbox.txt) · [Previous compiler](negative.txt) · [Fixture instructions](probe.txt) · [Fixture execution](behavior.txt) · [Runtime gates](runtime-gates.txt)
