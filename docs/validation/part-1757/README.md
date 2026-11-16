# Part 1757

Sink eligible tuple reads past calls while tracking the old field value through overwrites and trim. Preserve a tuple for newly eligible single-field sinks when the tuple is independently needed after the last call.

The C# compiler rebuilt all 1,366 Windows OTP 29.1.1 sources with 398 Equal and 968 Different. ELDAPv3 from the C# ASN.1 generator matches the release. The pinned loader accepts both images of every differing pair.

79 stack slot-count comparisons at corresponding frame allocations across 221 changed images, with 10 new matches and 7 lost matches. Sum of absolute slot-count gaps decreases by 84 across comparable allocations. 752 tuple-field placement comparisons at corresponding calls across 221 changed images, with 383 new matches and 153 lost matches. The absolute Code size gap increases by 1053 bytes. Normalized instructions gain 0 function matches and lose 0.

The compiler and test builds succeeded. All 11 targeted tests passed. The new test fails against the compiler from part 1756. The OTP VM agreed on the deferred fields, replacement values in the reused slots, projection after two calls, invalid tuple handling and dictionary effects.

On .NET 10.0.11, DETS passed and the port and dynamic apply checks reproduced the failures documented in part 1742. The full runtime suite and readiness regeneration were outside this run.

[Corpus](otp-corpus.json) · [Changed statuses](status-changes.txt) · [Loader and Code](loader-and-code.txt) · [Normalized functions](normalized-functions.txt) · [Slot-count changes](delta.txt) · [Projection placement](placement-delta.txt) · [First differing frames](first-frame-differences.txt) · [Remaining projection uses](projection-uses.txt) · [Repeated initialization candidates](repeated-initialization.txt) · [Variant comparison](variants.txt) · [Focused tests](focused.txt) · [Receive tests](mailbox.txt) · [Previous compiler](negative.txt) · [Fixture instructions](probe.txt) · [Fixture execution](behavior.txt) · [Runtime gates](runtime-gates.txt)
