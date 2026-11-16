# Part 1755

Keep a tuple across an ordinary call and sink eligible field reads to the later consumer. The garbage-collection regression uses one Y slot and the same field-read order as OTP.

The C# compiler rebuilt all 1,366 Windows OTP 29.1.1 sources with 398 Equal and 968 Different. ELDAPv3 from the C# ASN.1 generator matches the release. The pinned loader accepts both images of every differing pair. No status changes.

128 stack slot-count comparisons at corresponding frame allocations across 207 changed images, with 37 new matches and 2 lost matches. Sum of absolute slot-count gaps decreases by 144 across comparable allocations. 394 tuple-field placement comparisons at corresponding calls across 207 changed images, with 125 new matches and 81 lost matches. The absolute Code size gap decreases by 218 bytes. Normalized instructions gain 0 function matches and lose 0.

The compiler and test builds succeeded. All 11 targeted tests passed. The updated test fails against the compiler from part 1754. The OTP VM agreed on field values, call results, early field uses and process dictionary effects.

On .NET 10.0.11, DETS passed and the port and dynamic apply checks reproduced the failures documented in part 1742. The full runtime suite and readiness regeneration were outside this run.

[Corpus](otp-corpus.json) · [Changed statuses](status-changes.txt) · [Loader and Code](loader-and-code.txt) · [Normalized functions](normalized-functions.txt) · [Slot-count changes](delta.txt) · [Projection placement](placement-delta.txt) · [First differing frames](first-frame-differences.txt) · [Remaining projection candidates](projection-call-candidates.txt) · [Pre-call frame trims](precall-trim-candidates.txt) · [Additional tuple fixtures](tuple-fixtures.txt) · [Focused tests](focused.txt) · [Receive tests](mailbox.txt) · [Previous compiler](negative.txt) · [Fixture instructions](probe.txt) · [Fixture execution](behavior.txt) · [Runtime gates](runtime-gates.txt)
