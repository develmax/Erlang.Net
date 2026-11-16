# Part 1754

Remove a Y-to-X reload when the destination still holds the saved entry argument on every path. Run partial removal after return expansion so the existing guarded frame can still be delayed.

The C# compiler rebuilt all 1,366 Windows OTP 29.1.1 sources with 398 Equal and 968 Different. ELDAPv3 from the C# ASN.1 generator matches the release. The pinned loader accepts both images of every differing pair. No status changes.

27 stack slot-count comparisons at corresponding frame allocations across 675 changed images, with 3 new matches and 0 lost matches. Sum of absolute slot-count gaps decreases by 30 across comparable allocations. 3987 Y-to-X move-count comparisons in corresponding functions across 675 changed images, with 530 new matches and 4 lost matches. Sum of absolute move-count gaps decreases by 12833 across corresponding functions. The absolute Code size gap decreases by 38845 bytes. Normalized instructions gain 8 function matches and lose 0.

The compiler and test builds succeeded. All 11 targeted tests passed. The new test fails against the compiler from part 1753. The OTP VM agreed on branch results, call preservation, dictionary effects, exceptions and guarded frame behavior.

On .NET 10.0.11, DETS passed and the port and dynamic apply checks reproduced the failures documented in part 1742. The full runtime suite and readiness regeneration were outside this run.

[Corpus](otp-corpus.json) · [Changed statuses](status-changes.txt) · [Loader and Code](loader-and-code.txt) · [Normalized functions](normalized-functions.txt) · [Slot-count changes](delta.txt) · [Reload-count changes](move-delta.txt) · [First differing frames](first-frame-differences.txt) · [Remaining entry spills](entry-spills.txt) · [Tuple projections around calls](projection-call-candidates.txt) · [Guarded frame disassembly](guarded-frame.txt) · [Variants](variants.txt) · [Focused tests](focused.txt) · [Receive tests](mailbox.txt) · [Previous compiler](negative.txt) · [Fixture instructions](probe.txt) · [Fixture execution](behavior.txt) · [Runtime gates](runtime-gates.txt)
