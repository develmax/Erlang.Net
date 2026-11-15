# Part 1756

Allocate only the retained frame slots when an initial trim discards slots the straight-line prefix has not used. The fixture now matches OTP's single-slot frame.

The C# compiler rebuilt all 1,366 Windows OTP 29.1.1 sources with 398 Equal and 968 Different. ELDAPv3 from the C# ASN.1 generator matches the release. The pinned loader accepts both images of every differing pair. No status changes.

148 stack slot-count comparisons at corresponding frame allocations across 129 changed images, with 30 new matches and 0 lost matches. Sum of absolute slot-count gaps decreases by 148 across comparable allocations. 177 trim-count comparisons in corresponding functions across 129 changed images, with 133 new matches and 30 lost matches. Sum of absolute trim-count gaps decreases by 89 across corresponding functions. The absolute Code size gap decreases by 569 bytes. Normalized instructions gain 16 function matches and lose 0.

The compiler and test builds succeeded. All 11 targeted tests passed. The new test fails against the compiler from part 1755. The OTP VM agreed on the smaller entry frame, the inter-call trim, invalid tuple handling and dictionary effects.

On .NET 10.0.11, DETS passed and the port and dynamic apply checks reproduced the failures documented in part 1742. The full runtime suite and readiness regeneration were outside this run.

[Corpus](otp-corpus.json) · [Changed statuses](status-changes.txt) · [Loader and Code](loader-and-code.txt) · [Normalized functions](normalized-functions.txt) · [Slot-count changes](delta.txt) · [Trim-count changes](count-delta.txt) · [First differing frames](first-frame-differences.txt) · [Remaining projection uses](projection-uses.txt) · [Remaining pre-call trims](precall-trim-candidates.txt) · [Trim between calls](control-trim.txt) · [Focused tests](focused.txt) · [Receive tests](mailbox.txt) · [Previous compiler](negative.txt) · [Fixture instructions](probe.txt) · [Fixture execution](behavior.txt) · [Runtime gates](runtime-gates.txt)
